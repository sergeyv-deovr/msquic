namespace DeoVR.QuicNet
{
    /// <summary>
    /// Copy from <see cref="Microsoft.Quic.MsQuic"/>
    /// </summary>
    public enum QuicStatus
    {
        SUCCESS,
        PENDING,
        CONTINUE,
        OUT_OF_MEMORY,
        INVALID_PARAMETER,
        INVALID_STATE,
        NOT_SUPPORTED,
        NOT_FOUND,
        BUFFER_TOO_SMALL,
        HANDSHAKE_FAILURE,
        ABORTED,
        ADDRESS_IN_USE,
        INVALID_ADDRESS,
        CONNECTION_TIMEOUT,
        CONNECTION_IDLE,
        UNREACHABLE,
        INTERNAL_ERROR,
        CONNECTION_REFUSED,
        PROTOCOL_ERROR,
        VER_NEG_ERROR,
        TLS_ERROR,
        USER_CANCELED,
        ALPN_NEG_FAILURE,
        STREAM_LIMIT_REACHED,
        ALPN_IN_USE,
        CLOSE_NOTIFY,
        BAD_CERTIFICATE,
        UNSUPPORTED_CERTIFICATE,
        REVOKED_CERTIFICATE,
        EXPIRED_CERTIFICATE,
        UNKNOWN_CERTIFICATE,
        REQUIRED_CERTIFICATE,
        CERT_EXPIRED,
        CERT_UNTRUSTED_ROOT,
        CERT_NO_CERT,
        ADDRESS_NOT_AVAILABLE,
    }
}
