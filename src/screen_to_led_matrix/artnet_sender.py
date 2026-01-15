import socket
import struct
from typing import Optional
import numpy as np

class ArtNetSender:
    """
    Art-Net ArtDMX sender using a persistent UDP socket.
    """

    # Art-Net constants
    ARTNET_PORT: int = 6454
    ARTNET_HEADER: bytes = b"Art-Net\x00"
    OPCODE_OUTPUT: int = 0x5000              # ArtDMX opcode
    PROTOCOL_VERSION: int = 14               # Art-Net 4 spec
    DMX_CHANNELS_PER_UNIVERSE: int = 512

    def __init__(self, target_ip: str, start_universe: int = 0) -> None:
        """
        Initialize the Art-Net sender.

        Args:
            target_ip:
                Destination IP address for Art-Net packets
                (e.g. broadcast, localhost, or lighting node IP).
            start_universe:
                Universe index used for the first packet.
                Additional universes increment from this value.
        """
        self.target_ip: str = target_ip
        self.start_universe: int = start_universe
        self.sock: Optional[socket.socket] = None

    def __enter__(self) -> "ArtNetSender":
        """
        Enter the context manager and open the UDP socket.

        Returns:
            The active ArtNetSender instance.
        """
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:
        """
        Exit the context manager and close the UDP socket.
        """
        if self.sock is not None:
            self.sock.close()
            self.sock = None

    def _make_artdmx_packet(self, universe: int, data: bytes) -> bytes:
        """
        Build an ArtDMX packet for a given universe and DMX payload.

        Args:
            universe:
                Art-Net universe number (0–32767).
            data:
                DMX channel data (0–512 bytes).

        Returns:
            A complete ArtDMX packet ready for transmission.
        """
        if len(data) > 512:
            raise ValueError("DMX data must not exceed 512 bytes")

        packet = self.ARTNET_HEADER
        packet += struct.pack("<H", self.OPCODE_OUTPUT)         # Opcode (LE)
        packet += struct.pack(">H", self.PROTOCOL_VERSION)      # Protocol version (BE)
        packet += struct.pack("B", 0)                           # Sequence (0 = auto)
        packet += struct.pack("B", 0)                           # Physical port
        packet += struct.pack("<H", universe)                   # Universe (LE)
        packet += struct.pack(">H", len(data))                  # Length (BE)
        packet += data

        return packet

    def _send_dmx(self, universe: int, data: bytes) -> None:
        """
        Send DMX data to the configured Art-Net target.

        Args:
            universe:
                Art-Net universe number.
            data:
                DMX channel data (0–512 bytes).
        """

        if self.sock is None:
            raise RuntimeError("ArtNetSender must be used within a 'with' block")

        packet = self._make_artdmx_packet(universe, data)
        self.sock.sendto(packet, (self.target_ip, self.ARTNET_PORT))


    def _frame_to_rgb_bytes(self, frame: np.ndarray) -> bytes:
        """
        Convert a frame into a flat RGB byte stream.

        Supported formats:
            - (H, W, 4) BGRA
            - (H, W, 3) BGR / RGB
        """
        if frame.ndim != 3:
            raise ValueError("Frame must be a 3D array")

        channels = frame.shape[2]

        if channels == 4:
            frame = frame[:, :, :3]  # drop alpha
        elif channels != 3:
            raise ValueError("Frame must have 3 or 4 channels")

        return frame.reshape(-1).astype(np.uint8).tobytes()


    def send(self, frame: np.ndarray) -> None:
        """
        Send a full image frame via Art-Net.
        """
        if self.sock is None:
            raise RuntimeError("ArtNetSender must be used within a 'with' block")

        rgb_bytes = self._frame_to_rgb_bytes(frame)

        total_channels = len(rgb_bytes)
        universe_count = (
            total_channels + self.DMX_CHANNELS_PER_UNIVERSE - 1
        ) // self.DMX_CHANNELS_PER_UNIVERSE

        for i in range(universe_count):
            start = i * self.DMX_CHANNELS_PER_UNIVERSE
            end = start + self.DMX_CHANNELS_PER_UNIVERSE
            chunk = rgb_bytes[start:end]

            # Pad last universe if needed
            if len(chunk) < self.DMX_CHANNELS_PER_UNIVERSE:
                chunk += b"\x00" * (self.DMX_CHANNELS_PER_UNIVERSE - len(chunk))

            self._send_dmx(self.start_universe + i, chunk)



# Example code
if __name__ == "__main__":
    # Create a dummy frame: 64x64 RGB image
    height, width, channels = 64 * 3, 64 * 3, 3
    frame = np.zeros((height, width, channels), dtype=np.uint8)

    # Fill with a gradient pattern for testing
    for y in range(height):
        for x in range(width):
            frame[y, x, 0] = x % 256      # Red
            frame[y, x, 1] = y % 256      # Green
            frame[y, x, 2] = (x + y) % 256  # Blue

    # Send via Art-Net
    with ArtNetSender("127.0.0.1", start_universe=0) as sender:
        sender.send(frame)

    print("Sent dummy frame via Art-Net.")

