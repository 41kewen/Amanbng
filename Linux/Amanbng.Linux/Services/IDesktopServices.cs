namespace Amanbng.Linux.Services;

public interface IDesktopServices
{
    Task OpenAsync(string target, CancellationToken cancellationToken);
    Task PlaySoundAsync(string fileName, CancellationToken cancellationToken);
}
