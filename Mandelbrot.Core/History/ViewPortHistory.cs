using Mandelbrot.Core.Rendering;

namespace Mandelbrot.Core.History;

public class ViewPortHistory
{
	private readonly int _maxSize;
	private readonly Stack<ViewPort> _stack;

	public ViewPortHistory(int maxSize = 50)
	{
		_stack = new Stack<ViewPort>();
		_maxSize = maxSize;
	}

	public bool CanGoBack => _stack.Count > 0;

	public void Push(ViewPort viewPort)
	{
		_stack.Push(viewPort);
		while (_stack.Count > _maxSize)
		{
			Stack<ViewPort> temp = new();
			while (_stack.Count > 1) temp.Push(_stack.Pop());
			_stack.Pop();
			while (temp.Count > 0) _stack.Push(temp.Pop());
		}
	}

	public ViewPort Pop()
		=> _stack.Count > 0 ? _stack.Pop() : ViewPort.Default;

	public void Clear()
		=> _stack.Clear();
}