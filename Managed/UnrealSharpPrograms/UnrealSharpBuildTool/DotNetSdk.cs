namespace UnrealSharpBuildTool;

public static class DotNetSdk
{
    public static string GetIdentifier(TargetPlatform platform, TargetArchitecture architecture)
    {
        string result;
        switch (platform)
        {
            case TargetPlatform.Windows:
            case TargetPlatform.XboxOne:
            case TargetPlatform.XboxScarlett:
            case TargetPlatform.UWP:
                result = "win";
                break;
            case TargetPlatform.Linux:
                result = "linux";
                break;
            case TargetPlatform.PS4:
                result = "ps4";
                break;
            case TargetPlatform.PS5:
                result = "ps5";
                break;
            case TargetPlatform.Android:
                result = "android";
                break;
            case TargetPlatform.Switch:
                result = "switch";
                break;
            case TargetPlatform.Mac:
                result = "osx";
                break;
            case TargetPlatform.iOS:
                result = "ios";
                break;
            default: throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unsupported platform");
        }
        switch (architecture)
        {
            case TargetArchitecture.x64:
                result += "-x64";
                break;
            case TargetArchitecture.ARM64:
                result += "-arm64";
                break;
            default: throw new ArgumentOutOfRangeException(nameof(architecture), architecture, "Unsupported architecture");
        }
        return result;
    }
}