#!/usr/bin/env python3
"""Simple USB serial recorder for the pouring experiment.

CSV columns:
    elapsed_s, angle_y_deg, gy_deg_s, weight_g, pouring_rate_g_s

Install once:
    py -m pip install pyserial

Close both Arduino Serial Monitor windows, then run:
    py record_pouring.py

Press Ctrl+C to stop.
"""

import csv
import queue
import re
import threading
import time
from datetime import datetime
from pathlib import Path

import serial


# Current Arduino connections
IMU_PORT = "COM4"
IMU_BAUD = 9600

LOAD_PORT = "COM3"
LOAD_BAUD = 115200


# Examples received from the Arduinos:
# Angle Y: 12.34 | GY: 1.20 | GX: 0.00 | GZ: 0.00 | ...
# Weight: 25.67 g    Pouring Rate: 3.21 g/s
NUMBER = r"[-+]?(?:\d+(?:\.\d*)?|\.\d+)"
IMU_PATTERN = re.compile(
    rf"Angle\s*Y:\s*(?P<angle>{NUMBER})\s*\|\s*GY:\s*(?P<gy>{NUMBER})",
    re.IGNORECASE,
)
LOAD_PATTERN = re.compile(
    rf"Weight:\s*(?P<weight>{NUMBER})\s*g\s*"
    rf"Pouring\s*Rate:\s*(?P<rate>{NUMBER})\s*g\s*/\s*s",
    re.IGNORECASE,
)


latest_imu = None
imu_lock = threading.Lock()
load_readings = queue.Queue()
stop_event = threading.Event()


def read_imu(imu_serial):
    """Receive IMU lines and keep the latest pouring angle and Y velocity."""
    global latest_imu

    while not stop_event.is_set():
        try:
            line = imu_serial.readline().decode("utf-8", errors="ignore").strip()
        except serial.SerialException:
            break

        match = IMU_PATTERN.search(line)
        if match:
            reading = (
                float(match.group("angle")),
                float(match.group("gy")),
            )
            with imu_lock:
                latest_imu = reading


def read_load_cell(load_serial):
    """Receive load-cell lines and queue every weight/rate measurement."""
    while not stop_event.is_set():
        try:
            line = load_serial.readline().decode("utf-8", errors="ignore").strip()
        except serial.SerialException:
            break

        match = LOAD_PATTERN.search(line)
        if match:
            load_readings.put(
                (
                    float(match.group("weight")),
                    float(match.group("rate")),
                )
            )


def main():
    # Ask for the filename before opening either COM port.
    title = input("Enter a title for the recording (example: Deg_80_Volume_40): ").strip()
    safe_title = re.sub(r"[^A-Za-z0-9_-]+", "_", title).strip("_")
    if not safe_title:
        safe_title = datetime.now().strftime("%Y%m%d_%H%M%S")

    print("Close both Arduino Serial Monitor windows before running this program.")
    print(f"Opening IMU: {IMU_PORT} at {IMU_BAUD} baud")
    print(f"Opening load cell: {LOAD_PORT} at {LOAD_BAUD} baud")

    try:
        imu_serial = serial.Serial(IMU_PORT, IMU_BAUD, timeout=0.25)
        load_serial = serial.Serial(LOAD_PORT, LOAD_BAUD, timeout=0.25)
    except serial.SerialException as error:
        print(f"Could not open a COM port: {error}")
        return

    imu_thread = threading.Thread(target=read_imu, args=(imu_serial,), daemon=True)
    load_thread = threading.Thread(
        target=read_load_cell,
        args=(load_serial,),
        daemon=True,
    )
    imu_thread.start()
    load_thread.start()

    print("Waiting for IMU and load-cell data...")

    try:
        while True:
            with imu_lock:
                imu_ready = latest_imu is not None
            if imu_ready and not load_readings.empty():
                break
            time.sleep(0.05)

        # Remove measurements collected before recording officially starts.
        while not load_readings.empty():
            try:
                load_readings.get_nowait()
            except queue.Empty:
                break

        output_folder = Path(__file__).resolve().parent / "pouring_records"
        output_folder.mkdir(parents=True, exist_ok=True)
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        output_file = output_folder / f"pouring_{safe_title}.csv"

        # Do not overwrite an earlier recording that has the same title.
        if output_file.exists():
            output_file = output_folder / f"pouring_{safe_title}_{timestamp}.csv"

        start_time = time.monotonic()
        rows_saved = 0

        with output_file.open("w", newline="", encoding="utf-8") as file:
            writer = csv.writer(file)
            writer.writerow(
                [
                    "elapsed_s",
                    "angle_y_deg",
                    "gy_deg_s",
                    "weight_g",
                    "pouring_rate_g_s",
                ]
            )

            print(f"Recording to: {output_file}")
            print("Press Ctrl+C to stop and save.\n")

            while True:
                # One CSV row is written for every load-cell serial reading.
                try:
                    weight, pouring_rate = load_readings.get(timeout=0.5)
                except queue.Empty:
                    continue

                with imu_lock:
                    angle_y, gy = latest_imu

                elapsed = time.monotonic() - start_time
                writer.writerow(
                    [
                        f"{elapsed:.3f}",
                        f"{angle_y:.2f}",
                        f"{gy:.2f}",
                        f"{weight:.2f}",
                        f"{pouring_rate:.2f}",
                    ]
                )
                file.flush()
                rows_saved += 1

                print(
                    f"{elapsed:8.3f} s | Angle: {angle_y:7.2f} deg | "
                    f"GY: {gy:7.2f} deg/s | Weight: {weight:8.2f} g | "
                    f"Rate: {pouring_rate:7.2f} g/s"
                )

    except KeyboardInterrupt:
        print("\nRecording stopped.")
    finally:
        stop_event.set()
        imu_serial.close()
        load_serial.close()

    if "output_file" in locals():
        print(f"Saved {rows_saved} rows to: {output_file}")


if __name__ == "__main__":
    main()
