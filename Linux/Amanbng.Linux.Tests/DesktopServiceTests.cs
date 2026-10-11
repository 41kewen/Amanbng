using Amanbng.Linux.Services;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Amanbng.Linux.Tests;

public sealed class DesktopServiceTests
{
    [AvaloniaFact]
    public async Task SoundPlayerReportsMissingDependenciesAndExitFailures()
    {
        var window = new MainWindow();
        var service = new LinuxDesktopServices(window.Launcher);
        string? originalPath = Environment.GetEnvironmentVariable("PATH");
        string directory = Path.Combine(Path.GetTempPath(), $"amanbng-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            Environment.SetEnvironmentVariable("PATH", directory);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PlaySoundAsync("N1.mp3", default));
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.PlaySoundAsync("missing.mp3", default));

            string executable = Path.Combine(directory, "gst-play-1.0");
            await File.WriteAllTextAsync(executable, "#!/bin/sh\nprintf 'decoder unavailable' >&2\nexit 7\n");
            if (OperatingSystem.IsLinux())
                File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            IOException error = await Assert.ThrowsAsync<IOException>(() => service.PlaySoundAsync("N1.mp3", default));
            Assert.Contains("7", error.Message);
            Assert.Contains("decoder unavailable", error.Message);

            await File.WriteAllTextAsync(executable, "#!/bin/sh\nexec /bin/sleep 30\n");
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PlaySoundAsync("N1.mp3", cancellation.Token));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(directory, recursive: true);
            window.Close();
        }
    }
}
