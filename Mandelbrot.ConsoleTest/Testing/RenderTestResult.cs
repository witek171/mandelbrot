using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot.ConsoleTest.Testing
{
    public class RenderTestResult
    {
        public string CalculatorName { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MaxIterations { get; private set; }
        public int ThreadsUsed { get; private set; }
        public long RenderTimeMs { get; private set; }
        public double ZoomLevel { get; private set; }
        public DateTime Timestamp { get; private set; }
        public RenderTestResult(string name, int w, int h, int iter, int threads, long time, double zoom, DateTime ts)
        {
            CalculatorName = name;
            Width = w;
            Height = h;
            MaxIterations = iter;
            ThreadsUsed = threads;
            RenderTimeMs = time;
            ZoomLevel = zoom;
            Timestamp = ts;
        }
    }
}
