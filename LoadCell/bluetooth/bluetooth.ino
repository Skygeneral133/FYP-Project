#include <ArduinoBLE.h>

// =====================================================
// HX710 PINS
// =====================================================

#define HX710_DOUT 3
#define HX710_SCLK 2


// =====================================================
// CALIBRATION
// =====================================================

const float CAL_FACTOR = -12976.91;


// =====================================================
// SETTINGS
// =====================================================

const int NUM_AVERAGE_READINGS = 5;


// =====================================================
// BLE UUIDs
// =====================================================

BLEService loadCellService(
  "19B10000-E8F2-537E-4F6C-D104768A1214"
);

BLEStringCharacteristic loadCellCharacteristic(
  "19B10001-E8F2-537E-4F6C-D104768A1214",
  BLERead | BLENotify,
  20
);


// =====================================================
// VARIABLES
// =====================================================

long zeroOffset = 0;


// =====================================================
// SETUP
// =====================================================

void setup() {

  Serial.begin(115200);

  delay(1000);


  // ===================================================
  // HX710 SETUP
  // ===================================================

  pinMode(HX710_DOUT, INPUT);
  pinMode(HX710_SCLK, OUTPUT);

  digitalWrite(HX710_SCLK, LOW);

  Serial.println("Starting HX710...");

  delay(1000);


  // ===================================================
  // TARE
  // ===================================================

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


  // ===================================================
  // BLE SETUP
  // ===================================================

  Serial.println("Starting BLE...");

  if (!BLE.begin()) {

    Serial.println("BLE failed to start!");

    while (1);
  }

  BLE.setLocalName("LoadCellArduino");
  BLE.setDeviceName("LoadCellArduino");

  BLE.setAdvertisedService(loadCellService);

  loadCellService.addCharacteristic(
    loadCellCharacteristic
  );

  BLE.addService(loadCellService);

  loadCellCharacteristic.writeValue("0.00");

  BLE.advertise();

  Serial.println("BLE started!");
  Serial.println("Device name: LoadCellArduino");
  Serial.println("Waiting for BLE connection...");
  Serial.println();
}


// =====================================================
// MAIN LOOP
// =====================================================

void loop() {

  // ===================================================
  // CHECK BLE CONNECTION
  // ===================================================

  BLEDevice central = BLE.central();

  if (central) {

    Serial.println();
    Serial.print("Connected to: ");
    Serial.println(central.address());
    Serial.println();


    // =================================================
    // RUN WHILE CONNECTED
    // =================================================

    while (central.connected()) {


      // ===============================================
      // TAKE MULTIPLE READINGS
      // ===============================================

      long total = 0;

      for (int i = 0; i < NUM_AVERAGE_READINGS; i++) {

        total += readHX710();

        delay(10);
      }


      // ===============================================
      // CALCULATE AVERAGE
      // ===============================================

      long averageRaw =
        total / NUM_AVERAGE_READINGS;


      // ===============================================
      // REMOVE TARE OFFSET
      // ===============================================

      long taredValue =
        averageRaw - zeroOffset;


      // ===============================================
      // CONVERT TO GRAMS
      // ===============================================

      float weight =
        taredValue / CAL_FACTOR;


      // ===============================================
      // SERIAL OUTPUT
      // ===============================================

      Serial.print("Raw Avg: ");
      Serial.print(averageRaw);

      Serial.print("    Tared: ");
      Serial.print(taredValue);

      Serial.print("    Weight: ");
      Serial.print(weight, 2);

      Serial.println(" g");


      // ===============================================
      // SEND WEIGHT THROUGH BLE
      // ===============================================

      String weightString = String(weight, 2);

      loadCellCharacteristic.writeValue(weightString);


      // ===============================================
      // UPDATE RATE
      // ===============================================

      delay(50);
    }


    // =================================================
    // DISCONNECTED
    // =================================================

    Serial.println();
    Serial.println("BLE disconnected.");
    Serial.println("Waiting for connection...");
  }
}


// =====================================================
// HX710 READ FUNCTION
// =====================================================

long readHX710() {

  // ---------------------------------------------------
  // WAIT UNTIL HX710 DATA IS READY
  // ---------------------------------------------------

  while (digitalRead(HX710_DOUT) == HIGH) {

    delayMicroseconds(10);
  }


  long value = 0;


  // ---------------------------------------------------
  // READ 24 BITS
  // ---------------------------------------------------

  for (int i = 0; i < 24; i++) {

    digitalWrite(HX710_SCLK, HIGH);

    delayMicroseconds(1);

    value =
      (value << 1) |
      digitalRead(HX710_DOUT);

    digitalWrite(HX710_SCLK, LOW);

    delayMicroseconds(1);
  }


  // ---------------------------------------------------
  // 25TH CLOCK PULSE
  // ---------------------------------------------------

  digitalWrite(HX710_SCLK, HIGH);

  delayMicroseconds(1);

  digitalWrite(HX710_SCLK, LOW);

  delayMicroseconds(1);


  // ---------------------------------------------------
  // CONVERT 24-BIT TWO'S COMPLEMENT
  // ---------------------------------------------------

  if (value & 0x800000) {

    value |= 0xFF000000;
  }


  return value;
}