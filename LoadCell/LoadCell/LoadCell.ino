#define HX710_DOUT 3
#define HX710_SCLK 2

// =====================================================
// CALIBRATION
// =====================================================

// Your current calibration factor
const float CAL_FACTOR = -12976.91;


// =====================================================
// SETTINGS
// =====================================================

// Number of readings used for each average
const int NUM_AVERAGE_READINGS = 5;


// =====================================================
// VARIABLES
// =====================================================

long zeroOffset = 0;


// =====================================================
// SETUP
// =====================================================

void setup() {

  Serial.begin(115200);

  pinMode(HX710_DOUT, INPUT);
  pinMode(HX710_SCLK, OUTPUT);

  digitalWrite(HX710_SCLK, LOW);

  Serial.println("Starting HX710...");

  delay(1000);


  // ---------------------------------------------------
  // TARE
  // ---------------------------------------------------

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

}


// =====================================================
// MAIN LOOP
// =====================================================

void loop() {

  // ---------------------------------------------------
  // TAKE MULTIPLE READINGS
  // ---------------------------------------------------

  long total = 0;

  for (int i = 0; i < NUM_AVERAGE_READINGS; i++) {

    total += readHX710();

    // Small delay between samples
    delay(10);
  }


  // ---------------------------------------------------
  // CALCULATE AVERAGE
  // ---------------------------------------------------

  long averageRaw = total / NUM_AVERAGE_READINGS;


  // ---------------------------------------------------
  // REMOVE TARE OFFSET
  // ---------------------------------------------------

  long taredValue = averageRaw - zeroOffset;


  // ---------------------------------------------------
  // CONVERT TO GRAMS
  // ---------------------------------------------------

  float weight = taredValue / CAL_FACTOR;


  // ---------------------------------------------------
  // OUTPUT
  // ---------------------------------------------------

  Serial.print("Raw Avg: ");
  Serial.print(averageRaw);

  Serial.print("    Tared: ");
  Serial.print(taredValue);

  Serial.print("    Weight: ");
  Serial.print(weight, 2);

  Serial.println(" g");

}


// =====================================================
// HX710 READ FUNCTION
// =====================================================

long readHX710() {

  // Wait until HX710 data is ready
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

    value = (value << 1) | digitalRead(HX710_DOUT);

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