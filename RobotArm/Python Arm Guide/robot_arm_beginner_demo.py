"""
myCobot Pro 450 - Beginner Joint-Movement Demo

This script demonstrates how to:
1. Connect to a myCobot Pro 450 over the network.
2. Check and enable robot power.
3. Read the current joint angles and Cartesian coordinates.
4. Move the robot through a simple, editable joint-angle sequence.
5. stop the demonstration safely using Ctrl+C.

Before running:
- Confirm the robot controller is switched on.
- Confirm the computer is connected to the same network as the robot.
- Confirm the robot IP address and port below are correct.
- Clear the robot's workspace.
- Keep the emergency-stop button accessible.
- Begin with a low speed while testing new positions.

Install the Python package if required:
    pip install pymycobot

Run:
    python robot_arm_beginner_demo.py
"""

import signal
import time
from typing import Optional

from pymycobot import Pro450Client


# ============================================================
# USER SETTINGS
# ============================================================

ROBOT_IP = "192.168.0.232"
ROBOT_PORT = 4500

# pymycobot generally uses a speed range from 1 to 100.
# Start slowly when testing unfamiliar positions.
MOVE_SPEED = 30

# This is a simple time-based pause after each movement command.
# Increase it if the robot has not reached the target before the next step.
MOVE_DELAY_SECONDS = 3.0

AUTO_POWER_ON = True
PRINT_ROBOT_STATUS = True

# Each inner list contains the six robot joint angles in degrees:
# [J1, J2, J3, J4, J5, J6]
#
# Replace these examples only after confirming that the new positions
# are safe and reachable for the current robot setup.
JOINT_SEQUENCE = [
    [0, -90, 0, 90, 0, 0],       # Centre position
    [-30, -90, 0, 90, 0, 0],     # Rotate the base left
    [0, -90, 0, 90, 0, 0],       # Return to centre
    [30, -90, 0, 90, 0, 0],      # Rotate the base right
    [0, -90, 0, 90, 0, 0],       # Return to centre
]

INITIAL_POSITION = JOINT_SEQUENCE[0]


# ============================================================
# PROGRAM STATE
# ============================================================

stop_requested = False
robot: Optional[Pro450Client] = None


# ============================================================
# ROBOT CONNECTION AND STATUS
# ============================================================

def connect_robot() -> Pro450Client:
    """Create the robot client, check power, and report its current state."""
    arm = Pro450Client(ROBOT_IP, ROBOT_PORT)

    print(f"[INFO] Connecting to robot at {ROBOT_IP}:{ROBOT_PORT} ...")

    power_state = arm.is_power_on()
    print(f"[INFO] Robot power state: {power_state}")

    if power_state != 1:
        if not AUTO_POWER_ON:
            raise RuntimeError(
                "The robot is not powered on and AUTO_POWER_ON is disabled."
            )

        print("[INFO] Powering on the robot...")
        arm.power_on()
        time.sleep(3.0)

        power_state = arm.is_power_on()
        if power_state != 1:
            raise RuntimeError("The robot did not report a powered-on state.")

    if PRINT_ROBOT_STATUS:
        print_robot_status(arm, label="Initial robot status")

    return arm


def print_robot_status(arm: Pro450Client, label: str = "Robot status") -> None:
    """Print joint angles and Cartesian coordinates when available."""
    print(f"\n[INFO] {label}")

    try:
        angles = arm.get_angles()
        print(f"  Joint angles: {angles}")
    except Exception as error:
        print(f"  [WARN] Could not read joint angles: {error}")

    try:
        coordinates = arm.get_coords()
        print(f"  Coordinates:  {coordinates}")
    except Exception as error:
        print(f"  [WARN] Could not read Cartesian coordinates: {error}")


# ============================================================
# MOTION CONTROL
# ============================================================

def move_to_joint_position(
    arm: Pro450Client,
    target_angles: list[float],
    speed: int = MOVE_SPEED,
    wait_seconds: float = MOVE_DELAY_SECONDS,
) -> None:
    """
    Send one six-joint target to the robot.

    This beginner example waits for a fixed time after sending the command.
    More advanced software should check the robot's motion state or compare
    measured angles with the requested target.
    """
    if len(target_angles) != 6:
        raise ValueError(
            f"Expected six joint angles, but received {len(target_angles)}."
        )

    if not 1 <= speed <= 100:
        raise ValueError("Speed must be between 1 and 100.")

    print(f"[MOVE] Target angles: {target_angles}")
    print(f"[MOVE] Speed: {speed}")

    arm.send_angles(target_angles, speed)

    elapsed = 0.0
    while elapsed < wait_seconds:
        if stop_requested:
            print("[INFO] Stop requested while waiting for the movement.")
            return

        time.sleep(0.05)
        elapsed += 0.05


def move_to_initial_position(arm: Pro450Client) -> None:
    """Move the arm to the starting position used by this demonstration."""
    print("\n[INFO] Moving to the initial position...")
    move_to_joint_position(arm, INITIAL_POSITION)
    print("[INFO] Initial position command completed.")


def run_joint_demo(arm: Pro450Client) -> None:
    """Run each target in JOINT_SEQUENCE once."""
    global stop_requested

    stop_requested = False
    print("\n[INFO] Starting the joint-angle demonstration.")

    for step_number, target_angles in enumerate(JOINT_SEQUENCE, start=1):
        if stop_requested:
            print("[INFO] Demonstration stopped before the next movement.")
            break

        print(f"\n[INFO] Step {step_number} of {len(JOINT_SEQUENCE)}")
        move_to_joint_position(arm, target_angles)

    if stop_requested:
        print("[INFO] Demonstration ended early.")
    else:
        print("\n[INFO] Demonstration complete.")

    if PRINT_ROBOT_STATUS:
        print_robot_status(arm, label="Final robot status")


# ============================================================
# BEST-EFFORT STOP
# ============================================================

def stop_robot_motion(arm: Optional[Pro450Client]) -> None:
    """
    Try several stop methods because pymycobot versions can differ.

    A software stop is not a replacement for the physical emergency-stop
    button. Use the emergency stop immediately if the robot behaves unsafely.
    """
    if arm is None:
        return

    stop_methods = [
        "stop",
        "pause",
        "task_stop",
        "program_pause",
        "jog_stop",
    ]

    for method_name in stop_methods:
        method = getattr(arm, method_name, None)

        if callable(method):
            try:
                method()
                print(f"[INFO] Stop command sent using {method_name}().")
                return
            except Exception as error:
                print(f"[WARN] {method_name}() was unavailable: {error}")

    # Final fallback: request the currently measured angles again.
    # This may help hold the present position, but it is not a guaranteed
    # emergency stop.
    try:
        current_angles = arm.get_angles()

        if isinstance(current_angles, (list, tuple)) and len(current_angles) >= 6:
            hold_position = [float(angle) for angle in current_angles[:6]]
            arm.send_angles(hold_position, MOVE_SPEED)
            print("[INFO] Requested the current joint position as a hold target.")
            return
    except Exception as error:
        print(f"[WARN] Could not request a hold position: {error}")

    print("[WARN] No software stop method succeeded. Use the physical emergency stop.")


# ============================================================
# USER INPUT AND SHUTDOWN
# ============================================================

def handle_ctrl_c(signum, frame) -> None:
    """Set the stop flag when Ctrl+C is pressed."""
    global stop_requested

    stop_requested = True
    print("\n[WARN] Ctrl+C detected. Requesting a robot stop...")
    stop_robot_motion(robot)


def main() -> None:
    """Connect to the robot and provide a simple text-based menu."""
    global robot
    global stop_requested

    signal.signal(signal.SIGINT, handle_ctrl_c)

    try:
        robot = connect_robot()
        move_to_initial_position(robot)

        print("\nRobot arm beginner demo")
        print("-----------------------")
        print("GO      Run the example joint sequence")
        print("STATUS  Print current angles and coordinates")
        print("HOME    Return to the initial demo position")
        print("QUIT    Exit the program")
        print("\nPress Ctrl+C to request a software stop.")

        while True:
            try:
                command = input("\n> ").strip().upper()
            except EOFError:
                command = "QUIT"
            except KeyboardInterrupt:
                # The signal handler has already requested the stop.
                stop_requested = False
                continue

            if command == "GO":
                run_joint_demo(robot)

            elif command == "STATUS":
                print_robot_status(robot)

            elif command == "HOME":
                stop_requested = False
                move_to_initial_position(robot)

            elif command == "QUIT":
                print("[INFO] Exiting the demonstration.")
                break

            elif not command:
                continue

            else:
                print("[INFO] Unknown command. Use GO, STATUS, HOME, or QUIT.")

    except ConnectionError as error:
        print(f"[ERROR] Could not connect to the robot: {error}")
        print("[HELP] Check the robot IP address, port, power, and network connection.")

    except Exception as error:
        print(f"[ERROR] The program stopped because of an unexpected error: {error}")

    finally:
        stop_robot_motion(robot)
        print("[INFO] Program closed.")


if __name__ == "__main__":
    main()
