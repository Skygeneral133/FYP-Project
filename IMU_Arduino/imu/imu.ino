#include <Arduino_LSM6DS3.h>
#include <ArduinoBLE.h>

#define SERVICE_UUID        "19b10000-e8f2-537e-4f6c-d104768a1214"
#define CHARACTERISTIC_UUID "19b10001-e8f2-537e-4f6c-d104768a1214"
#define RESET_UUID          "19b10002-e8f2-537e-4f6c-d104768a1214"


BLEService imuService(SERVICE_UUID);
BLEStringCharacteristic dataCharacteristic(CHARACTERISTIC_UUID, BLERead | BLENotify, 50); // IMU data reading characteristic
BLEStringCharacteristic resetCharacteristic(RESET_UUID, BLEWrite, 20);  // Reset command characteristic

float angleY = 0.0;
float gyroBiasX = 0.0;
float gyroBiasY = 0.0;
float gyroBiasZ = 0.0;
float accelZeroY = 0.0;
unsigned long previousTime = 0;

const int NUM_CALIBRATION_SAMPLES = 300;
const float GYRO_DEADBAND = 0.10;

// Complementary filter: Higher number = trust gyro more | Lower number = trust accelerometer more
const float ALPHA = 0.98;

// CALCULATE ACCELEROMETER Y ANGLE
float getAccelAngleY() {
  float ax, ay, az;
  while (!IMU.accelerationAvailable()) {
  }
  IMU.readAcceleration(ax, ay, az);

  // Y axis rotation angle, 
  float angle = atan2(-ax, az)* 180.0 / PI;
  return angle;
}

// CALIBRATE GYROSCOPE
void calibrateGyro() {
  Serial.println();
  Serial.println("==============================");
  Serial.println("GYRO CALIBRATION");
  Serial.println("==============================");
  Serial.println("KEEP IMU COMPLETELY STILL!");
  delay(1500);

  float sumX = 0.0;
  float sumY = 0.0;
  float sumZ = 0.0;

  int samples = 0;

  while (samples < NUM_CALIBRATION_SAMPLES) {
    if (IMU.gyroscopeAvailable()) {
      float gx, gy, gz;
      IMU.readGyroscope(gx, gy, gz);
      sumX += gx;
      sumY += gy;
      sumZ += gz;
      samples++;
      delay(5);
    }
  }

  gyroBiasX = sumX / NUM_CALIBRATION_SAMPLES;
  gyroBiasY = sumY / NUM_CALIBRATION_SAMPLES;
  gyroBiasZ = sumZ / NUM_CALIBRATION_SAMPLES;

  Serial.print("GX bias: ");
  Serial.println(gyroBiasX, 4);
  Serial.print("GY bias: ");
  Serial.println(gyroBiasY, 4);
  Serial.print("GZ bias: ");
  Serial.println(gyroBiasZ, 4);
  Serial.println("Calibration complete.");
}

// ZERO CURRENT POSITION
void zeroAngle() {
  Serial.println();
  Serial.println("Setting current position to 0 deg...");
  const int samples = 100;
  float total = 0.0;
  for (int i = 0; i < samples; i++) {
    total += getAccelAngleY();
    delay(5);
  }

  accelZeroY = total / samples;
  angleY = 0.0;
  previousTime = micros();
  
  Serial.print("Accelerometer zero reference: ");
  Serial.print(accelZeroY, 2);
  Serial.println(" deg");

  Serial.println("Angle reset to 0 deg.");
}


void setup() {
  Serial.begin(9600);
  delay(1000);;

  // START IMU
  if (!IMU.begin()) {
    Serial.println("Failed to initialize IMU!");
    while (1);
  }
  Serial.println("IMU initialized!");

  // Calibrate gyro and set current position to 0 deg
  calibrateGyro();
  zeroAngle();
  
  // Start BLE
  if (!BLE.begin()) {
    Serial.println("Failed to initialize BLE!");
    while (1);
  }
  BLE.setLocalName("IMUArduino");
  BLE.setDeviceName("IMUArduino");
  BLE.setAdvertisedService(imuService);
  imuService.addCharacteristic(dataCharacteristic);
  imuService.addCharacteristic(resetCharacteristic);
  BLE.addService(imuService);
  dataCharacteristic.writeValue("0.00,0.00");
  BLE.advertise();
  Serial.println();
  Serial.println("BLE started!");
  Serial.println("Waiting for connection...");
}

void loop() {
  BLEDevice central = BLE.central();
  if (central) {
    Serial.print("Connected to: ");
    Serial.println(central.address());
    previousTime = micros();
    
    while (central.connected()) {
      
      if (resetCharacteristic.written()) {
        String command = resetCharacteristic.value();
        command.trim();
        
        if (command == "RESET") {
          Serial.println();
          Serial.println("RESET RECEIVED");
          calibrateGyro();
          zeroAngle();
          dataCharacteristic.writeValue("0.00,0.00");
        }
      }
      
      // Read IMU Data and calculate angle
      if (IMU.gyroscopeAvailable()) {
        float gx, gy, gz;
        IMU.readGyroscope(gx, gy, gz);
        
        unsigned long currentTime = micros();
        float dt = (currentTime - previousTime) / 1000000.0;
        previousTime = currentTime;

        // Filter invalid dt values
        if (dt <= 0 || dt > 0.1) {
          delay(10);
          continue;
        }

        // Remove gyro bias
        float correctedGX = gx - gyroBiasX;
        float correctedGY = gy - gyroBiasY;
        float correctedGZ = gz - gyroBiasZ;

        // Gyro deadband: ignore small values to reduce noise
        if (abs(correctedGY)< GYRO_DEADBAND) {
          correctedGY = 0.0;
        }

        // Gyro Y angle
        float gyroAngleY = angleY + correctedGY * dt;
        
        // Read accelerometer
        float ax, ay, az;
        if (IMU.accelerationAvailable()) {
          IMU.readAcceleration(ax,ay,az);
          
          // Y angle relative to starting position
          float rawAccelY = atan2(-ax, az) * 180.0 / PI;
          float accelAngleY = rawAccelY - accelZeroY;
          float accelMagnitude = sqrt(ax * ax + ay * ay + az * az);

          // Determine if we're mostly rotating around Y axis
          bool mostlyYRotation = abs(correctedGY) >= abs(correctedGX) && abs(correctedGY) >= abs(correctedGZ);

          // COMPLEMENTARY FILTER
          // Accelerometer is trusted only when:
          //
          // 1. acceleration is near gravity
          // 2. we're mostly rotating around Y
          //
          // Otherwise keep gyro-Y estimate.

          bool accelValid = accelMagnitude > 0.85 && accelMagnitude < 1.15;
          if (accelValid && ( mostlyYRotation || abs(correctedGY) < 1.0)
          ) {
            angleY = ALPHA * gyroAngleY + (1.0 - ALPHA) * accelAngleY;
          }

          else {
            angleY = gyroAngleY;
          }

          char data[50];
          snprintf(data, sizeof(data), "%.2f,%.2f", angleY, correctedGY);

          dataCharacteristic.writeValue(data);

          // SERIAL DEBUGGING
          Serial.print("Angle Y: ");
          Serial.print(angleY, 2);

          Serial.print(" | GY: ");
          Serial.print(correctedGY, 2);

          Serial.print(" | GX: ");
          Serial.print(correctedGX, 2);

          Serial.print(" | GZ: ");
          Serial.print(correctedGZ, 2);

          Serial.print(" | Acc Y: ");
          Serial.print(accelAngleY, 2);

          Serial.print(" | Acc mag: ");
          Serial.println(accelMagnitude, 3);
        }
      }
      delay(10);
    }
    Serial.println();
    Serial.println("BLE disconnected.");
    Serial.println("Waiting for connection...");
  }
}

