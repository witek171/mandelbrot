using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Mandelbrot.Core.Caching
{
    public sealed class RenderCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly long _maxBytes;
        private long _currentBytes;
        private readonly object _evictionLock = new();
        private bool _disposed;

        public RenderCache(long maxMemoryMB = 100)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _maxBytes = maxMemoryMB * 1024 * 1024;
        }

        public bool TryGet(string key, out Bitmap bitmap)
        {
            bitmap = null;
            if (_disposed || string.IsNullOrEmpty(key)) return false;

            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccess = DateTime.UtcNow;
                entry.Hits++;
                bitmap = entry.Bitmap;
                return true;
            }
            return false;
        }

        public void Add(string key, Bitmap bitmap)
        {
            if (_disposed || bitmap == null || string.IsNullOrEmpty(key)) return;

            long size = (long)bitmap.Width * bitmap.Height * 4;
            EnsureCapacity(size);

            var copy = new Bitmap(bitmap);
            var entry = new CacheEntry
            {
                Bitmap = copy,
                Size = size,
                LastAccess = DateTime.UtcNow
            };

            if (_cache.TryAdd(key, entry))
                Interlocked.Add(ref _currentBytes, size);
            else
                copy.Dispose();
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
                        removed.Bitmap.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var entry in _cache.Values)
                entry.Bitmap.Dispose();
            _cache.Clear();
        }

        private class CacheEntry
        {
            public Bitmap Bitmap;
            public long Size;
            public DateTime LastAccess;
            public int Hits;
        }
    }
}