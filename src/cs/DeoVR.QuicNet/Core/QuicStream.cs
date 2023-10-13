using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DeoVR.QuicNet.Data;
using Microsoft.Quic;
using Microsoft.Quic.Polyfill;

namespace DeoVR.QuicNet.Core
{
    /// <summary>
    /// Encapsulates QUIC stream
    /// </summary>
    public class QuicStream : IDisposable
    {
        private static readonly ConcurrentRegistry<QuicStream> _registry = new();

        private bool _disposed = false;

        private readonly QuicStreamSettings _settings;
        private readonly QuicStreamEventHandler _handler;

        private readonly ConcurrentDictionary<long, QuicMessage> _frames = new();

        private long _messageId = 0;
        private TaskCompletionSource<bool>? _openTask;
        private TaskCompletionSource<bool>? _closeTask;

        public QuicContext Context => Connection.Context;

        public QuicConnection Connection { get; }

        public bool IsOpen { get; private set; }

        public bool IsActive { get; private set; }

        internal unsafe QUIC_HANDLE* Handle { get; private set; }

        internal QuicStream(QuicConnection connection, QuicStreamSettings settings, QuicStreamEventHandler handler)
        {
            Connection = connection;
            _settings = settings;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            _handler.Stream = this;
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
                    throw new InvalidOperationException("Already open");
                if (!Connection.IsOpen)
                    throw new InvalidOperationException("Connection is closed");
                if (!Connection.IsActive)
                    throw new InvalidOperationException("Connection is not active");

                _openTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var task = _openTask.Task;
                IsActive = false;

                QUIC_HANDLE* stream = null;
                try
                {
                    var callback = (delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>)Marshal.GetFunctionPointerForDelegate(Quic.StreamCallback);
                    MsQuic.ThrowIfFailure(Context.Api.Table->StreamOpen(Connection.Handle, _settings.OpenFlags, callback, null, &stream));
                    
                    Handle = stream;
                    Quic.Streams.TryAdd((IntPtr)Handle, this);

                    MsQuic.ThrowIfFailure(Context.Api.Table->StreamStart(stream, _settings.StartFlags));
                }
                catch
                {
                    Handle = null;
                    _openTask = null;
                    if (stream != null)
                    {
                        Context.Api.Table->StreamShutdown(stream, QUIC_STREAM_SHUTDOWN_FLAGS.NONE, 0);
                        Context.Api.Table->StreamClose(stream);
                    }
                    throw;
                }

                IsOpen = true;

                _handler.OpenInitiatedInternal();
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
                    throw new InvalidOperationException("Already closed");

                _closeTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var task = _closeTask.Task;

                Context.Api.Table->StreamShutdown(Handle, QUIC_STREAM_SHUTDOWN_FLAGS.NONE, 0);
                Context.Api.Table->StreamClose(Handle);
                Quic.Streams.TryRemove((IntPtr)Handle, out _);
                Handle = null;

                _handler.CloseInitiatedInternal();
                return task;
            }
        }

        public void Send(QuicMessage frame)
        {
            if (!Connection.IsOpen)
                throw new InvalidOperationException("Connection is closed");
            if (!Connection.IsActive)
                throw new InvalidOperationException("Connection is not active");
            if (!IsOpen)
                throw new InvalidOperationException("Stream is closed");
            if (!IsActive)
                throw new InvalidOperationException("Stream is not active");

            var messageId = ++_messageId;
            frame.MessageId = new PinnedObject<long>(messageId);
            _frames.TryAdd(messageId, frame);

            unsafe
            {
                try
                {
                    MsQuic.ThrowIfFailure(Context.Api.Table->StreamSend(Handle, frame.Ptr, 1, QUIC_SEND_FLAGS.NONE, frame.MessageId.VoidPtr));
                }
                catch
                {
                    frame.Dispose();
                    _frames.TryRemove(messageId, out _);
                }
            }
        }

        internal unsafe int EventCallback(QUIC_HANDLE* handle, QUIC_STREAM_EVENT* evnt)
        {
            if (evnt->Type == QUIC_STREAM_EVENT_TYPE.START_COMPLETE)
            {
                if (MsQuic.StatusSucceeded(evnt->START_COMPLETE.Status))
                    IsActive = true;
                _openTask?.SetResult(IsActive);
                _openTask = null;
                //OnMessage?.Invoke($"START COMPLETE: {evnt->START_COMPLETE.Status} {evnt->START_COMPLETE.PeerAccepted}");
            }
            else if(evnt->Type == QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN)
            {
                IsActive = false;

                _openTask?.SetCanceled();
                _openTask = null;
            }
            else if (evnt->Type == QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE)
            {
                IsActive = false;

                _openTask?.SetCanceled();
                _openTask = null;

                _closeTask?.SetResult(!IsActive);
                _closeTask = null;
            }
            else if (evnt->Type == QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE)
            {
                var frameCtx = PinnedObject<long?>.FromPtr(evnt->SEND_COMPLETE.ClientContext);
                if (frameCtx == null)
                {
                    //_handler.Log("Unable to parse message id");
                }
                else
                {
                    var messageId = frameCtx.Value;
                    if (messageId != 0)
                    {
                        if (_frames.TryRemove(messageId, out var message))
                        {
                            message?.Dispose();
                        }
                    }
                }
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
                    Context.Api.Table->StreamShutdown(Handle, QUIC_STREAM_SHUTDOWN_FLAGS.NONE, 0);
                    Context.Api.Table->StreamClose(Handle);

                    Quic.Streams.TryRemove((IntPtr)Handle, out _);
                }
                Handle = null;
            }

            _disposed = true;
        }

        ~QuicStream() => Dispose(false);
    }
}
