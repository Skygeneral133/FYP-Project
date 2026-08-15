# Load Cell

## Requirements

* Arduino with BLE support
* HX710 load cell amplifier
* Load cell
* ArduinoBLE library — download **ArduinoBLE** from the Arduino IDE Library Manager.

## Files

### Arduino Code

Reads the load cell through the HX710, performs tare and calibration, averages the readings, and calculates the weight in grams.

### Bluetooth Arduino Code

Creates a Bluetooth Low Energy (BLE) connection and sends the calculated weight wirelessly to the computer.

### `index.html`

Provides a web interface that connects to the Arduino through Web Bluetooth and displays the received weight in grams.

## Running the Web Interface

```bash
python -m http.server 8000
```

Then open:

```text
http://localhost:8000
```
