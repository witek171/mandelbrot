using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;

namespace Mandelbrot.Core.Pooling
{
	public sealed class BitmapPool : IDisposable
	{
		private readonly ConcurrentDictionary<string, ConcurrentQueue<Bitmap>> _pools;
		private readonly int _maxPerSize;
		private bool _disposed;

		public BitmapPool(int maxPerSize = 3)
		{
			_pools = new ConcurrentDictionary<string, ConcurrentQueue<Bitmap>>();
			_maxPerSize = maxPerSize;
		}

		public Bitmap Rent(int width, int height)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(BitmapPool));

			string key = $"{width}x{height}";

			if (_pools.TryGetValue(key, out var queue) && queue.TryDequeue(out var bitmap))
			{
				return bitmap;
			}

			return new Bitmap(width, height, PixelFormat.Format32bppArgb);
		}

		public void Return(Bitmap bitmap)
		{
			if (bitmap == null || _disposed) return;

			string key = $"{bitmap.Width}x{bitmap.Height}";
			var queue = _pools.GetOrAdd(key, _ => new ConcurrentQueue<Bitmap>());

			if (queue.Count < _maxPerSize)
				queue.Enqueue(bitmap);
			else
				bitmap.Dispose();
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			foreach (var queue in _pools.Values)
				while (queue.TryDequeue(out var bitmap))
					bitmap.Dispose();

			_pools.Clear();
		}
	}
}