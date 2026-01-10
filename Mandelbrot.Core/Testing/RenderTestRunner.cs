using Mandelbrot.Core.Calculators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot.Core.Testing
{
    public class RenderTestRunner
    {
        public RenderTestResult Run(
            IMandelbrotCalculator calculator,
            int width,
            int height,
            ViewPort viewPort,
            int maxIterations,
            ColorPalette palette)
        {
            var r = calculator.Render(width, height, viewPort, maxIterations, palette);

            return new RenderTestResult
            {
                CalculatorName = calculator.Name,
                Width = width,
                Height = height,
                MaxIterations = maxIterations,
                ThreadsUsed = r.ThreadsUsed,
                RenderTimeMs = r.RenderTimeMs,
                ZoomLevel = r.ZoomLevel,
                Timestamp = DateTime.Now
            };
        }
    }
}
