from screen_capture import ScreenCapture
from artnet_sender import ArtNetSender
import time

SINGLE_PANEL_WIDTH = 64
SINGLE_PANEL_HEIGHT = 64
NUMBER_OF_HORIZONTAL_PANELS = 12
NUMBER_OF_VERTICAL_PANELS = 8
PIXEL_WIDTH = SINGLE_PANEL_WIDTH * NUMBER_OF_HORIZONTAL_PANELS
PIXEL_HEIGHT = SINGLE_PANEL_HEIGHT * NUMBER_OF_VERTICAL_PANELS
CAPTURE_REGION = {
    "top": 0,
    "left": 0,
    "width": PIXEL_WIDTH,
    "height": PIXEL_HEIGHT
}
CAPTURE_BRIGHTNESS = 0.8
ARTNET_IP = "127.0.0.1"
ARTNET_START_UNIVERSE = 1

# Set your target FPS here (-1 for uncapped)
TARGET_FPS = 25

def main():
    print(f"Starts capturing region: {CAPTURE_REGION} with brightness: {CAPTURE_BRIGHTNESS}")
    prev_time = time.perf_counter()
    frame_count = 0

    
    if TARGET_FPS > 0:
        frame_duration = 1.0 / TARGET_FPS
        print(f"Target FPS: {TARGET_FPS}")

    else:
        frame_duration = 0 
        print(f"Target FPS: Uncapped")


    with ScreenCapture(CAPTURE_REGION, CAPTURE_BRIGHTNESS) as screen_capture:
        with ArtNetSender(ARTNET_IP, start_universe=ARTNET_START_UNIVERSE) as artnet: 
            while True: 
                loop_start = time.perf_counter()
                frame = screen_capture.capture()
                artnet.send(frame)

                # FPS calculation
                frame_count += 1
                now = time.perf_counter()
                if now - prev_time > 1:
                    prev_time = now
                    print(f"FPS: {frame_count:.1f}")
                    frame_count = 0
                
                # Try and hit target fps
                if TARGET_FPS > 0:
                    elapsed = time.perf_counter() - loop_start
                    sleep_time = frame_duration - elapsed
                    if sleep_time > 0:
                        time.sleep(sleep_time)



if __name__ == "__main__":
    main()
