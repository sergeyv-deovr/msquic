using Microsoft.Quic;

namespace DeoVR.QuicNet.Core
{
    public class QuicStreamSettings
    {
        public QUIC_STREAM_START_FLAGS StartFlags { get; set; } = QUIC_STREAM_START_FLAGS.NONE;

        public QUIC_STREAM_OPEN_FLAGS OpenFlags { get; set; } = QUIC_STREAM_OPEN_FLAGS.NONE;
    }
}
