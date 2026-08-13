import time
import cv2

import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

from pymycobot import Pro450Client


# ----------------------------
# Robot connection (Pro 450)
# ----------------------------
ROBOT_IP = "192.168.0.232"  # Pro 450 default IP :contentReference[oaicite:1]{index=1}
ROBOT_PORT = 4500           # Pro 450 default port :contentReference[oaicite:2]{index=2}

mc = Pro450Client(ROBOT_IP, ROBOT_PORT)  # Official client :contentReference[oaicite:3]{index=3}

# Power on if needed (safe guard)
try:
    if mc.is_power_on() != 1:
        print("Robot is off -> powering on...")
        mc.power_on()
        time.sleep(1.0)
except Exception as e:
    print("WARNING: Could not query/power robot:", e)

# Optional: favor "fresh" mode so latest command wins (if supported)
try:
    mc.set_fresh_mode(1)  # described in Pro 450 API doc :contentReference[oaicite:4]{index=4}
except Exception:
    pass


# ----------------------------
# Gesture -> Pose mapping
# Angles are [J1..J6] in degrees
# IMPORTANT: These are arbitrary example poses.
# Replace with your own safe poses.
# ----------------------------
GESTURE_TO_ANGLES = {
    "Open_Palm":   [0,   0,   0,   0,   0,   0],
    "Closed_Fist": [0, -30,  45,   0,  30,   0],
    "Thumb_Up":    [20, -20,  20,  40,  20,   0],
    "Thumb_Down":  [-20, -20, 20, -40,  20,   0],
    "Victory":     [0,  -45,  70,  20,  45,  10],
    "Pointing_Up": [30, -10,  20,  60, -10,   0],
    "ILoveYou":    [-30, -35, 60,  10,  60, -10],
}

MOVE_SPEED = 25  # 1-100 typical (keep conservative)
CONF_THRESH = 0.55
COOLDOWN_S = 1.0  # prevent spamming robot


# ----------------------------
# MediaPipe Gesture Recognizer
# ----------------------------
MODEL_PATH = "./robot arm demo/gesture_recognizer.task"  # put file next to this script

base_options = python.BaseOptions(model_asset_path=MODEL_PATH)
options = vision.GestureRecognizerOptions(
    base_options=base_options,
    running_mode=vision.RunningMode.VIDEO,
    num_hands=1,
)
recognizer = vision.GestureRecognizer.create_from_options(options)


def send_pose(gesture_name: str):
    """Send a pose corresponding to gesture_name."""
    if gesture_name not in GESTURE_TO_ANGLES:
        return False

    angles = GESTURE_TO_ANGLES[gesture_name]
    print(f"-> Sending pose for {gesture_name}: {angles}")

    # Most pymycobot arms use send_angles(list, speed)
    # Pro450Client supports motion APIs; send_angles is commonly available in this family.
    # If yours errors, tell me the error text and we’ll swap to the correct Pro450 call.
    mc.send_angles(angles, MOVE_SPEED)
    return True


def main():
    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        raise RuntimeError("Could not open camera index 0")

    last_sent_gesture = None
    last_sent_time = 0.0

    print("Running. Press 'q' to quit.")
    print("Recognized gestures:", list(GESTURE_TO_ANGLES.keys()))

    while True:
        ok, frame = cap.read()
        if not ok:
            break

        # MediaPipe expects RGB
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame_rgb)
        timestamp_ms = int(time.time() * 1000)

        result = recognizer.recognize_for_video(mp_image, timestamp_ms)

        gesture_text = "None"
        conf_text = ""

        if result.gestures:
            top = result.gestures[0][0]  # [hand0][best]
            gesture_name = top.category_name
            score = float(top.score)

            gesture_text = gesture_name
            conf_text = f"{score:.2f}"

            now = time.time()
            if score >= CONF_THRESH:
                # cooldown + "only send when changes" logic
                if (gesture_name != last_sent_gesture) and ((now - last_sent_time) >= COOLDOWN_S):
                    if send_pose(gesture_name):
                        last_sent_gesture = gesture_name
                        last_sent_time = now

        # Simple overlay
        cv2.putText(frame, f"Gesture: {gesture_text} {conf_text}",
                    (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 2)

        cv2.imshow("Gesture -> Pro450", frame)
        if (cv2.waitKey(1) & 0xFF) == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()