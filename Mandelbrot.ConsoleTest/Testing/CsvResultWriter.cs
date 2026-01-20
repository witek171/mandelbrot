using Mandelbrot.ConsoleTest.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot.ConsoleTest.Testing
{
    public static class CsvResultWriter
    {
        public static void Append(string path, RenderTestResult r)
        {
            bool exists = File.Exists(path);

            using var w = new StreamWriter(path, true);

            if (!exists)
                w.WriteLine("Timestamp,Calculator,Width,Height,Iterations,Threads,TimeMs,Zoom");

            w.WriteLine(
                $"{r.Timestamp:O},{r.CalculatorName},{r.Width},{r.Height}," +
                $"{r.MaxIterations},{r.ThreadsUsed},{r.RenderTimeMs},{r.ZoomLevel:F6}"
            );
        }
    }
}
