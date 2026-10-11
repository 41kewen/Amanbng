using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Platform.Storage;

namespace Amanbng.Linux.Services;

public sealed class LinuxDesktopServices(ILauncher launcher) : IDesktopServices
{
    public async Task OpenAsync(string target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Let the desktop portal manage external applications; closing this window must not kill them.
        Task<bool> launch = Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) && uri.Scheme == "https"
            ? launcher.LaunchUriAsync(uri)
            : launcher.LaunchFileInfoAsync(new FileInfo(target));
        if (!await launch.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken))
            throw new IOException("系统未能打开目标，请检查默认浏览器或视频播放器。");
    }

    public Task PlaySoundAsync(string fileName, CancellationToken cancellationToken)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("找不到音效文件，请重新解压完整安装包。", path);

        if (FindExecutable("gst-play-1.0") is { } gstreamer)
            return RunAsync(gstreamer, ["--no-interactive", path], cancellationToken);
        if (FindExecutable("ffplay") is { } ffplay)
            return RunAsync(ffplay, ["-nodisp", "-autoexit", "-loglevel", "error", path], cancellationToken);
        if (FindExecutable("mpg123") is { } mpg123)
            return RunAsync(mpg123, ["-q", path], cancellationToken);

        throw new InvalidOperationException("播放音效需要 GStreamer 播放器、ffplay 或 mpg123。请按 Linux 安装说明安装其中一个。也可以关闭右上角的音效继续使用。");
    }

    private static string? FindExecutable(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
        .Where(directory => !string.IsNullOrEmpty(directory))
        .Select(directory => Path.Combine(directory, name))
        .FirstOrDefault(File.Exists);

    private static async Task RunAsync(string command, string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var process = new Process { StartInfo = startInfo };
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!process.Start())
                throw new IOException($"无法启动 {command}。");

            Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                await output;
                string detail = (await error).Trim();
                if (process.ExitCode != 0)
                {
                    if (detail.Length > 300)
                        detail = detail[..300];
                    throw new IOException($"{Path.GetFileName(command)} 退出码 {process.ExitCode}。{detail}");
                }
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
                // Observe both redirected streams even when cancellation interrupts playback.
                try { await Task.WhenAll(output, error); }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
            }
        }
        catch (Win32Exception exception)
        {
            throw new IOException($"无法运行 {Path.GetFileName(command)}，请检查是否已安装。", exception);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("外部程序响应超时，请检查默认浏览器或音频播放器。");
        }
    }
}
