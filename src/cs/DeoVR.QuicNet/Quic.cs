using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DeoVR.QuicNet.Core;
using Microsoft.Quic;

namespace DeoVR.QuicNet
{
    /// <summary>
    /// Core helper class for working with QUIC protocol
    /// </summary>
    public static class Quic
    {
        /// <summary>
        /// To make it work in Unity for Android - add [MonoPInvokeCallback(typeof(CallbackDelegate))] attribute
        /// to the delegate implementation function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int CallbackDelegate(void* handle, void* context, void* evnt);

        public static unsafe CallbackDelegate? ConnectionCallback { get; set; } = HandleConnectionEvent;

        public static unsafe CallbackDelegate? StreamCallback { get; set; } = HandleStreamEvent;

        public static unsafe CallbackDelegate? ListenerCallback { get; set; } = HandleListenerEvent;

        internal static ConcurrentDictionary<IntPtr, QuicConnection> Connections { get; } = new();

        internal static ConcurrentDictionary<IntPtr, QuicStream> Streams { get; } = new();

        /// <summary>
        /// Entry point for working with QUIC protocol
        /// </summary>
        public static QuicContext Open(QuicSettings settings) => new(settings);

        public static bool IsSuccess(this QuicStatus quicStatus) => quicStatus == QuicStatus.SUCCESS;

        public static bool IsFailure(this QuicStatus quicStatus) => quicStatus != QuicStatus.SUCCESS;

        public static unsafe int HandleConnectionEvent(void* handle, void* context, void* evnt)
        {
            try
            {
                if (Connections.TryGetValue((IntPtr)handle, out var connection))
                    return connection.EventCallback((QUIC_HANDLE*)handle, (QUIC_CONNECTION_EVENT*)evnt);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
        }

        public static unsafe int HandleStreamEvent(void* handle, void* context, void* evnt)
        {
            try
            {
                if (Streams.TryGetValue((IntPtr)handle, out var stream))
                    return stream.EventCallback((QUIC_HANDLE*)handle, (QUIC_STREAM_EVENT*)evnt);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
        }

        public static unsafe int HandleListenerEvent(void* handle, void* context, void* evnt) => -1;


        #region Internal helpers

        private static readonly Dictionary<int, QuicStatus> _msQuicStatuses = new();

        static Quic()
        {
            foreach (var v in Enum.GetValues(typeof(QuicStatus)))
            {
                var enumValue = (QuicStatus)v;
                _msQuicStatuses[enumValue.ToMsQuicStatus()] = enumValue;
            }
        }

        /// <summary>
        /// Convert enum value to int value for MsQuic
        /// </summary>
        internal static int ToMsQuicStatus(this QuicStatus quicStatus) => quicStatus switch
        {
            QuicStatus.SUCCESS => MsQuic.QUIC_STATUS_SUCCESS,
            QuicStatus.PENDING => MsQuic.QUIC_STATUS_PENDING,
            QuicStatus.CONTINUE => MsQuic.QUIC_STATUS_CONTINUE,
            QuicStatus.OUT_OF_MEMORY => MsQuic.QUIC_STATUS_OUT_OF_MEMORY,
            QuicStatus.INVALID_PARAMETER => MsQuic.QUIC_STATUS_INVALID_PARAMETER,
            QuicStatus.INVALID_STATE => MsQuic.QUIC_STATUS_INVALID_STATE,
            QuicStatus.NOT_SUPPORTED => MsQuic.QUIC_STATUS_NOT_SUPPORTED,
            QuicStatus.NOT_FOUND => MsQuic.QUIC_STATUS_NOT_FOUND,
            QuicStatus.BUFFER_TOO_SMALL => MsQuic.QUIC_STATUS_BUFFER_TOO_SMALL,
            QuicStatus.HANDSHAKE_FAILURE => MsQuic.QUIC_STATUS_HANDSHAKE_FAILURE,
            QuicStatus.ABORTED => MsQuic.QUIC_STATUS_ABORTED,
            QuicStatus.ADDRESS_IN_USE => MsQuic.QUIC_STATUS_ADDRESS_IN_USE,
            QuicStatus.INVALID_ADDRESS => MsQuic.QUIC_STATUS_INVALID_ADDRESS,
            QuicStatus.CONNECTION_TIMEOUT => MsQuic.QUIC_STATUS_CONNECTION_TIMEOUT,
            QuicStatus.CONNECTION_IDLE => MsQuic.QUIC_STATUS_CONNECTION_IDLE,
            QuicStatus.UNREACHABLE => MsQuic.QUIC_STATUS_UNREACHABLE,
            QuicStatus.INTERNAL_ERROR => MsQuic.QUIC_STATUS_INTERNAL_ERROR,
            QuicStatus.CONNECTION_REFUSED => MsQuic.QUIC_STATUS_CONNECTION_REFUSED,
            QuicStatus.PROTOCOL_ERROR => MsQuic.QUIC_STATUS_PROTOCOL_ERROR,
            QuicStatus.VER_NEG_ERROR => MsQuic.QUIC_STATUS_VER_NEG_ERROR,
            QuicStatus.TLS_ERROR => MsQuic.QUIC_STATUS_TLS_ERROR,
            QuicStatus.USER_CANCELED => MsQuic.QUIC_STATUS_USER_CANCELED,
            QuicStatus.ALPN_NEG_FAILURE => MsQuic.QUIC_STATUS_ALPN_NEG_FAILURE,
            QuicStatus.STREAM_LIMIT_REACHED => MsQuic.QUIC_STATUS_STREAM_LIMIT_REACHED,
            QuicStatus.ALPN_IN_USE => MsQuic.QUIC_STATUS_ALPN_IN_USE,
            QuicStatus.CLOSE_NOTIFY => MsQuic.QUIC_STATUS_CLOSE_NOTIFY,
            QuicStatus.BAD_CERTIFICATE => MsQuic.QUIC_STATUS_BAD_CERTIFICATE,
            QuicStatus.UNSUPPORTED_CERTIFICATE => MsQuic.QUIC_STATUS_UNSUPPORTED_CERTIFICATE,
            QuicStatus.REVOKED_CERTIFICATE => MsQuic.QUIC_STATUS_REVOKED_CERTIFICATE,
            QuicStatus.EXPIRED_CERTIFICATE => MsQuic.QUIC_STATUS_EXPIRED_CERTIFICATE,
            QuicStatus.UNKNOWN_CERTIFICATE => MsQuic.QUIC_STATUS_UNKNOWN_CERTIFICATE,
            QuicStatus.REQUIRED_CERTIFICATE => MsQuic.QUIC_STATUS_REQUIRED_CERTIFICATE,
            QuicStatus.CERT_EXPIRED => MsQuic.QUIC_STATUS_CERT_EXPIRED,
            QuicStatus.CERT_UNTRUSTED_ROOT => MsQuic.QUIC_STATUS_CERT_UNTRUSTED_ROOT,
            QuicStatus.CERT_NO_CERT => MsQuic.QUIC_STATUS_CERT_NO_CERT,
            QuicStatus.ADDRESS_NOT_AVAILABLE => MsQuic.QUIC_STATUS_ADDRESS_NOT_AVAILABLE,
            _ => MsQuic.QUIC_STATUS_NOT_SUPPORTED,
        };

        internal static QuicStatus ToQuicStatus(this int msQuicStatus) => _msQuicStatuses.TryGetValue(msQuicStatus, out var v) ? v : QuicStatus.INTERNAL_ERROR;


        #endregion
    }
}
