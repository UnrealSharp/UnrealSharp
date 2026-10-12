using System;
using System.IO;
using System.Linq;

namespace UnrealSharp.Automation.Utilities;

public struct DotnetVersionInfo
{
	public string VersionName;
	public Version LatestVersion;
}

public static class DotNetUtilities
{
	private const int DotnetMajorVersion = 10;

	private static string? _cachedExecutable;
	private static string? _cachedSdkPath;

	public static string Version => $"net{DotnetMajorVersion}.0";
	
	public static string DotnetFolder => Path.GetDirectoryName(DotNetExecutable)!;

	public static string HostFxrFilename
	{
		get
		{
			if (OperatingSystem.IsWindows())
			{
				return "hostfxr.dll";
			}

			if (OperatingSystem.IsMacOS())
			{
				return "libhostfxr.dylib";
			}

			if (OperatingSystem.IsLinux())
			{
				return "libhostfxr.so";
			}

			throw new PlatformNotSupportedException("Unsupported platform");
		}
	}
	
	public static string DotNetExecutable
	{
		get
		{
			if (_cachedExecutable != null)
			{
				return _cachedExecutable;
			}

			string DotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

			string? DotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
			if (!string.IsNullOrEmpty(DotnetRoot))
			{
				string Candidate = Path.Combine(DotnetRoot, DotnetExe);
				if (TryResolveSdkHost(Candidate, out string? Resolved))
				{
					_cachedExecutable = Resolved;
					return _cachedExecutable;
				}
			}

			string? PathVariable = Environment.GetEnvironmentVariable("PATH");
			if (PathVariable != null)
			{
				string[] PathEntries = PathVariable.Split(Path.PathSeparator);

				foreach (string PathEntry in PathEntries)
				{
					if (string.IsNullOrWhiteSpace(PathEntry))
					{
						continue;
					}

					string Candidate = Path.Combine(PathEntry.Trim(), DotnetExe);

					if (!TryResolveSdkHost(Candidate, out string? Resolved))
					{
						continue;
					}

					_cachedExecutable = Resolved;
					return _cachedExecutable;
				}
			}

			string[] Fallbacks = GetWellKnownInstallPaths();

			foreach (string Fallback in Fallbacks)
			{
				if (TryResolveSdkHost(Fallback, out string? Resolved))
				{
					_cachedExecutable = Resolved;
					return _cachedExecutable;
				}
			}

			throw new Exception($"Couldn't find {DotnetExe}! Set DOTNET_ROOT or ensure dotnet is on PATH.");
		}
	}

	public static string LatestDotNetSdkPath
	{
		get
		{
			if (_cachedSdkPath != null)
			{
				return _cachedSdkPath;
			}

			if (!Directory.Exists(DotNetSdkDirectory))
			{
				throw new Exception($".NET SDK directory not found: {DotNetSdkDirectory}");
			}

			_cachedSdkPath = Path.Combine(DotNetSdkDirectory, LatestDotNetSdkVersionInfo.VersionName);
			return _cachedSdkPath;
		}
	}

	public static string DotNetSdkDirectory
	{
		get
		{
			string DotNetExecutableDirectory = Path.GetDirectoryName(DotNetExecutable)!;
			return Path.Combine(DotNetExecutableDirectory, "sdk");
		}
	}
	
	public static DotnetVersionInfo LatestDotNetSdkVersionInfo => ParseLatestDotnetVersionsInDirectory(DotNetSdkDirectory);

	public static DotnetVersionInfo ParseLatestDotnetVersionsInDirectory(string directory)
	{
		string[] FolderPaths = Directory.GetDirectories(directory);

		string? VersionName = null;
		Version LatestVersion = new Version(0, 0);

		foreach (string FolderPath in FolderPaths)
		{
			string FolderName = Path.GetFileName(FolderPath);
			int DashIndex = FolderName.IndexOf('-');
			string NumericPart = DashIndex < 0 ? FolderName : FolderName.Substring(0, DashIndex);

			if (!System.Version.TryParse(NumericPart, out Version? ParsedVersion))
			{
				continue;
			}

			if (ParsedVersion.Major != DotnetMajorVersion)
			{
				continue;
			}

			if (ParsedVersion <= LatestVersion)
			{
				continue;
			}

			LatestVersion = ParsedVersion;
			VersionName = FolderName;
		}

		if (VersionName == null)
		{
			throw new Exception($"Couldn't find .NET SDK version {DotnetMajorVersion}.x in {DotNetSdkDirectory}");
		}
		
		return new DotnetVersionInfo
		{
			VersionName = VersionName,
			LatestVersion = LatestVersion
		};
	}
	
	public static string LatestHostFxrPath
	{
		get
		{
			string HostFxrDirectory = Path.Combine(DotnetFolder, "host", "fxr");
			DotnetVersionInfo LatestHostFxrVersionInfo = ParseLatestDotnetVersionsInDirectory(HostFxrDirectory);
			return Path.Combine(HostFxrDirectory, LatestHostFxrVersionInfo.VersionName, HostFxrFilename);
		}
	}

	// Follows symlinks and skips shims (mise, asdf) that have no sdk/ folder next to them.
	private static bool TryResolveSdkHost(string candidate, out string? resolved)
	{
		resolved = null;
		if (!File.Exists(candidate) || IsUnrealBundledDotNet(candidate))
		{
			return false;
		}

		string ResolvedPath = candidate;
		try
		{
			FileSystemInfo? Target = new FileInfo(candidate).ResolveLinkTarget(returnFinalTarget: true);
			if (Target != null)
			{
				ResolvedPath = Target.FullName;
			}
		}
		catch (Exception Ex) when (Ex is IOException or UnauthorizedAccessException)
		{
			LoggerUtilities.LogUnrealSharpWarning($"Could not resolve the symlink {candidate}, using it as is: {Ex.Message}");
		}

		string SdkRoot = Path.GetDirectoryName(ResolvedPath)!;
		if (IsUnrealBundledDotNet(ResolvedPath) || !HasSdkForMajorVersion(SdkRoot))
		{
			return false;
		}

		resolved = ResolvedPath;
		return true;
	}

	private static bool HasSdkForMajorVersion(string sdkRoot)
	{
		string SdkDirectory = Path.Combine(sdkRoot, "sdk");
		return Directory.Exists(SdkDirectory)
			&& Directory.EnumerateDirectories(SdkDirectory).Any(directory => Path.GetFileName(directory).StartsWith($"{DotnetMajorVersion}."));
	}

	private static bool IsUnrealBundledDotNet(string dotnetPath)
	{
		string Normalised = dotnetPath.Replace('\\', '/');
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
			return
			[
				"/usr/local/share/dotnet/dotnet",
				"/opt/homebrew/bin/dotnet",
				"/opt/homebrew/Cellar/dotnet/dotnet"
			];
		}

		if (OperatingSystem.IsLinux())
		{
			return
			[
				"/usr/share/dotnet/dotnet",
				"/usr/local/share/dotnet/dotnet",
				"/usr/bin/dotnet",
				"/snap/bin/dotnet"
			];
		}

		return [];
	}
}