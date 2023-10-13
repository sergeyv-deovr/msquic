using System;
using DeoVR.QuicNet.Data;

namespace DeoVR.QuicNet.Haptics
{
    public class HapticFrame
    {
        public const int HEADER_LENGTH = 3;
        public const int MAX_DATA_LENGTH = 1021;
        public const int MAX_FRAME_LENGTH = 1024;

        public HapticFrame() { }

        public FrameType FrameType { get; set; }
        public byte[] Data { get; set; }

        /// <summary>
        /// Buffer: 0x01, 0x06, 0x00, 0x01, 0x02, 0x03
        /// HapticFrame{
        /// Type: entities.FrameType_SUBSCRIPTION,
        /// Data: [] byte{1, 2, 3},
        /// }
        /// </summary>
        public QuicMessage AsQuicMessage()
        {
            var length = (ushort)(Data.Length + HEADER_LENGTH);
            var buffer = new QuicMessage(length);

            buffer.Data.Data[0] = (byte)FrameType;
            var lengthBytes = BitConverter.GetBytes(length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);
            lengthBytes.AsSpan().CopyTo(buffer.Data.Data.AsSpan(1));
            Data.AsSpan().CopyTo(buffer.Data.Data.AsSpan(HEADER_LENGTH));
            return buffer;
        }

        public Signal AsSignal() => Signal.Parser.ParseFrom(Data);
    }
}
