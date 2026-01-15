import time
import numpy as np
import mss
import cv2

DEFAULT_CAPTURE_SIZE = {
    "top": 0,
    "left": 0,
    "width": 512,
    "height": 512
}   

def main():
    with mss.mss() as sct:
        #monitor = sct.monitors[1]  # Primary monitor

        prev_time = time.perf_counter()
        frame_count = 0

        while True:
            # Capture screen
            img = sct.grab(DEFAULT_CAPTURE_SIZE)

            # Convert to NumPy array (BGRA)
            frame = np.asarray(img, dtype=np.uint8)

            # Drop alpha channel (BGRA → BGR)
            frame = frame[:, :, :3]

            # ---- Optional processing here ----
            frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

            # Display (comment out if benchmarking raw capture speed)
            #cv2.imshow("Screen", frame)

            #if cv2.waitKey(1) & 0xFF == 27:  # ESC to quit
             #   break

            # FPS calculation
            frame_count += 1
            if frame_count % 60 == 0:
                now = time.perf_counter()
                fps = 60 / (now - prev_time)
                prev_time = now
                print(f"FPS: {fps:.1f}")

        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
