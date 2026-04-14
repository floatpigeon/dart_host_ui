using Avalonia;
using Avalonia.LinuxFramebuffer;

namespace DartHost.App;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var launchMode = ResolveLaunchMode(args);
        var builder = BuildAvaloniaApp(launchMode);

        switch (launchMode)
        {
            case LaunchMode.Drm:
                builder.StartLinuxDrm(args, null, 1d);
                break;
            case LaunchMode.FbDev:
                builder.StartLinuxFbDev(args);
                break;
            default:
                builder.StartWithClassicDesktopLifetime(args);
                break;
        }
    }

    public static AppBuilder BuildAvaloniaApp(LaunchMode launchMode)
    {
        var builder = AppBuilder.Configure<App>()
            .WithInterFont()
            .LogToTrace();

#if DEBUG
        builder = builder.WithDeveloperTools();
#endif

        if (launchMode == LaunchMode.Desktop)
        {
            builder = builder.UsePlatformDetect();
        }

        return builder;
    }

    private static LaunchMode ResolveLaunchMode(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--fbdev", StringComparison.OrdinalIgnoreCase)))
        {
            return LaunchMode.FbDev;
        }

        if (args.Any(arg => string.Equals(arg, "--drm", StringComparison.OrdinalIgnoreCase)))
        {
            return LaunchMode.Drm;
        }

        if (args.Any(arg => string.Equals(arg, "--desktop", StringComparison.OrdinalIgnoreCase)))
        {
            return LaunchMode.Desktop;
        }

        if (!OperatingSystem.IsLinux())
        {
            return LaunchMode.Desktop;
        }

        var forceFramebuffer = string.Equals(
            Environment.GetEnvironmentVariable("DART_HOST_USE_FRAMEBUFFER"),
            "1",
            StringComparison.Ordinal);
        if (forceFramebuffer)
        {
            return LaunchMode.Drm;
        }

        var hasWindowingEnvironment =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"))
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

        return hasWindowingEnvironment ? LaunchMode.Desktop : LaunchMode.Drm;
    }

    public enum LaunchMode
    {
        Desktop,
        Drm,
        FbDev
    }
}
