using System.Collections.Concurrent;

namespace Mandelbrot.Core.Rendering;

public sealed class IterationCache : IDisposable
{
	private readonly ConcurrentDictionary<string, CacheEntry> _cache;
	private readonly object _evictionLock = new();
	private readonly long _maxBytes;
	private long _currentBytes;
	private bool _disposed;

	public IterationCache(long maxMemoryMB = 200)
	{
		_cache = new ConcurrentDictionary<string, CacheEntry>();
		_maxBytes = maxMemoryMB * 1024 * 1024;
	}

	public int Count => _cache.Count;
	public long CurrentMemoryMB => _currentBytes / (1024 * 1024);

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		Clear();
	}

	public bool TryGet(string key, out IterationData data)
	{
		data = null;
		if (_disposed || string.IsNullOrEmpty(key)) return false;

		if (_cache.TryGetValue(key, out CacheEntry? entry))
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

		CacheEntry entry = new()
		{
			Data = data,
			Size = size,
			LastAccess = DateTime.UtcNow
		};

		if (_cache.TryAdd(key, entry)) Interlocked.Add(ref _currentBytes, size);
	}

	private void EnsureCapacity(long required)
	{
		if (_currentBytes + required <= _maxBytes) return;

		lock (_evictionLock)
		{
			if (_currentBytes + required <= _maxBytes) return;

			List<KeyValuePair<string, CacheEntry>>? toEvict = _cache
				.OrderBy(e => e.Value.LastAccess)
				.ThenBy(e => e.Value.Hits)
				.ToList();

			foreach (KeyValuePair<string, CacheEntry> kvp in toEvict)
			{
				if (_currentBytes + required <= _maxBytes * 0.7)
					break;

				if (_cache.TryRemove(kvp.Key, out CacheEntry? removed))
					Interlocked.Add(ref _currentBytes, -removed.Size);
			}
		}
	}

	public void Clear()
	{
		_cache.Clear();
		_currentBytes = 0;
	}

	private class CacheEntry
	{
		public IterationData Data;
		public int Hits;
		public DateTime LastAccess;
		public long Size;
	}
}