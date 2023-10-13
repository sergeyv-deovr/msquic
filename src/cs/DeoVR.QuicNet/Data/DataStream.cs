using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace DeoVR.QuicNet.Data
{
    public class DataStream : Stream
    {
        private readonly ConcurrentQueue<byte[]> _queue = new ConcurrentQueue<byte[]>();

        private byte[]? _pendingData = null;
        private int _pendingDataPos = 0;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        /// <summary>
        /// Amount of pending buffers
        /// </summary>
        public override long Length => _queue.Count + (_pendingData != null ? 1 : 0);

        public override long Position { get => 0; set => throw new NotImplementedException(); }

        /// <summary>
        /// In milliseconds
        /// </summary>
        public override int ReadTimeout { get; set; } = 1000;

        public DataStream()
        {

        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var span = buffer.AsSpan(offset, count);
            var written = 0;
            var timeout = ReadTimeout;
            while (written < count)
            {
                if (_pendingData == null)
                {
                    _pendingDataPos = 0;
                    // Update pending data
                    while (timeout > 0)
                    {
                        if (_queue.TryDequeue(out _pendingData))
                            break;
                        timeout -= 100;
                        Thread.Sleep(100);
                    }
                    // Waiting timed out
                    if (_pendingData == null)
                        return written;
                }
                else
                {
                    // Read pending data
                    var pendingSpan = _pendingData.AsSpan(_pendingDataPos);
                    if (pendingSpan.Length <= span.Length)
                    {
                        var len = pendingSpan.Length;
                        pendingSpan.CopyTo(span);
                        written += len;
                        span = span.Slice(len);
                        _pendingData = null;
                        _pendingDataPos = 0;
                    }
                    else
                    {
                        var len = span.Length;
                        // Target buffer is smaller than pending data
                        pendingSpan.Slice(0, len).CopyTo(span);
                        written += len;
                        _pendingDataPos += len;
                        break;
                    }
                }

                timeout = 0;
            }

            return written;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        public void Write(Span<byte> buffer)
        {
            _queue.Enqueue(buffer.ToArray());
        }
    }
}
