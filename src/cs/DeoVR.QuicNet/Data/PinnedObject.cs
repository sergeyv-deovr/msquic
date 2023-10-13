using System;
using System.Runtime.InteropServices;

namespace DeoVR.QuicNet.Data
{
    public class PinnedObject<T> : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Target object
        /// </summary>
        public T Target { get; }

        /// <summary>
        /// Pinned handle
        /// </summary>
        public GCHandle Handle { get; }

        /// <summary>
        /// Pointer to pinned object
        /// </summary>
        public IntPtr Ptr { get; }

        /// <summary>
        /// void* pointer to pinned object
        /// </summary>
        public unsafe void* VoidPtr => (void*)Ptr;

        /// <summary>
        /// Convert <see cref="IntPtr"/> to <see cref="T"/>
        /// </summary>
        public static T? FromIntPtr(IntPtr ptr)
        {
            return (T?) GCHandle.FromIntPtr(ptr).Target;
        }

        /// <summary>
        /// Convert <see cref="IntPtr"/> to <see cref="T"/>
        /// </summary>
        public static unsafe T? FromPtr(void* ptr)
        {
            return (T?)GCHandle.FromIntPtr((IntPtr)ptr).Target;
        }

        public PinnedObject(T obj)
        {
            Target = obj;
            Handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            Ptr = GCHandle.ToIntPtr(Handle);
        }

        public override string ToString() => Target?.ToString() ?? base.ToString();

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            Handle.Free();
            _disposed = true;
        }

        ~PinnedObject()
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
