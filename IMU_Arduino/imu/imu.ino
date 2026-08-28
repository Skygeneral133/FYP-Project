#include <Arduino_LSM6DS3.h>
#include <ArduinoBLE.h>

// =====================================================
// BLE UUIDs
// =====================================================

#define SERVICE_UUID        "19b10000-e8f2-537e-4f6c-d104768a1214"
#define CHARACTERISTIC_UUID "19b10001-e8f2-537e-4f6c-d104768a1214"
#define RESET_UUID          "19b10002-e8f2-537e-4f6c-d104768a1214"


// =====================================================
// BLE SERVICE
// =====================================================

BLEService imuService(SERVICE_UUID);


// =====================================================
// IMU DATA CHARACTERISTIC
//
// Arduino -> HTML
//
// Format:
//
// angleY,angularVelocityY
//
// Example:
//
// 25.43,18.72
// =====================================================

BLEStringCharacteristic dataCharacteristic(
  CHARACTERISTIC_UUID,
  BLERead | BLENotify,
  50
);


// =====================================================
// RESET CHARACTERISTIC
//
// HTML -> Arduino
//
// HTML sends:
//
// RESET
// =====================================================

BLEStringCharacteristic resetCharacteristic(
  RESET_UUID,
  BLEWrite,
  20
);


// =====================================================
// ANGLES
// =====================================================

float angleX = 0.0;
float angleY = 0.0;
float angleZ = 0.0;


// =====================================================
// GYRO BIAS
// =====================================================

float gyroBiasY = 0.0;


// =====================================================
// TIMING
// =====================================================

unsigned long previousTime = 0;


// =====================================================
// DEAD BAND
//
// Small gyro measurements below this value
// are treated as zero.
//
// This helps reduce small stationary drift/noise.
//
// Units: degrees/second
// =====================================================

const float GYRO_DEADBAND = 0.08;


// =====================================================
// CALIBRATION SETTINGS
// =====================================================

const int NUM_CALIBRATION_SAMPLES = 200;


// =====================================================
// CALIBRATE GYRO
// =====================================================

void calibrateGyro() {

  Serial.println();
  Serial.println("=================================");
  Serial.println("GYRO CALIBRATION");
  Serial.println("=================================");

  Serial.println(
    "Keep the IMU COMPLETELY STILL!"
  );

  Serial.println(
    "Do not move the beaker."
  );

  Serial.println(
    "Collecting samples..."
  );

  delay(1000);


  // ---------------------------------------------
  // Reset accumulated value
  // ---------------------------------------------

  float sumY = 0.0;

  int samplesTaken = 0;


  // ---------------------------------------------
  // Collect gyro samples
  // ---------------------------------------------

  while (
    samplesTaken < NUM_CALIBRATION_SAMPLES
  ) {

    if (IMU.gyroscopeAvailable()) {

      float gx;
      float gy;
      float gz;


      IMU.readGyroscope(
        gx,
        gy,
        gz
      );


      sumY += gy;

      samplesTaken++;


      // Print progress every 20 samples

      if (
        samplesTaken % 20 == 0
      ) {

        Serial.print(
          "Samples: "
        );

        Serial.print(
          samplesTaken
        );

        Serial.print(
          "/"
        );

        Serial.println(
          NUM_CALIBRATION_SAMPLES
        );
      }


      delay(5);
    }
  }


  // ---------------------------------------------
  // Calculate average bias
  // ---------------------------------------------

  gyroBiasY =
    sumY /
    NUM_CALIBRATION_SAMPLES;


  // ---------------------------------------------
  // Print result
  // ---------------------------------------------

  Serial.println();

  Serial.print(
    "Gyro Y Bias = "
  );

  Serial.print(
    gyroBiasY,
    4
  );

  Serial.println(
    " deg/s"
  );


  Serial.println(
    "Gyro calibration complete."
  );

  Serial.println(
    "================================="
  );

  Serial.println();


  // ---------------------------------------------
  // Reset angles
  // ---------------------------------------------

  angleX = 0.0;
  angleY = 0.0;
  angleZ = 0.0;


  // ---------------------------------------------
  // Reset timer
  // ---------------------------------------------

  previousTime = micros();
}


// =====================================================
// SETUP
// =====================================================

void setup() {

  Serial.begin(9600);

  while (!Serial);


  // ===================================================
  // START IMU
  // ===================================================

  if (!IMU.begin()) {

    Serial.println(
      "Failed to initialize IMU!"
    );

    while (1);
  }


  Serial.println(
    "IMU initialized!"
  );


  // ===================================================
  // START BLE
  // ===================================================

  if (!BLE.begin()) {

    Serial.println(
      "Failed to initialize BLE!"
    );

    while (1);
  }


  BLE.setLocalName(
    "IMUArduino"
  );


  BLE.setDeviceName(
    "IMUArduino"
  );


  BLE.setAdvertisedService(
    imuService
  );


  // ===================================================
  // ADD DATA CHARACTERISTIC
  // ===================================================

  imuService.addCharacteristic(
    dataCharacteristic
  );


  // ===================================================
  // ADD RESET CHARACTERISTIC
  // ===================================================

  imuService.addCharacteristic(
    resetCharacteristic
  );


  // ===================================================
  // ADD SERVICE
  // ===================================================

  BLE.addService(
    imuService
  );


  // ===================================================
  // INITIAL BLE VALUE
  // ===================================================

  dataCharacteristic.writeValue(
    "0.00,0.00"
  );


  // ===================================================
  // START ADVERTISING
  // ===================================================

  BLE.advertise();


  Serial.println(
    "BLE started!"
  );

  Serial.println(
    "Device name: IMUArduino"
  );

  Serial.println(
    "Waiting for connection..."
  );


  // ===================================================
  // INITIAL TIME
  // ===================================================

  previousTime = micros();
}


// =====================================================
// LOOP
// =====================================================

void loop() {

  // ===================================================
  // CHECK BLE CONNECTION
  // ===================================================

  BLEDevice central =
    BLE.central();


  if (central) {

    Serial.println();

    Serial.print(
      "Connected to: "
    );

    Serial.println(
      central.address()
    );

    Serial.println();


    // Reset timing when connection starts

    previousTime = micros();


    // =================================================
    // MAIN BLE LOOP
    // =================================================

    while (
      central.connected()
    ) {


      // ===============================================
      // CHECK FOR RESET COMMAND
      // ===============================================

      if (
        resetCharacteristic.written()
      ) {

        String command =
          resetCharacteristic.value();


        command.trim();


        Serial.print(
          "BLE command received: "
        );

        Serial.println(
          command
        );


        // =============================================
        // RESET COMMAND
        // =============================================

        if (
          command == "RESET"
        ) {

          // -------------------------------------------
          // Calibrate gyro and reset angle
          // -------------------------------------------

          calibrateGyro();


          // -------------------------------------------
          // Send zero value to HTML
          // -------------------------------------------

          dataCharacteristic.writeValue(
            "0.00,0.00"
          );


          Serial.println(
            "IMU reset complete."
          );

        }
      }


      // ===============================================
      // READ GYROSCOPE
      // ===============================================

      float gx;
      float gy;
      float gz;


      if (
        IMU.gyroscopeAvailable()
      ) {

        IMU.readGyroscope(
          gx,
          gy,
          gz
        );


        // =============================================
        // CALCULATE TIME
        // =============================================

        unsigned long currentTime =
          micros();


        float dt =
          (
            currentTime -
            previousTime
          )
          / 1000000.0;


        previousTime =
          currentTime;


        // =============================================
        // APPLY GYRO BIAS CORRECTION
        // =============================================

        float correctedGy =
          gy - gyroBiasY;


        // =============================================
        // APPLY DEAD BAND
        // =============================================

        if (
          abs(correctedGy)
          < GYRO_DEADBAND
        ) {

          correctedGy = 0.0;
        }


        // =============================================
        // INTEGRATE GYRO
        //
        // angle = angle + angular velocity * dt
        // =============================================

        angleY +=
          correctedGy * dt;


        // =============================================
        // Y ANGULAR VELOCITY
        // =============================================

        float angularVelocityY =
          correctedGy;


        // =============================================
        // SEND BLE DATA
        //
        // Format:
        //
        // angleY,angularVelocityY
        //
        // Example:
        //
        // 25.43,18.72
        // =============================================

        char data[50];


        snprintf(
          data,
          sizeof(data),
          "%.2f,%.2f",
          angleY,
          angularVelocityY
        );


        dataCharacteristic.writeValue(
          data
        );


        // =============================================
        // SERIAL MONITOR
        // =============================================

        Serial.print(
          "Y Angle: "
        );

        Serial.print(
          angleY,
          2
        );


        Serial.print(
          " deg | Y Angular Velocity: "
        );

        Serial.print(
          angularVelocityY,
          2
        );


        Serial.print(
          " deg/s | Bias: "
        );

        Serial.print(
          gyroBiasY,
          4
        );


        Serial.println(
          " deg/s"
        );
      }


      // ===============================================
      // LOOP DELAY
      // ===============================================

      delay(10);
    }


    // =================================================
    // BLE DISCONNECTED
    // =================================================

    Serial.println();

    Serial.println(
      "BLE disconnected."
    );

    Serial.println(
      "Waiting for connection..."
    );

  }
}