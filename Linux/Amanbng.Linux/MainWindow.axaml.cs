using System.Globalization;
using Amanbng.Linux.Models;
using Amanbng.Linux.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Amanbng.Linux;

public partial class MainWindow : Window
{
    private readonly IDesktopServices desktop;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource page = new();
    private CancellationTokenSource? sound;
    private readonly Dictionary<Hand, Bitmap> handImages = [];
    private readonly Bitmap unknownHand;
    private readonly Avalonia.Threading.DispatcherTimer meritTimer;
    private Hand opponent;
    private long merit;
    private bool ready;

    public MainWindow() : this(null) { }

    public MainWindow(IDesktopServices? desktop)
    {
        InitializeComponent();
        this.desktop = desktop ?? new LinuxDesktopServices(Launcher);
        foreach (var (hand, file) in new[] { (Hand.Scissors, "SetJIANDAO.png"), (Hand.Rock, "SetSHITOU.png"), (Hand.Paper, "SetBU.png") })
            handImages.Add(hand, LoadBitmap(file));
        unknownHand = LoadBitmap("SetNone.png");
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
        if (File.Exists(iconPath))
            Icon = new WindowIcon(iconPath);

        meritTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        meritTimer.Tick += (_, _) =>
        {
            MeritFeedback.Opacity = 0;
            meritTimer.Stop();
        };
        Closed += OnClosed;
        ready = true;
        NewRound();
    }

    private static Bitmap LoadBitmap(string file)
    {
        using Stream stream = AssetLoader.Open(new Uri($"avares://Amanbng.Linux/Assets/{file}"));
        return new Bitmap(stream);
    }

    private void OnExplore(object? sender, RoutedEventArgs e) => Tools.SelectedIndex = 1;

    private void OnToolChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!ready || e.Source != Tools)
            return;

        page.Cancel();
        page.Dispose();
        page = new CancellationTokenSource();
        sound?.Cancel();
        meritTimer.Stop();
        MeritFeedback.Opacity = 0;
        StatusText.Text = "准备就绪";
    }

    private void OnSoundChanged(object? sender, RoutedEventArgs e)
    {
        if (ready && SoundToggle.IsChecked != true)
            sound?.Cancel();
    }

    private async void OnReadCard(object? sender, RoutedEventArgs e)
    {
        CardResult.Text = "dú  kā  qì";
        await PlaySoundAsync("N1.mp3");
    }

    private async void OnCalculateHeight(object? sender, RoutedEventArgs e)
    {
        string value = HeightInput.Text?.Trim() ?? "";
        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal height) || height <= 0)
        {
            HeightResult.Text = "请输入大于 0 的身高，例如 175。";
            return;
        }

        CancellationToken token = page.Token;
        HeightButton.IsEnabled = false;
        HeightProgress.IsVisible = true;
        HeightResult.Text = "正在认真计算…";
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), token);
            HeightResult.Text = $"你的身高是：{value} 厘米";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            HeightResult.Text = "计算已取消，可重新开始。";
        }
        finally
        {
            HeightButton.IsEnabled = true;
            HeightProgress.IsVisible = false;
        }
    }

    private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!ready)
            return;
        (TimeTitle.Text, TimeButton.Content) = TimeLanguage.SelectedIndex switch
        {
            1 => ("Time Viewer", "check time"),
            2 => ("タイムビューア", "時間を確認する"),
            3 => ("시간 뷰어", "시간 확인"),
            _ => ("时间查看器", "查看时间")
        };
    }

    private async void OnCheckTime(object? sender, RoutedEventArgs e) =>
        await OpenAsync("https://www.baidu.com/s?wd=%E7%8E%B0%E5%9C%A8%E5%87%A0%E7%82%B9%E4%BA%86");

    private async void OnLocatePhone(object? sender, RoutedEventArgs e)
    {
        string location = PhoneInput.Text?.Trim() ?? "";
        bool advanced = AdvancedSearch.IsChecked == true;
        PhonePicture.IsVisible = false;
        if (string.IsNullOrEmpty(location) && !advanced)
        {
            PhoneResult.Text = "请先告诉我们手机的位置。";
            return;
        }

        CancellationToken token = page.Token;
        PhoneButton.IsEnabled = false;
        PhoneProgress.IsVisible = true;
        PhoneResult.Text = "正在定位…";
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            PhoneResult.Text = advanced ? "已查找到，在这里：" : $"已查找到，在：{location}";
            PhonePicture.IsVisible = advanced;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            PhoneResult.Text = "定位已取消，可重新开始。";
        }
        finally
        {
            PhoneButton.IsEnabled = true;
            PhoneProgress.IsVisible = false;
        }
    }

    private void OnJoin(object? sender, RoutedEventArgs e) => JoinedText.Text = (FirstText.Text ?? "") + (SecondText.Text ?? "");

    private void OnDifficultyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ready)
            NewRound();
    }

    private void OnNewRound(object? sender, RoutedEventArgs e) => NewRound();

    private void NewRound()
    {
        opponent = RockPaperScissors.RandomHand();
        OpponentImage.Source = handImages[opponent];
        OpponentText.Text = RockPaperScissors.DisplayName(opponent);
        PlayerImage.Source = unknownHand;
        PlayerText.Text = "正在选择";
        GameResult.Text = "请选择你的出招";
        // The original's normal mode deliberately allows only the losing move.
        bool normal = Difficulty.SelectedIndex == 1;
        ScissorsButton.IsEnabled = !normal || RockPaperScissors.Evaluate(Hand.Scissors, opponent) == RoundResult.Lose;
        RockButton.IsEnabled = !normal || RockPaperScissors.Evaluate(Hand.Rock, opponent) == RoundResult.Lose;
        PaperButton.IsEnabled = !normal || RockPaperScissors.Evaluate(Hand.Paper, opponent) == RoundResult.Lose;
    }

    private void OnScissors(object? sender, RoutedEventArgs e) => PlayHand(Hand.Scissors);
    private void OnRock(object? sender, RoutedEventArgs e) => PlayHand(Hand.Rock);
    private void OnPaper(object? sender, RoutedEventArgs e) => PlayHand(Hand.Paper);

    private void PlayHand(Hand hand)
    {
        PlayerImage.Source = handImages[hand];
        PlayerText.Text = RockPaperScissors.DisplayName(hand);
        GameResult.Text = RockPaperScissors.Evaluate(hand, opponent) switch
        {
            RoundResult.Draw => "平局",
            RoundResult.Win => "你赢了",
            _ => "你输了"
        };
    }

    private async void OnMerit(object? sender, RoutedEventArgs e)
    {
        if (merit < long.MaxValue)
            merit++;
        MeritCount.Text = $"功德数：{merit}";
        MeritFeedback.Opacity = 1;
        meritTimer.Stop();
        meritTimer.Start();
        await PlaySoundAsync("NBm.mp3");
    }

    private void OnResetMerit(object? sender, RoutedEventArgs e)
    {
        merit = 0;
        MeritCount.Text = "功德数：0（已重置）";
        meritTimer.Stop();
        MeritFeedback.Opacity = 0;
    }

    private async Task PlaySoundAsync(string fileName)
    {
        if (SoundToggle.IsChecked != true)
            return;

        sound?.Cancel();
        using var playback = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        sound = playback;
        StatusText.Text = "正在播放音效";
        try
        {
            await desktop.PlaySoundAsync(fileName, playback.Token);
            if (!playback.IsCancellationRequested)
                StatusText.Text = "准备就绪";
        }
        catch (OperationCanceledException) when (playback.IsCancellationRequested) { }
        catch (Exception exception) when (IsDesktopError(exception))
        {
            if (!playback.IsCancellationRequested)
                StatusText.Text = $"音效未播放：{exception.Message}";
        }
        finally
        {
            if (sound == playback)
                sound = null;
        }
    }

    private async void OnProject(object? sender, RoutedEventArgs e) => await OpenAsync("https://github.com/zhweaa/Amanbng");
    private async void OnEasterEgg(object? sender, RoutedEventArgs e) => await OpenAsync(Path.Combine(AppContext.BaseDirectory, "Assets", "iiilab_video_2.mp4"));
    private async void OnBackgroundVideo(object? sender, RoutedEventArgs e) => await OpenAsync(Path.Combine(AppContext.BaseDirectory, "Assets", "v6.mp4"));

    private async Task OpenAsync(string target)
    {
        CancellationToken token = page.Token;
        try
        {
            await desktop.OpenAsync(target, token);
            if (!token.IsCancellationRequested)
                StatusText.Text = "已交给系统默认应用打开";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) when (IsDesktopError(exception))
        {
            if (!token.IsCancellationRequested)
                StatusText.Text = $"打开失败：{exception.Message}";
        }
    }

    private static bool IsDesktopError(Exception exception) => exception is IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException;

    private void OnClosed(object? sender, EventArgs e)
    {
        ready = false;
        meritTimer.Stop();
        page.Cancel();
        page.Dispose();
        lifetime.Cancel();
        lifetime.Dispose();
        foreach (Bitmap bitmap in handImages.Values)
            bitmap.Dispose();
        unknownHand.Dispose();
    }
}
