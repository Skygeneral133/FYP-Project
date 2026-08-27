#include <ArduinoBLE.h>

// Define HX710 pins
#define HX710_DOUT 4
#define HX710_SCLK 2

// Calibration factor 
// Calculate by taredValue = averageRaw (at 100g) - zeroOffset (at 0g) and then CAL_FACTOR = taredValue / 100.0 
const float CAL_FACTOR = -12976.91;  // Increase cal_fac, decreases weight reading. Decrease cal_fac, increases weight reading.

// Variables 
const int NUM_AVERAGE_READINGS = 5;
long zeroOffset = 0;

// BLE UUIDs (Bluetooth Low Energy - Universally Unique Identifier)
// https://docs.arduino.cc/learn/communication/bluetooth/ 
BLEService loadCellService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLEStringCharacteristic loadCellCharacteristic("19B10001-E8F2-537E-4F6C-D104768A1214", BLERead | BLENotify, 20);

void setup() {
  Serial.begin(115200);
  delay(1000);

  // Start HX710
  pinMode(HX710_DOUT, INPUT);
  pinMode(HX710_SCLK, OUTPUT);
  digitalWrite(HX710_SCLK, LOW);
  Serial.println("Starting HX710...");
  delay(1000);

  // Tare the load cell to get the zero offset
  Serial.println();
  Serial.println("Taring...");
  Serial.println("Make sure there is NO weight on the load cell.");

  long total = 0;
  for (int i = 0; i < 20; i++) {
    total += readHX710();
    delay(50);
  }

  zeroOffset = total / 20;

  Serial.print("Zero offset: ");
  Serial.println(zeroOffset);

  Serial.println("Tare complete.");
  Serial.println("Apply weight...");
  Serial.println();

  // BLE SETUP
  Serial.println("Starting BLE...");
  if (!BLE.begin()) {
    Serial.println("BLE failed to start!");
    while (1);
  }

  BLE.setLocalName("LoadCellArduino");
  BLE.setDeviceName("LoadCellArduino");
  BLE.setAdvertisedService(loadCellService);
  loadCellService.addCharacteristic(loadCellCharacteristic);
  BLE.addService(loadCellService);
  loadCellCharacteristic.writeValue("0.00");
  BLE.advertise();
  Serial.println("BLE started!");
  Serial.println("Device name: LoadCellArduino");
  Serial.println("Waiting for BLE connection...");
  Serial.println();
}

void loop() {
  // CHECK BLE CONNECTION
  BLEDevice central = BLE.central();

  if (central) {
    Serial.println();
    Serial.print("Connected to: ");
    Serial.println(central.address());
    Serial.println();

    while (central.connected()) {
      // Take multiple readings
      long total = 0;
      for (int i = 0; i < NUM_AVERAGE_READINGS; i++) {
        total += readHX710();
        delay(10);
      }

      long averageRaw = total / NUM_AVERAGE_READINGS;  // Get average of the readings
      long taredValue = averageRaw - zeroOffset;       // Subtract the zero offset to get the tared value
      float weight = taredValue / CAL_FACTOR;          // Convert to grams

      // Serial output for debugging (Raw Avg: -7787477    Tared: -12977    Weight: 1.00 g)
      Serial.print("Raw Avg: ");
      Serial.print(averageRaw);
      Serial.print("    Tared: ");
      Serial.print(taredValue);
      Serial.print("    Weight: ");
      Serial.print(weight, 2);
      Serial.println(" g");

      // Send weight to BLE characteristic
      String weightString = String(weight, 2);
      loadCellCharacteristic.writeValue(weightString);

      // Update rate
      delay(50);
    }

    // BLE disconnected
    Serial.println();
    Serial.println("BLE disconnected.");
    Serial.println("Waiting for connection...");
  }
}

// HX710 READ FUNCTION
long readHX710() {
  // Wait for HX710 to be ready
  unsigned long startTime = micros();

  while (digitalRead(HX710_DOUT) == HIGH) {
    if (micros() - startTime > 1000000) {  // 1 second timeout
      Serial.println("ERROR: HX710 DOUT stayed HIGH");
      return 0;
    }
    delayMicroseconds(10);
  }

  
  long value = 0;

  // Read 24 bits of data from HX710
  for (int i = 0; i < 24; i++) {
    digitalWrite(HX710_SCLK, HIGH);
    delayMicroseconds(1);
    value = (value << 1) | digitalRead(HX710_DOUT);
    digitalWrite(HX710_SCLK, LOW);
    delayMicroseconds(1);
  }
  
  // 25th clock pulse
  digitalWrite(HX710_SCLK, HIGH);
  delayMicroseconds(1);
  digitalWrite(HX710_SCLK, LOW);
  delayMicroseconds(1);

  // Convert 24-bit signed value to 2's complement
  if (value & 0x800000) {
    value |= 0xFF000000;
  }

  return value;
}