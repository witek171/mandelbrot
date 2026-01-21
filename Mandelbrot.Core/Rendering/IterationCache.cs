// Mandelbrot.Core/Caching/IterationCache.cs
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.Caching
{
    /// <summary>
    /// Cache dla ITERACJI (nie bitmap!)
    /// Zmiana palety = instant, bo mamy iteracje w cache
    /// </summary>
    public sealed class IterationCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly long _maxBytes;
        private long _currentBytes;
        private readonly object _evictionLock = new();
        private bool _disposed;

        public int Count => _cache.Count;
        public long CurrentMemoryMB => _currentBytes / (1024 * 1024);

        public IterationCache(long maxMemoryMB = 200)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _maxBytes = maxMemoryMB * 1024 * 1024;
        }

        public bool TryGet(string key, out IterationData data)
        {
            data = null;
            if (_disposed || string.IsNullOrEmpty(key)) return false;

            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccess = DateTime.UtcNow;
                entry.Hits++;
                data = entry.Data;
                return true;
            }
            return false;
        }

        public void Add(IterationData data)
        {
            if (_disposed || data == null) return;

            string key = data.GetCacheKey();
            long size = data.Iterations.Length * sizeof(int);

            EnsureCapacity(size);

            var entry = new CacheEntry
            {
                Data = data,
                Size = size,
                LastAccess = DateTime.UtcNow
            };

            if (_cache.TryAdd(key, entry))
            {
                Interlocked.Add(ref _currentBytes, size);
            }
        }

        private void EnsureCapacity(long required)
        {
            if (_currentBytes + required <= _maxBytes) return;

            lock (_evictionLock)
            {
                if (_currentBytes + required <= _maxBytes) return;

                var toEvict = _cache
                    .OrderBy(e => e.Value.LastAccess)
                    .ThenBy(e => e.Value.Hits)
                    .ToList();

                foreach (var kvp in toEvict)
                {
                    if (_currentBytes + required <= _maxBytes * 0.7) break;

                    if (_cache.TryRemove(kvp.Key, out var removed))
                    {
                        Interlocked.Add(ref _currentBytes, -removed.Size);
                    }
                }
            }
        }

        public void Clear()
        {
            _cache.Clear();
            _currentBytes = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }

        private class CacheEntry
        {
            public IterationData Data;
            public long Size;
            public DateTime LastAccess;
            public int Hits;
        }
    }
}