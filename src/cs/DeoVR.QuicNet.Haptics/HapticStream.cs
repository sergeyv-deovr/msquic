using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeoVR.QuicNet.Core;
using Google.Protobuf;

namespace DeoVR.QuicNet.Haptics
{
    public class HapticStream : QuicStreamEventHandler
    {
        private readonly string _jwtKey;

        private readonly ConcurrentQueue<HapticFrame> _incomingFrames = new ConcurrentQueue<HapticFrame>();
        private readonly byte[] _frameBuffer = new byte[4096];

        private Thread _processingThread;
        private EventWaitHandle _threadSignal = new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool _shutdown = false;

        public int FramesCount => _incomingFrames.Count;

        public HapticStream(string jwtKey)
        {
            _jwtKey = jwtKey;
            _processingThread = new Thread(ThreadFunc);
            _processingThread.IsBackground = true;
            _processingThread.Start();
        }

        protected override void Disposing()
        {
            _shutdown = true;
            _threadSignal.Set();
            _processingThread.Join();

            _threadSignal.Dispose();
            _threadSignal = null;
            _processingThread = null;
        }

        public override QuicStatus StartComplete(QuicStatus startStatus)
        {
            if (startStatus.IsFailure())
                return QuicStatus.ABORTED;

            var hapticFrame = new HapticFrame
            {
                FrameType = FrameType.Subscription,
                Data = new Auth { JwtToken = _jwtKey }.ToByteArray()
            };

            var quicFrame = hapticFrame.AsQuicMessage();
            Stream.Send(quicFrame);
            return QuicStatus.SUCCESS;
        }

        public override void CloseInitiated() { }

        public override QuicStatus Receive(Span<byte> bytes)
        {
            var status = base.Receive(bytes);
            if (status.IsFailure())
                return status;

            // Notify processing thread to parse new data
            _threadSignal.Set();
            return QuicStatus.SUCCESS;
        }

        public bool ReadNextFrame(out HapticFrame frame)
        {
            return _incomingFrames.TryDequeue(out frame);
        }

        private void ThreadFunc(object _)
        {
            try
            {
                while (!_shutdown)
                {
                    while (ReceiveStream.Length > 0)
                    {
                        if (_shutdown) return;

                        var frame = ParseStreamData();
                        _incomingFrames.Enqueue(frame);
                    }
                    _threadSignal.WaitOne();
                }
            }
            catch (Exception e)
            {
                UnhandledExceptionInternal(e);
            }
        }

        private HapticFrame ParseStreamData()
        {
            var frame = new HapticFrame { FrameType = FrameType.Unknown };

            var read = ReceiveStream.Read(_frameBuffer, 0, 1);
            if (read == 1)
                frame.FrameType = (FrameType)_frameBuffer[0];

            if (frame.FrameType == FrameType.Unknown)
                throw new Exception("unknown frame type");

            read = ReceiveStream.Read(_frameBuffer, 0, 2);
            if (read != 2)
                throw new Exception("unable to read frame length");

            var lengthSpan = _frameBuffer.AsSpan(0, 2);
            if (!BitConverter.IsLittleEndian)
                lengthSpan.Reverse();

            var length = BitConverter.ToUInt16(lengthSpan.ToArray(), 0);
            if (length <= HapticFrame.HEADER_LENGTH)
                throw new Exception($"invalid frame length: {length}");

            length -= HapticFrame.HEADER_LENGTH;
            read = ReceiveStream.Read(_frameBuffer, 0, length);
            if (read < length)
                throw new Exception($"failed to read {length} bytes, received only {read}");

            frame.Data = _frameBuffer.AsSpan(0, length).ToArray();
            return frame;
        }
    }
}
