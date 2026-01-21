using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Mandelbrot.ConsoleTest.Testing;

public static class SystemInfo
{
	public static string CpuName { get; } = GetCpuName();
	public static string GpuName { get; } = GetGpuName();
	public static int LogicalCores { get; } = Environment.ProcessorCount;
	public static int PhysicalCores { get; } = GetPhysicalCoreCount();
	public static double TotalRamGb { get; } = GetTotalRamGb();
	public static string OsVersion { get; } = RuntimeInformation.OSDescription;

	private static string GetCpuName()
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
					@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
				return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Unknown";
			}
		}
		catch
		{
		}

		return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
	}

	private static string GetGpuName()
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				using ManagementObjectSearcher searcher = new(
					"Select Name from Win32_VideoController");

				List<string> gpus = [];

				foreach (ManagementBaseObject item in searcher.Get())
				{
					string? name = item["Name"]?.ToString();
					if (!string.IsNullOrWhiteSpace(name))
						gpus.Add(name.Trim());
				}

				return gpus.Count > 0
					? string.Join(" | ", gpus)
					: "Unknown GPU";
			}
		}
		catch
		{
		}

		return "Unknown GPU";
	}

	private static int GetPhysicalCoreCount()
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				int coreCount = 0;
				foreach (ManagementBaseObject? item in new ManagementObjectSearcher(
							"Select NumberOfCores from Win32_Processor").Get())
					coreCount += int.Parse(item["NumberOfCores"].ToString()!);
				return coreCount;
			}
		}
		catch
		{
		}

		return Environment.ProcessorCount / 2;
	}

	private static double GetTotalRamGb()
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				using ManagementObjectSearcher searcher = new(
					"Select TotalPhysicalMemory from Win32_ComputerSystem");
				foreach (ManagementBaseObject? item in searcher.Get())
					return Convert.ToDouble(item["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
			}
		}
		catch
		{
		}

		return 0;
	}

	public static void Print()
	{
		Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
		Console.WriteLine("║                    INFORMACJE O SYSTEMIE                 ║");
		Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
		Console.WriteLine($"║ CPU: {CpuName,-51} ║");
		Console.WriteLine($"║ GPU: {GpuName,-51} ║");
		Console.WriteLine($"║ Rdzenie: {PhysicalCores} fizycznych, {LogicalCores} logicznych                    ║");
		Console.WriteLine($"║ RAM: {TotalRamGb:F1} GB                                              ║");
		Console.WriteLine($"║ OS: {OsVersion,-52} ║");
		Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
	}
}