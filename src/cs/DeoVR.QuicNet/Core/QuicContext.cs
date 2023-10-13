using System;
using System.Collections.Concurrent;
using Microsoft.Quic;
using static DeoVR.QuicNet.Quic;

namespace DeoVR.QuicNet.Core
{
    /// <summary>
    /// Encapsulates QUIC configuration
    /// </summary>
    public class QuicContext : IDisposable
    {
        private bool _disposed = false;
        private readonly QuicSettings _settings;

        internal QuicApi Api { get; }

        internal unsafe QUIC_HANDLE* Configuration { get; private set; }

        // TODO extend settings
        public QuicContext(QuicSettings quicSettings)
        {
            _settings = quicSettings;

            Api = new QuicApi();

            unsafe
            {
                QUIC_HANDLE* configuration = null;

                try
                {
                    var alpnStr = "h3";
                    if (!string.IsNullOrEmpty(_settings.CustomAlpn))
                    {
                        alpnStr = _settings.CustomAlpn;
                    }
                    else if (_settings.Version == QuicVersion.Version_1)
                    {
                        alpnStr = "h3-01";
                    }
                    else if (_settings.Version == QuicVersion.Version_2)
                    {
                        alpnStr = "h3-02";
                    }

                    var settings = new QUIC_SETTINGS
                    {
                        IsSetFlags = 0,
                        PeerBidiStreamCount = 1,
                        PeerUnidiStreamCount = 3,
                    };
                    settings.IsSet.PeerBidiStreamCount = 1;
                    settings.IsSet.PeerUnidiStreamCount = 1;

                    byte* alpn = stackalloc byte[alpnStr.Length];
                    for (var i = 0; i < alpnStr.Length; ++i)
                    {
                        alpn[i] = (byte)alpnStr[i];
                    }
                    var alpnBuffer = new QUIC_BUFFER()
                    {
                        Buffer = alpn,
                        Length = (uint)alpnStr.Length
                    };

                    MsQuic.ThrowIfFailure(Api.Table->ConfigurationOpen(Api.Registration, &alpnBuffer, 1, &settings, (uint)sizeof(QUIC_SETTINGS), null, &configuration));

                    var config = new QUIC_CREDENTIAL_CONFIG
                    {
                        Flags = quicSettings.CredentialFlags,
                    };

                    MsQuic.ThrowIfFailure(Api.Table->ConfigurationLoadCredential(configuration, &config));
                }
                catch
                {
                    if (configuration != null)
                        Api.Table->ConfigurationClose(configuration);

                    throw;
                }

                Configuration = configuration;
            }

        }

        public QuicConnection CreateConnection(QuicConnectionSettings settings, QuicConnectionEventHandler handler)
        {
            return new QuicConnection(this, settings, handler);
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

            unsafe
            {
                if (Configuration != null)
                    Api.Table->ConfigurationClose(Configuration);
                Configuration = null;
            }
            if (disposing)
            {
                Api.Dispose();
            }

            _disposed = true;
        }

        ~QuicContext() => Dispose(false);
    }
}
