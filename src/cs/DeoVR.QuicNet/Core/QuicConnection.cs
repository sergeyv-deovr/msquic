using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DeoVR.QuicNet.Data;
using Microsoft.Quic;

namespace DeoVR.QuicNet.Core
{
    /// <summary>
    /// Encapsulates QUIC connection
    /// </summary>
    public class QuicConnection : IDisposable
    {
        private bool _disposed;
        private readonly QuicConnectionEventHandler _handler;
        private readonly QuicConnectionSettings _settings;
        private TaskCompletionSource<bool>? _openTask = null;
        private TaskCompletionSource<bool>? _closeTask = null;

        public QuicContext Context { get; }

        public bool IsOpen { get; private set; }

        public bool IsActive { get; private set; }

        public string? Host => _settings.Host;

        public ushort Port => _settings.Port;

        internal unsafe QUIC_HANDLE* Handle { get; private set; } = null;

        internal QuicConnection(QuicContext context, QuicConnectionSettings settings, QuicConnectionEventHandler handler)
        {
            Context = context;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.Host)) throw new ArgumentNullException(nameof(settings.Host));

            _handler.Connection = this;
        }

        public Task OpenAsync()
        {
            if (_openTask != null)
                throw new InvalidOperationException("Already opening");

            if (Quic.ConnectionCallback == null)
                throw new Exception("Connection callback is not set for context");

            unsafe
            {
                if (Handle != null)
                    throw new InvalidOperationException("Connection is already open");

                _openTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var task = _openTask.Task;

                ResetState();

                QUIC_HANDLE* connection = null;
                try
                {
                    var callback = (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>)Marshal.GetFunctionPointerForDelegate(Quic.ConnectionCallback);
                    MsQuic.ThrowIfFailure(Context.Api.Table->ConnectionOpen(Context.Api.Registration, callback, null, &connection));

                    Handle = connection;
                    Quic.Connections.TryAdd((IntPtr)Handle, this);

                    var hostBytes = Encoding.UTF8.GetBytes(Host);
                    sbyte* addrBytes = stackalloc sbyte[hostBytes.Length + 1];
                    for (var i = 0; i < hostBytes.Length; i++)
                    {
                        addrBytes[i] = (sbyte)hostBytes[i];
                    }
                    addrBytes[hostBytes.Length] = 0;
                    MsQuic.ThrowIfFailure(Context.Api.Table->ConnectionStart(connection, Context.Configuration, 0, addrBytes, Port));
                }
                catch
                {
                    Handle = null;
                    _openTask = null;
                    if (connection != null)
                    {
                        Context.Api.Table->ConnectionShutdown(connection, QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT, 0);
                        Context.Api.Table->ConnectionClose(connection);
                    }
                    throw;
                }

                IsOpen = true;
                return task;
            }
        }

        public Task CloseAsync()
        {
            if (_closeTask != null)
                throw new InvalidOperationException("Already closing");
            unsafe
            {
                if (Handle == null)
                    throw new InvalidOperationException("Connection is closed");

                _closeTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var task = _closeTask.Task;

                Context.Api.Table->ConnectionShutdown(Handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, 0);
                Context.Api.Table->ConnectionClose(Handle);
                Quic.Connections.TryRemove((IntPtr)Handle, out _);
                Handle = null;

                IsOpen = false;
                ResetState();
                return task;
            }
        }

        public QuicStream CreateStream(QuicStreamSettings settings, QuicStreamEventHandler handler)
        {
            return new QuicStream(this, settings, handler);
        }

        private void ResetState()
        {
            IsActive = false;
        }

        internal unsafe int EventCallback(QUIC_HANDLE* handle, QUIC_CONNECTION_EVENT* evnt)
        {
            if (evnt->Type == QUIC_CONNECTION_EVENT_TYPE.CONNECTED)
            {
                IsActive = true;

                _openTask?.SetResult(true);
                _openTask = null;

                //void* buf = stackalloc byte[128];
                //uint len = 128;
                //if (MsQuic.StatusSucceeded(Context.Api.Table->GetParam(handle, MsQuic.QUIC_PARAM_CONN_REMOTE_ADDRESS, &len, buf)))
                //{
                //    QuicAddr* addr = (QuicAddr*)buf;
                //    //_handler.Log($"Connected Family: {addr->Family}");
                //}
            }

            if (evnt->Type == QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT)
            {
                _openTask?.SetCanceled();
                _openTask = null;
            }
            else if (evnt->Type == QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER)
            {
                _openTask?.SetCanceled();
                _openTask = null;
                //_handler.Log($"Error code: {evnt->SHUTDOWN_INITIATED_BY_PEER.ErrorCode.ToString("X8")}");
                //return MsQuic.QUIC_STATUS_SUCCESS;
            }
            else if (evnt->Type == QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE)
            {
                _openTask?.SetCanceled();
                _openTask = null;
                _closeTask?.SetResult(true);
                _closeTask = null;

                IsActive = false;
            }

            return _handler.Process(handle, evnt).ToMsQuicStatus();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {

            }

            unsafe
            {
                if (Handle != null)
                {
                    Context.Api.Table->ConnectionShutdown(Handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, 0);
                    Context.Api.Table->ConnectionClose(Handle);

                    Quic.Connections.TryRemove((IntPtr)Handle, out _);
                }

                Handle = null;
            }

            _disposed = true;
        }

        ~QuicConnection() => Dispose(false);
    }
}
