using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Quic;

namespace DeoVR.QuicNet.Data
{
    /// <summary>
    /// Concurrent dictionary with continuous counter
    /// </summary>
    internal class ConcurrentRegistry<T>
    {
        private long _instanceIdCounter = 0;
        private readonly ConcurrentDictionary<long, T> _instances = new();

        public long Register(T obj)
        {
            var id = Interlocked.Increment(ref _instanceIdCounter);
            _instances.TryAdd(id, obj);
            return id;
        }

        public bool Find(long id, out T obj) => _instances.TryGetValue(id, out obj);

        public bool Remove(long instanceId) => _instances.TryRemove(instanceId, out _);
    }
}
