using System;
using System.Diagnostics;
using DeoVR.QuicNet.Data;
using Microsoft.Quic;

namespace DeoVR.QuicNet.Core
{
    /// <summary>
    /// Processes stream events.
    /// Reference: <see cref="QUIC_STREAM_EVENT_TYPE"/>
    /// <br/><br/>
    /// <strong>
    /// WARNING:<br/>
    /// These events are being called from a separate thread in native code.<br/>
    /// Be very careful with the implementation.
    /// </strong>
    /// </summary>
    public abstract class QuicStreamEventHandler : IDisposable
    {
        public QuicStream? Stream { get; set; }

        public DataStream ReceiveStream { get; } = new DataStream();

        public void Dispose()
        {
            ReceiveStream.Dispose();
            Disposing();
        }

        protected abstract void Disposing();

        public virtual void OpenInitiated() { }
        public virtual void CloseInitiated() { }

        public virtual void UnhandledException(Exception e) { }

        #region Stream events
        public virtual QuicStatus StartComplete(QuicStatus startStatus) { return QuicStatus.SUCCESS; }
        public virtual QuicStatus Receive(Span<byte> bytes)
        {
            try { ReceiveStream.Write(bytes); }
            catch (Exception e) { return UnhandledExceptionInternal(e); }
            return QuicStatus.SUCCESS;
        }
        public virtual QuicStatus SendComplete(IntPtr context, bool cancelled) { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerSendShutdown() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerSendAborted() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerReceiveAborted() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus SendShutdownComplete() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus ShutdownComplete() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus IdealSendBufferSize() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerAccepted() { return QuicStatus.SUCCESS; }

        #endregion

        internal unsafe QuicStatus ReceiveInternal(QUIC_STREAM_EVENT* evnt)
        {
            if (evnt->RECEIVE.BufferCount == 0)
                return QuicStatus.SUCCESS;

            for (var i = 0; i < evnt->RECEIVE.BufferCount; ++i)
            {
                var status = Receive(evnt->RECEIVE.Buffers[i].Span);
                if (status.IsFailure())
                    return status;
            }
            return QuicStatus.SUCCESS;
        }

        internal unsafe QuicStatus Process(QUIC_HANDLE* handle, QUIC_STREAM_EVENT* evnt)
        {
            try
            {
                return evnt->Type switch
                {
                    QUIC_STREAM_EVENT_TYPE.START_COMPLETE => StartComplete(evnt->START_COMPLETE.Status.ToQuicStatus()),
                    QUIC_STREAM_EVENT_TYPE.RECEIVE => ReceiveInternal(evnt),
                    QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE => SendComplete((IntPtr)evnt->SEND_COMPLETE.ClientContext, evnt->SEND_COMPLETE.Canceled > 0),
                    QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN => PeerSendShutdown(),
                    QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED => PeerSendAborted(),
                    QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED => PeerReceiveAborted(),
                    QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE => SendShutdownComplete(),
                    QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE => ShutdownComplete(),
                    QUIC_STREAM_EVENT_TYPE.IDEAL_SEND_BUFFER_SIZE => IdealSendBufferSize(),
                    QUIC_STREAM_EVENT_TYPE.PEER_ACCEPTED => PeerAccepted(),
                    _ => QuicStatus.INTERNAL_ERROR,
                };
            }
            catch (Exception e)
            {
                UnhandledExceptionInternal(e);
                return QuicStatus.INTERNAL_ERROR;
            }
        }

        internal void OpenInitiatedInternal()
        {
            try { OpenInitiated(); }
            catch (Exception e) { UnhandledExceptionInternal(e); }
        }

        internal void CloseInitiatedInternal()
        {
            try { CloseInitiated(); }
            catch (Exception e) { UnhandledExceptionInternal(e); }
        }

        protected QuicStatus UnhandledExceptionInternal(Exception e)
        {
            try { UnhandledException(e); } catch (Exception ee) { Debug.WriteLine(ee); }
            return QuicStatus.INTERNAL_ERROR;
        }
    }
}
