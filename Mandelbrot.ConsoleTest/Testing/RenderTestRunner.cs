using Mandelbrot.Core.Calculators;
using Mandelbrot.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot.ConsoleTest.Testing
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

            return new RenderTestResult(
                calculator.Name,
                width,
                height,
                maxIterations,
                r.ThreadsUsed,
                r.RenderTimeMs,
                r.ZoomLevel,
                DateTime.Now
            );
        }
    }
}
