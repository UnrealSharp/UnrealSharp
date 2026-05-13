using System;
using System.IO;

namespace UnrealSharp.Automation.Utilities;

public static class DotNetUtilities
{
	private const string DotnetMajorVersion = "10.0";

	private static readonly Version RequiredVersion = new Version(10, 0);

	private static string? _cachedExecutable;
	private static string? _cachedSdkPath;
	
	public static string GetVersion()
	{
		return "net" + DotnetMajorVersion;
	}

	public static string FindDotNetExecutable()
	{
		if (_cachedExecutable != null)
		{
			return _cachedExecutable;
		}

		const string dotnetWin = "dotnet.exe";
		const string dotnetUnix = "dotnet";

		string DotnetExe = OperatingSystem.IsWindows() ? dotnetWin : dotnetUnix;

		string? DotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
		if (!string.IsNullOrEmpty(DotnetRoot))
		{
			string Candidate = Path.Combine(DotnetRoot, DotnetExe);
			if (File.Exists(Candidate) && !IsUnrealBundledDotNet(Candidate))
			{
				_cachedExecutable = Candidate;
				return _cachedExecutable;
			}
		}
		
		string? PathVariable = Environment.GetEnvironmentVariable("PATH");
		if (PathVariable != null)
		{
			string[] Paths = PathVariable.Split(Path.PathSeparator);

			foreach (string Path in Paths)
			{
				if (string.IsNullOrWhiteSpace(Path))
				{
					continue;
				}

				string DotnetExePath = System.IO.Path.Combine(Path.Trim(), DotnetExe);

				if (!File.Exists(DotnetExePath) || IsUnrealBundledDotNet(DotnetExePath))
				{
					continue;
				}

				_cachedExecutable = DotnetExePath;
				return _cachedExecutable;
			}
		}
		
		string[] Fallbacks = GetWellKnownInstallPaths();

		foreach (string Fallback in Fallbacks)
		{
			if (File.Exists(Fallback))
			{
				_cachedExecutable = Fallback;
				return _cachedExecutable;
			}
		}

		throw new Exception($"Couldn't find {DotnetExe}! Set DOTNET_ROOT or ensure dotnet is on PATH.");
	}

	public static string GetLatestDotNetSdkPath()
	{
		if (_cachedSdkPath != null)
		{
			return _cachedSdkPath;
		}

		string DotNetExecutable = FindDotNetExecutable();
		string DotNetExecutableDirectory = Path.GetDirectoryName(DotNetExecutable)!;
		string DotNetSdkDirectory = Path.Combine(DotNetExecutableDirectory, "sdk");

		if (!Directory.Exists(DotNetSdkDirectory))
		{
			throw new Exception($".NET SDK directory not found: {DotNetSdkDirectory}");
		}

		string[] FolderPaths = Directory.GetDirectories(DotNetSdkDirectory);

		string? VersionName = null;
		Version LatestVersion = new Version(0, 0);

		foreach (string FolderPath in FolderPaths)
		{
			string FolderName = Path.GetFileName(FolderPath);
			int DashIndex = FolderName.IndexOf('-');
			string NumericPart = DashIndex < 0 ? FolderName : FolderName.Substring(0, DashIndex);

			if (!System.Version.TryParse(NumericPart, out Version? Version))
			{
				continue;
			}

			if (Version.Major != RequiredVersion.Major || Version.Minor != RequiredVersion.Minor)
			{
				continue;
			}

			if (Version <= LatestVersion)
			{
				continue;
			}

			LatestVersion = Version;
			VersionName = FolderName;
		}

		if (VersionName == null)
		{
			throw new Exception($"Couldn't find .NET SDK version {DotnetMajorVersion}.x in {DotNetSdkDirectory}");
		}

		_cachedSdkPath = Path.Combine(DotNetSdkDirectory, VersionName);
		return _cachedSdkPath;
	}
	
	private static bool IsUnrealBundledDotNet(string path)
	{
		string Normalised = path.Replace('\\', '/');
		return Normalised.Contains("/Binaries/ThirdParty/DotNet/", StringComparison.OrdinalIgnoreCase);
	}

	private static string[] GetWellKnownInstallPaths()
	{
		if (OperatingSystem.IsWindows())
		{
			string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			return
			[
				Path.Combine(ProgramFiles, "dotnet", "dotnet.exe")
			];
		}

		if (OperatingSystem.IsMacOS())
		{
			return new[]
			{
				"/usr/local/share/dotnet/dotnet",
				"/opt/homebrew/bin/dotnet",
				"/opt/homebrew/Cellar/dotnet/dotnet"
			};
		}

		if (OperatingSystem.IsLinux())
		{
			return new[]
			{
				"/usr/share/dotnet/dotnet",
				"/usr/local/share/dotnet/dotnet",
				"/usr/bin/dotnet",
				"/snap/bin/dotnet"
			};
		}

		return Array.Empty<string>();
	}
}