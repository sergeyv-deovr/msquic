using System;
using System.Runtime.InteropServices;

namespace DeoVR.QuicNet.Data
{
    /// <summary>
    /// Easy wrapper for unmanaged data to get pointers for native code
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataPointer<T> : IDisposable
    {
        private T? _data;
        private GCHandle? _dataHandle = null;
        private bool _disposed;

        public T? Data => _data;

        public IntPtr? DataPtr => _dataHandle?.AddrOfPinnedObject();

        /// <summary>
        /// Convert <see cref="IntPtr"/> to <see cref="DataPointer{T}"/>
        /// </summary>
        public static DataPointer<T>? FromIntPtr(IntPtr ptr)
        {
            var handle = GCHandle.FromIntPtr(ptr);
            return handle.Target as DataPointer<T>;
        }

        public DataPointer(T content)
        {
            _data = content;
            _dataHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_data is IDisposable d)
                        d.Dispose();
                    _data = default;
                }

                _dataHandle?.Free();
                _dataHandle = null;

                _disposed = true;
            }
        }

        ~DataPointer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
