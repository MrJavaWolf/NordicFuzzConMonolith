import socket
import struct
from typing import Optional
import numpy as np

class E131Sender:
    """
    E1.31 / sACN DMX sender using a persistent UDP socket.
    """

    E131_PORT: int = 5568
    DMX_CHANNELS_PER_UNIVERSE: int = 512

    ROOT_VECTOR = 0x00000004
    FRAMING_VECTOR = 0x00000002
    DMP_VECTOR = 0x02

    def __init__(self, target_ip: str, start_universe: int = 1) -> None:
        self.target_ip: str = target_ip
        self.start_universe: int = start_universe
        self.sock: Optional[socket.socket] = None
        self._sequence: int = 1  # DMX sequence number
        self._packet_buffers: dict[int, bytearray] = {}

    def __enter__(self) -> "E131Sender":
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:
        if self.sock:
            self.sock.close()
            self.sock = None

    def _get_dmx_packet_buffer(self, universe: int) -> bytearray:
        """
        Get or create a reusable sACN packet buffer for a universe
        """
        if universe not in self._packet_buffers:
            # E1.31 header + DMX data
            packet = bytearray(126 + self.DMX_CHANNELS_PER_UNIVERSE)
            self._packet_buffers[universe] = packet
        return self._packet_buffers[universe]

    def _send_as_dmx(self, universe: int, rgb_view: memoryview) -> None:
        if self.sock is None:
            raise RuntimeError("E131Sender must be used within a 'with' block")

        packet = self._get_dmx_packet_buffer(universe)
        total_length = 126 + self.DMX_CHANNELS_PER_UNIVERSE

        # Root Layer
        struct.pack_into(">HHI12sH12s", packet, 0,
                         0x0010 | (total_length & 0x0FFF),  # PDU Length
                         0x0000,                             # Flags/Length
                         self.ROOT_VECTOR,                   # Vector
                         b"ASC-E1.17\x00\x00\x00\x00\x00\x00",  # CID (placeholder)
                         0,                                  # Options / Reserved
                         b'\x00' * 12)                       # Reserved

        # Framing Layer
        struct.pack_into(">I64sHBBH", packet, 38,
                         self.FRAMING_VECTOR,  # Vector
                         b"E1.31 Python Sender".ljust(64, b'\x00'),  # Source Name
                         0,                   # Priority
                         self._sequence,      # Sequence
                         0,                   # Options
                         universe)            # Universe

        # DMP Layer
        struct.pack_into(">BBBHB", packet, 115,
                         self.DMP_VECTOR,      # Vector
                         0xa1,                 # Address Type & Data Type
                         0x00,                 # First Property Address Hi
                         0x00,                 # First Property Address Lo
                         0x01)                 # Start Code
        # DMX data
        packet[126:] = rgb_view.tobytes()[:self.DMX_CHANNELS_PER_UNIVERSE]

        # Send
        self.sock.sendto(packet, (self.target_ip, self.E131_PORT))

        # Increment sequence
        self._sequence = (self._sequence + 1) % 256

    def send(self, frame: np.ndarray) -> None:
        if self.sock is None:
            raise RuntimeError("E131Sender must be used within a 'with' block")

        if frame.dtype != np.uint8:
            raise RuntimeError("Frame must be uint8")
        if not frame.flags["C_CONTIGUOUS"]:
            raise RuntimeError("Frame must be C_CONTIGUOUS")

        rgb_view = memoryview(frame).cast("B")
        total_channels = frame.size
        universe_count = (total_channels + self.DMX_CHANNELS_PER_UNIVERSE - 1) // self.DMX_CHANNELS_PER_UNIVERSE

        for i in range(universe_count):
            start = i * self.DMX_CHANNELS_PER_UNIVERSE
            end = start + self.DMX_CHANNELS_PER_UNIVERSE
            chunk_view = rgb_view[start:end]
            self._send_as_dmx(self.start_universe + i, chunk_view)


# Example usage
if __name__ == "__main__":
    height, width, channels = 512, 512, 3
    frame = np.zeros((height, width, channels), dtype=np.uint8)
    y = np.arange(height)[:, None]
    x = np.arange(width)[None, :]
    frame[..., 0] = x % 256
    frame[..., 1] = y % 256
    frame[..., 2] = (x + y) % 256

    with E131Sender("127.0.0.1", start_universe=1) as sender:
        sender.send(frame)
        print("Sent dummy frame via E1.31")
