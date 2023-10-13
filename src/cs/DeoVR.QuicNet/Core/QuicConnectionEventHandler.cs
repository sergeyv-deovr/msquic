using Microsoft.Quic;

namespace DeoVR.QuicNet.Core
{
    /// <summary>
    /// Processes connection events.
    /// Reference: <see cref="QUIC_CONNECTION_EVENT_TYPE"/>
    /// <br/><br/>
    /// <strong>
    /// WARNING:<br/>
    /// These events are being called from a separate thread in native code.<br/>
    /// Be very careful with the implementation as any unhandled exception will crash the app without any logs.
    /// </strong>
    /// </summary>
    public abstract class QuicConnectionEventHandler
    {
        public QuicConnection? Connection { get; set; }

        public virtual QuicStatus Connected() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus ShutdownInitiatedByTransport(QuicStatus quicStatus, ulong errorCode) { return QuicStatus.SUCCESS; }
        public virtual QuicStatus ShutdownInitiatedByPeer(ulong errorCode) { return QuicStatus.SUCCESS; }
        public virtual QuicStatus ShutdownComplete() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus LocalAddressChanged() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerAddressChanged() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerStreamStarted() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus StreamsAvailable() { return QuicStatus.ABORTED; }
        public virtual QuicStatus PeerNeedsStreams() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus IdealProcessorChanged() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus DatagramStateChanged() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus DatagramReceived() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus DatagramSendStateChanged() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus Resumed() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus ResumptionTicketReceived() { return QuicStatus.SUCCESS; }
        public virtual QuicStatus PeerCertificateReceived() { return QuicStatus.SUCCESS; }

        internal unsafe QuicStatus Process(QUIC_HANDLE* handle, QUIC_CONNECTION_EVENT* evnt) => evnt->Type switch
        {
            QUIC_CONNECTION_EVENT_TYPE.CONNECTED => Connected(),
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT => ShutdownInitiatedByTransport(evnt->SHUTDOWN_INITIATED_BY_TRANSPORT.Status.ToQuicStatus(), evnt->SHUTDOWN_INITIATED_BY_TRANSPORT.ErrorCode),
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER => ShutdownInitiatedByPeer(evnt->SHUTDOWN_INITIATED_BY_PEER.ErrorCode),
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE => ShutdownComplete(),
            QUIC_CONNECTION_EVENT_TYPE.LOCAL_ADDRESS_CHANGED => LocalAddressChanged(),
            QUIC_CONNECTION_EVENT_TYPE.PEER_ADDRESS_CHANGED => PeerAddressChanged(),
            QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED => PeerStreamStarted(),
            QUIC_CONNECTION_EVENT_TYPE.STREAMS_AVAILABLE => StreamsAvailable(),
            QUIC_CONNECTION_EVENT_TYPE.PEER_NEEDS_STREAMS => PeerNeedsStreams(),
            QUIC_CONNECTION_EVENT_TYPE.IDEAL_PROCESSOR_CHANGED => IdealProcessorChanged(),
            QUIC_CONNECTION_EVENT_TYPE.DATAGRAM_STATE_CHANGED => DatagramStateChanged(),
            QUIC_CONNECTION_EVENT_TYPE.DATAGRAM_RECEIVED => DatagramReceived(),
            QUIC_CONNECTION_EVENT_TYPE.DATAGRAM_SEND_STATE_CHANGED => DatagramSendStateChanged(),
            QUIC_CONNECTION_EVENT_TYPE.RESUMED => Resumed(),
            QUIC_CONNECTION_EVENT_TYPE.RESUMPTION_TICKET_RECEIVED => ResumptionTicketReceived(),
            QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED => PeerCertificateReceived(),
            _ => QuicStatus.INTERNAL_ERROR,
        };
    }
}
