using System;
using System.Windows.Forms;

namespace Mandelbrot.WinForms
{
	static class Program
	{
		/// <summary>
		/// Główny punkt wejścia aplikacji.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// Włączenie stylów wizualnych Windows
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Uruchomienie głównego formularza
			Application.Run(new MainForm());
		}
	}
}