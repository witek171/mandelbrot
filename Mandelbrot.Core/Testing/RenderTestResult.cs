using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot.Core.Testing
{
    public class RenderTestResult
    {
        public string CalculatorName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MaxIterations { get; set; }
        public int ThreadsUsed { get; set; }
        public long RenderTimeMs { get; set; }
        public double ZoomLevel { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
