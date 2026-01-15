import time
import numpy as np
import mss
import cv2
from typing import Dict, Optional

class ScreenCapture:
    """
    Screen capture utility using MSS.

    This class must be used as a context manager (with-statement)
    to ensure the underlying MSS resources are properly released.

    Example:
        region = {"top": 0, "left": 0, "width": 512, "height": 512}
        with ScreenCapture(region) as capturer:
            frame = capturer.capture()
    """


    def __init__(self, capture_region: Dict[str, int]) -> None:
        """
        Initialize the screen capture.

        Args:
            capture_region:
                Dictionary defining the screen region to capture.
                Required keys:
                    - "top": top-left y coordinate
                    - "left": top-left x coordinate
                    - "width": capture width in pixels
                    - "height": capture height in pixels
        """
        
        self.capture_region: Dict[str, int] = capture_region
        self.sct: Optional[mss.mss] = None

    def __enter__(self) -> None:
        self.sct = mss.mss()
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:
        if self.sct is not None:
            self.sct.close()
            self.sct = None

    def capture(self) -> np.ndarray:
        """
        Capture the current screen region.

        Returns:
            A NumPy array with shape (H, W, 4) in BGRA format.

        Raises:
            RuntimeError:
                If called outside of a 'with ScreenCapture(...)' block.
        """
        if self.sct is None:
            raise RuntimeError("ScreenCapturer must be used within a 'with' block")

        img = self.sct.grab(self.capture_region)

        # BGRA → NumPy array
        frame = np.asarray(img, dtype=np.uint8)

        return frame
    
    def set_capture_region(self, capture_region: Dict[str, int]) -> None:
        """Hot-swap the capture region."""
        self.capture_region = capture_region

## Example code for using ScreenCapturer
def main():
    capture_region = {
        "top": 0,
        "left": 0,
        "width": 512,
        "height": 512
    }
    with ScreenCapture(capture_region) as screen_capture:
        prev_time = time.perf_counter()
        frame_count = 0

        while True:
            # Capture screen
            img = screen_capture.capture()

            # Drop alpha channel (BGRA → BGR)
            frame = img[:, :, :3]

            # ---- Optional processing here ----
            frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

            # Display (comment out if benchmarking raw capture speed)
            cv2.imshow("Screen", frame)

            # ESC or Window close (X) exit 
            if cv2.waitKey(1) & 0xFF == 27 or \
                cv2.getWindowProperty("Screen", cv2.WND_PROP_VISIBLE) < 1: 
                break

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
