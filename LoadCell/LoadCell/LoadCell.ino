#include <ArduinoBLE.h>

#define HX710_DOUT 4
#define HX710_SCLK 2

const float CAL_FACTOR = -12976.91;
const int NUM_AVERAGE_READINGS = 5;
long zeroOffset = 0;

BLEService loadCellService("19B10000-E8F2-537E-4F6C-D104768A1214");
BLEStringCharacteristic loadCellCharacteristic("19B10001-E8F2-537E-4F6C-D104768A1214", BLERead | BLENotify, 30);  // Load cell reading characteristic
BLEStringCharacteristic tareCharacteristic( "19B10002-E8F2-537E-4F6C-D104768A1214", BLEWrite, 20);  // Tare command characteristic


float previousWeight = 0.0;
unsigned long previousTime = 0;

void setup() {
  Serial.begin(115200);
  delay(1000);
  pinMode(HX710_DOUT, INPUT);
  pinMode(HX710_SCLK, OUTPUT);
  digitalWrite(HX710_SCLK, LOW);
  Serial.println("Starting HX710...");
  delay(1000);

  // Initial tare
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

  // Add characteristic
  loadCellService.addCharacteristic(loadCellCharacteristic);
  loadCellService.addCharacteristic(tareCharacteristic);

  // Add service
  BLE.addService(loadCellService);

  loadCellCharacteristic.writeValue("0.00,0.00"); // Initial value

  BLE.advertise();
  Serial.println("BLE started!");
  Serial.println("Device name: LoadCellArduino");
  Serial.println("Waiting for BLE connection...");
  Serial.println();
}

void loop() {
  // Check BLE connection
  BLEDevice central = BLE.central();

  if (central) {
    Serial.println();
    Serial.print("Connected to: ");
    Serial.println(central.address());
    Serial.println();


    // Reset pouring-rate calculation
    previousWeight = 0.0;
    previousTime = millis();  // Start time for pouring rate calculation
    while (central.connected()) {

      // Check for tare command
      if (tareCharacteristic.written()) {
        String command = tareCharacteristic.value();
        command.trim();
        Serial.print("BLE command received: ");
        Serial.println(command);

        if (command == "TARE") {
          Serial.println();
          Serial.println("==============================");
          Serial.println("RETARING LOAD CELL");
          Serial.println("==============================");

          Serial.println(
            "Remove all weight from load cell."
          );

          // Take 20 readings
          long tareTotal = 0;
          for (int i = 0; i < 20; i++) {
            tareTotal += readHX710();
            delay(50);
          }

          // Calculate new zero offset
          zeroOffset = tareTotal / 20;

          // Reset pouring rate
          previousWeight = 0.0;
          previousTime = millis();

          Serial.print("New zero offset: ");
          Serial.println(zeroOffset);
          Serial.println("Tare complete!");
          Serial.println("==============================");
          Serial.println();
        }
      }

      // Take multiple readings
      long total = 0;
      for (int i = 0; i < NUM_AVERAGE_READINGS; i++) {
        total += readHX710();
        delay(10);
      }

      // Average raw reading
      long averageRaw = total / NUM_AVERAGE_READINGS;

      // Apply tare
      long taredValue = averageRaw - zeroOffset;

      // Convert to grams
      float weight = taredValue / CAL_FACTOR;

      // Calculate time difference
      unsigned long currentTime = millis();
      float dt = (currentTime - previousTime)/ 1000.0;

      // Calculate pouring rate
      float pouringRate = 0.0;
      if (dt > 0) {
        pouringRate = (weight - previousWeight) / dt;
      }

      // Update previous values
      previousWeight = weight;
      previousTime = currentTime;
      
      // Serial Monitor
      Serial.print("Weight: ");
      Serial.print(weight,2);
      Serial.print(" g");
      Serial.print("    Pouring Rate: ");
      Serial.print(pouringRate, 2);
      Serial.println(" g/s");

      // Send BLE data in Format : weight,pouringRate
      String data = String(weight, 2) + "," + String(pouringRate, 2); 
      loadCellCharacteristic.writeValue(data);

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
  unsigned long startTime = micros();
  while (digitalRead(HX710_DOUT) == HIGH) {

    if (micros() - startTime > 1000000) {
      Serial.println("ERROR: HX710 DOUT stayed HIGH");
      return 0;
    }

    delayMicroseconds(10);
  }

  long value = 0;

  // Read 24 bits
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

  // Convert 24-bit signed value
  if (value & 0x800000) {
    value |= 0xFF000000;
  }
  
  return value;
}