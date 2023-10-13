using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using DeoVR.QuicNet.Core;
using Microsoft.Quic;

namespace DeoVR.QuicNet.Data
{
    /// <summary>
    /// Managed wrapper for <see cref="QUIC_BUFFER"/> to simplify sending and receiving data over <see cref="QuicStream"/>
    /// </summary>
    public class QuicMessage : IDisposable
    {
        private PinnedObject<long>? _messageId = null;

        public DataPointer<byte[]>? Data { get; private set; }

        internal DataPointer<QUIC_BUFFER>? Buffer { get; private set; }

        /// <summary>
        /// Native pointer to <see cref="QUIC_BUFFER"/>
        /// </summary>
        internal unsafe QUIC_BUFFER* Ptr => (QUIC_BUFFER*)Buffer?.DataPtr;

        /// <summary>
        /// Message id for passing through native callbacks
        /// </summary>
        internal PinnedObject<long>? MessageId
        {
            get => _messageId;
            set
            {
                _messageId?.Dispose();
                _messageId = value;
            }
        }

        public QuicMessage(uint size)
        {
            Data = new DataPointer<byte[]>(new byte[size]);
            unsafe
            {
                Buffer = new DataPointer<QUIC_BUFFER>(new QUIC_BUFFER
                {
                    Buffer = (byte*)Data.DataPtr,
                    Length = size
                });
            }
        }

        public void Dispose()
        {
            Buffer?.Dispose();
            Buffer = null;

            Data?.Dispose();
            Data = null;

            _messageId?.Dispose();
            _messageId = null;
        }
    }
}
