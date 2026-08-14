# myCobot Pro 450 Beginner Python Demo

## Purpose

This example is a stripped-back introduction to controlling the myCobot Pro 450 from Python. It contains only robot-arm functionality. All Nicla Vision, IMU, camera, serial-port, frame-saving, CSV, image conversion, threading, and capture-session code has been removed.

The program demonstrates:

- connecting to the robot over the network;
- checking and enabling robot power;
- reading joint angles and Cartesian coordinates;
- moving to a known initial position;
- running a short joint-space movement sequence;
- responding to simple terminal commands; and
- requesting a best-effort software stop.

## 1. Safety before running

1. Clear people, tools, cables, and loose objects from the robot workspace.
2. Keep the physical emergency-stop button within reach.
3. Confirm that the example joint positions are safe for the installed tool and surrounding equipment.
4. Start with a low movement speed.
5. Do not rely on `Ctrl+C` as an emergency stop. It is only a software stop request.
6. Test unfamiliar positions one at a time before adding them to a sequence.

## 2. Requirements

The computer and robot controller must be connected to the same network.

Install Python and the robot library:

```bash
pip install pymycobot
```

The supplied example uses:

```python
from pymycobot import Pro450Client
```

## 3. Configure the connection

Edit these values near the top of the script:

```python
ROBOT_IP = "192.168.0.232"
ROBOT_PORT = 4500
```

The IP address must match the address configured on the robot controller.

## 4. Configure movement speed

```python
MOVE_SPEED = 30
```

The script expects a value from 1 to 100. A low value is recommended for first tests.

The program also uses a fixed wait after each command:

```python
MOVE_DELAY_SECONDS = 3.0
```

This is intentionally simple. It does not prove that the robot has reached the target. Increase the delay for larger or slower movements.

## 5. Understand joint-angle commands

A joint target contains six angles:

```python
[J1, J2, J3, J4, J5, J6]
```

For example:

```python
[0, -90, 0, 90, 0, 0]
```

This is a joint-space command. It specifies the angle of every joint rather than directly specifying the tool position.

The demonstration sequence is:

```python
JOINT_SEQUENCE = [
    [0, -90, 0, 90, 0, 0],
    [-30, -90, 0, 90, 0, 0],
    [0, -90, 0, 90, 0, 0],
    [30, -90, 0, 90, 0, 0],
    [0, -90, 0, 90, 0, 0],
]
```

Only the first joint changes in this introductory example. This makes the sequence easier to understand, but the positions must still be checked on the real robot.

## 6. Run the program

Save the script as:

```text
robot_arm_beginner_demo.py
```

Run it from a terminal:

```bash
python robot_arm_beginner_demo.py
```

The program connects, checks power, prints the initial status, and commands the initial demo position.

## 7. Terminal commands

### `GO`

Runs the complete `JOINT_SEQUENCE`.

### `STATUS`

Prints:

- the current six joint angles; and
- the current Cartesian coordinates returned by the robot.

### `HOME`

Returns to `INITIAL_POSITION`. In this script, “HOME” means the demonstration's chosen starting pose. It does not necessarily mean the manufacturer's mechanical zero or calibration pose.

### `QUIT`

Ends the program.

### `Ctrl+C`

Sets a stop flag and tries several possible pymycobot stop methods. The exact available method can vary with the installed pymycobot version. If no direct method succeeds, the script requests the current joint angles as a hold target.

This remains a best-effort software action. Use the physical emergency-stop button for unsafe motion.

## 8. Important functions

### `connect_robot()`

Creates the `Pro450Client`, reads the power state, optionally powers the robot on, and prints its current state.

### `print_robot_status()`

Uses:

```python
arm.get_angles()
arm.get_coords()
```

It catches read errors so that one unavailable status value does not immediately terminate the program.

### `move_to_joint_position()`

Validates that exactly six angles were supplied, validates the speed, sends the command, and waits while checking for a stop request.

### `run_joint_demo()`

Steps through `JOINT_SEQUENCE` and ends early when a stop is requested.

### `stop_robot_motion()`

Tries several stop method names because pymycobot versions and robot clients may expose different APIs. The fallback is not guaranteed to stop hazardous motion.

## 9. Add a new safe movement

First test one target by replacing the sequence temporarily:

```python
JOINT_SEQUENCE = [
    [0, -90, 0, 90, 0, 0],
    [10, -85, 0, 85, 0, 0],
]
```

Use a low speed and verify the movement physically. Once confirmed, additional targets can be added.

## 10. Example: repeat a movement

To repeat the same sequence twice:

```python
for repetition in range(2):
    print(f"Repetition {repetition + 1}")
    run_joint_demo(robot)
```

This can be placed in the `GO` branch, although beginners should first confirm that one full sequence is safe.

## 11. Example: read the final pose

```python
angles = robot.get_angles()
coordinates = robot.get_coords()

print("Measured joint angles:", angles)
print("Measured tool coordinates:", coordinates)
```

These readings are useful for recording a manually positioned pose or checking whether a command produced the expected result.

## 12. Limitations of this beginner example

- Movement completion is approximated using a time delay.
- It does not validate joint limits or collisions.
- It does not perform trajectory planning.
- It does not verify Cartesian path safety.
- It does not manage tool or gripper outputs.
- Its software stop behaviour depends on the installed pymycobot API.
- Its example positions are not guaranteed safe for every installation.

A more advanced version should use confirmed motion-state feedback, explicit joint-limit checks, collision-aware planning where available, and a stop method verified for the exact controller and pymycobot version.
