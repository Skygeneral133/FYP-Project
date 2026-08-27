#include <Arduino_LSM6DS3.h>
#include <ArduinoBLE.h>

// BLE UUIDs
#define SERVICE_UUID        "19b10000-e8f2-537e-4f6c-d104768a1214"
#define CHARACTERISTIC_UUID "19b10001-e8f2-537e-4f6c-d104768a1214"

// BLE service and characteristic
BLEService imuService(SERVICE_UUID);

BLEFloatCharacteristic angleCharacteristic(
  CHARACTERISTIC_UUID,
  BLERead | BLENotify
);

// Angles
float angleX = 0.0;
float angleY = 0.0;
float angleZ = 0.0;

unsigned long previousTime;

void setup() {

  Serial.begin(9600);

  while (!Serial);

  // -------------------------
  // Start IMU
  // -------------------------
  if (!IMU.begin()) {
    Serial.println("Failed to initialize IMU!");
    while (1);
  }

  Serial.println("IMU initialized!");

  // -------------------------
  // Start BLE
  // -------------------------
  if (!BLE.begin()) {
    Serial.println("Failed to initialize BLE!");
    while (1);
  }

  // BLE device name
  BLE.setLocalName("IMUArduino");

  // Advertise our service
  BLE.setAdvertisedService(imuService);

  // Add characteristic
  imuService.addCharacteristic(angleCharacteristic);

  // Add service
  BLE.addService(imuService);

  // Initial angle
  angleCharacteristic.writeValue(0.0);

  // Start advertising
  BLE.advertise();

  Serial.println("BLE started!");
  Serial.println("Waiting for connection...");

  previousTime = micros();
}

void loop() {

  // Check for BLE connection
  BLEDevice central = BLE.central();

  if (central) {

    Serial.print("Connected to: ");
    Serial.println(central.address());

    while (central.connected()) {

      float gx, gy, gz;

      if (IMU.gyroscopeAvailable()) {

        IMU.readGyroscope(gx, gy, gz);

        // Calculate time difference
        unsigned long currentTime = micros();

        float dt =
          (currentTime - previousTime) / 1000000.0;

        previousTime = currentTime;

        // Integrate gyro
        angleX += gx * dt;
        angleY += gy * dt;
        angleZ += gz * dt;

        // Send Z angle through BLE
        angleCharacteristic.writeValue(angleZ);

        // Serial monitor
        Serial.print("X: ");
        Serial.print(angleX, 2);

        Serial.print(" | Y: ");
        Serial.print(angleY, 2);

        Serial.print(" | Z: ");
        Serial.print(angleZ, 2);

        Serial.println(" deg");
      }

      delay(10);
    }

    Serial.println("BLE disconnected.");
  }
}