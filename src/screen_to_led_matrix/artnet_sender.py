import socket
import struct
from typing import Optional
import numpy as np
import time


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
    PACKET_HEADER_SIZE = 18

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

        # Prebuilt header template (everything except universe + data)
        self._base_header = bytearray(self.PACKET_HEADER_SIZE)

        # Fixed fields
        self._base_header[0:8] = self.ARTNET_HEADER
        struct.pack_into("<H", self._base_header, 8, self.OPCODE_OUTPUT)                # Opcode (LE)
        struct.pack_into(">H", self._base_header, 10, self.PROTOCOL_VERSION)            # Protocol version (BE)    
        self._base_header[12] = 0                                                       # Sequence (0 = auto)
        self._base_header[13] = 0                                                       # Physical port
        struct.pack_into(">H", self._base_header, 16, self.DMX_CHANNELS_PER_UNIVERSE)   # Data length

        # Packet buffers reused per universe index
        self._packet_buffers: dict[int, bytearray] = {}

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

    def _get_dmx_packet_buffer(self, universe: int) -> bytearray:
        """
        Get or create a reusable dmx packet buffer for a universe
        """
        if universe not in self._packet_buffers:
            # Allocate header + 512-byte payload
            packet = bytearray(self.PACKET_HEADER_SIZE + self.DMX_CHANNELS_PER_UNIVERSE)
            packet[:self.PACKET_HEADER_SIZE] = self._base_header
            struct.pack_into("<H", packet, 14, universe)
            self._packet_buffers[universe] = packet
        return self._packet_buffers[universe]
    

    def _send_as_dmx(self, universe: int, rgb_view: memoryview) -> None:
        """
        Send the RGB data as a DMX
        """
        if self.sock is None:
            raise RuntimeError("ArtNetSender must be used within a 'with' block")

        dmx_packet = self._get_dmx_packet_buffer(universe)

        # Copy rgb data into dmx packet
        dmx_packet_view = memoryview(dmx_packet)[self.PACKET_HEADER_SIZE:]
        dmx_packet_view[:len(rgb_view)] = rgb_view
        if len(rgb_view) < self.DMX_CHANNELS_PER_UNIVERSE:
            dmx_packet_view[len(rgb_view):] = b"\x00" * (self.DMX_CHANNELS_PER_UNIVERSE - len(rgb_view))

        # Send entire packet
        self.sock.sendto(dmx_packet, (self.target_ip, self.ARTNET_PORT))


    def send(self, frame: np.ndarray) -> None:
        """
        Send a full image frame via Art-Net.
        """
        if self.sock is None:
            raise RuntimeError("ArtNetSender must be used within a 'with' block")

        if frame.dtype != np.uint8:
            raise RuntimeError("The frame is expected to be of type uint8")
        
        if not frame.flags["C_CONTIGUOUS"]:
            raise RuntimeError("The frame is expected to be C_CONTIGUOUS")

        rgb_view = memoryview(frame).cast("B")
        total_channels = frame.size
        universe_count = (total_channels + self.DMX_CHANNELS_PER_UNIVERSE - 1) // self.DMX_CHANNELS_PER_UNIVERSE

        for i in range(universe_count):
            start = i * self.DMX_CHANNELS_PER_UNIVERSE
            end = start + self.DMX_CHANNELS_PER_UNIVERSE
            chunk_view = rgb_view[start:end]
            self._send_as_dmx(self.start_universe + i, chunk_view)

# Example code
if __name__ == "__main__":
    # Create a dummy RGB frame
    height, width, channels = 512, 512, 3
    frame = np.zeros((height, width, channels), dtype=np.uint8)

    # Fill with a gradient pattern for testing
    y = np.arange(height)[:, None]
    x = np.arange(width)[None, :]
    frame[..., 0] = x % 256 # Red
    frame[..., 1] = y % 256 # Green
    frame[..., 2] = (x + y) % 256 # Blue

    # Send via Art-Net
    #with ArtNetSender("127.0.0.1", start_universe=0) as sender:
    with ArtNetSender("10.2.60.235", start_universe=0) as sender:
        
        counter = 0
        while True:
            frame[..., 0] = (x + counter) % 256 # Red
            frame[..., 1] = (y + counter) % 256 # Green
            frame[..., 2] = (x + y + counter) % 256 # Blue
            sender.send(frame)
            time.sleep(0.01)
            counter = counter + 1

#        sender.send(frame)

        print(f"Sent dummy frame via Art-Net")

