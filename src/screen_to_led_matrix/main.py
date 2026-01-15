from screen_capture import ScreenCapture
from artnet_sender import ArtNetSender
import time

SINGLE_PANEL_WIDTH = 64
SINGLE_PANEL_HEIGHT = 64
NUMBER_OF_HORIZONTAL_PANELS = 12
NUMBER_OF_VERTICAL_PANELS = 12
PIXEL_WIDTH = SINGLE_PANEL_WIDTH * NUMBER_OF_HORIZONTAL_PANELS
PIXEL_HEIGHT = SINGLE_PANEL_HEIGHT * NUMBER_OF_VERTICAL_PANELS
CAPTURE_REGION = {
    "top": 0,
    "left": 0,
    "width": PIXEL_WIDTH,
    "height": PIXEL_HEIGHT
}
ARTNET_IP = "127.0.0.1"
ARTNET_START_UNIVERSE = 0

def main():
    print(f"Starts capturing region: {CAPTURE_REGION}")
    prev_time = time.perf_counter()
    frame_count = 0
    with ScreenCapture(CAPTURE_REGION) as screen_capture:
        with ArtNetSender(ARTNET_IP, start_universe=ARTNET_START_UNIVERSE) as artnet: 
            while True: 
                frame = screen_capture.capture()
                artnet.send(frame)
                # FPS calculation
                frame_count += 1
                if frame_count % 60 == 0:
                    now = time.perf_counter()
                    fps = 60 / (now - prev_time)
                    prev_time = now
                    print(f"FPS: {fps:.1f}")



if __name__ == "__main__":
    main()
