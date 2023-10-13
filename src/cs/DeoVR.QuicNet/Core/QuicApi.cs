using System;
using Microsoft.Quic;

namespace DeoVR.QuicNet.Core
{
    /// <summary>
    /// Encapsulates QUIC ApiTable and Registration
    /// </summary>
    internal class QuicApi : IDisposable
    {
        internal unsafe QUIC_API_TABLE* Table => _table;
        internal unsafe QUIC_HANDLE* Registration => _registration;

        private static int _references = 0;
        private static QuicApi? _instance;

        private static unsafe QUIC_API_TABLE* _table = null;
        private static unsafe QUIC_HANDLE* _registration = null;

        private bool _disposed = false;

        public QuicApi()
        {
            if (_instance != null)
            {
                ++_references;
                return;
            }

            unsafe
            {
                QUIC_HANDLE* registration = null;

                try
                {
                    _table = MsQuic.Open();
                    MsQuic.ThrowIfFailure(_table->RegistrationOpen(null, &registration));
                }
                catch
                {
                    if (registration != null)
                        _table->RegistrationClose(registration);

                    MsQuic.Close(_table);
                    _table = null;
                    throw;
                }

                _registration = registration;
            }

            _references = 1;
            _instance = this;
        }

        public void Dispose()
        {
            // "Really" dispose only when there are no references
            --_references;
            if (_references > 0)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // dispose managed state (managed objects).
            }

            unsafe
            {
                if (_registration != null)
                    _table->RegistrationClose(_registration);

                _registration = null;

                if (_table != null)
                    MsQuic.Close(_table);

                _table = null;
            }

            _disposed = true;
            _instance = null;
        }

        ~QuicApi() => Dispose(false);
    }
}
