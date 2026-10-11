using Amanbng.Linux;
using Amanbng.Linux.Models;
using Amanbng.Linux.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Amanbng.Linux.Tests.TestApplication))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Amanbng.Linux.Tests;

public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class ToolTests
{
    [Theory]
    [InlineData(Hand.Scissors, Hand.Scissors, RoundResult.Draw)]
    [InlineData(Hand.Scissors, Hand.Rock, RoundResult.Lose)]
    [InlineData(Hand.Scissors, Hand.Paper, RoundResult.Win)]
    [InlineData(Hand.Rock, Hand.Scissors, RoundResult.Win)]
    [InlineData(Hand.Rock, Hand.Rock, RoundResult.Draw)]
    [InlineData(Hand.Rock, Hand.Paper, RoundResult.Lose)]
    [InlineData(Hand.Paper, Hand.Scissors, RoundResult.Lose)]
    [InlineData(Hand.Paper, Hand.Rock, RoundResult.Win)]
    [InlineData(Hand.Paper, Hand.Paper, RoundResult.Draw)]
    public void AllGameOutcomes(Hand player, Hand opponent, RoundResult expected) =>
        Assert.Equal(expected, RockPaperScissors.Evaluate(player, opponent));

    [AvaloniaFact]
    public void UnicodeAndEmptyStringsCanBeJoined()
    {
        var window = OpenWindow();
        try
        {
            Get<TabControl>(window, "Tools").SelectedIndex = 5;
            Get<TextBox>(window, "FirstText").Text = "你好\n";
            Get<TextBox>(window, "SecondText").Text = "Linux 🐧";
            Click(window, "JoinButton");
            Assert.Equal("你好\nLinux 🐧", Get<TextBox>(window, "JoinedText").Text);
            Get<TextBox>(window, "FirstText").Text = null;
            Get<TextBox>(window, "SecondText").Text = "";
            Click(window, "JoinButton");
            Assert.Equal("", Get<TextBox>(window, "JoinedText").Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task HeightValidationCompletionAndNavigationCancellation()
    {
        var window = OpenWindow();
        try
        {
            var tabs = Get<TabControl>(window, "Tools");
            tabs.SelectedIndex = 2;
            foreach (string value in new[] { "", "abc", "-1", "0", "NaN" })
            {
                Get<TextBox>(window, "HeightInput").Text = value;
                Click(window, "HeightButton");
                Assert.Contains("请输入", Get<TextBlock>(window, "HeightResult").Text);
            }
            Get<TextBox>(window, "HeightInput").Text = "175.5";
            Click(window, "HeightButton");
            Assert.False(Get<Button>(window, "HeightButton").IsEnabled);
            await WaitUntil(() => Get<Button>(window, "HeightButton").IsEnabled);
            Assert.Equal("你的身高是：175.5 厘米", Get<TextBlock>(window, "HeightResult").Text);

            Click(window, "HeightButton");
            tabs.SelectedIndex = 0;
            await WaitUntil(() => Get<Button>(window, "HeightButton").IsEnabled);
            Assert.Contains("取消", Get<TextBlock>(window, "HeightResult").Text);
            Assert.False(Get<ProgressBar>(window, "HeightProgress").IsVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task PhoneModesAndClosingDuringSearch()
    {
        var window = OpenWindow();
        Get<TabControl>(window, "Tools").SelectedIndex = 4;
        try
        {
            Click(window, "PhoneButton");
            Assert.Contains("请先", Get<TextBlock>(window, "PhoneResult").Text);
            Get<TextBox>(window, "PhoneInput").Text = " 沙发上 ";
            Click(window, "PhoneButton");
            await WaitUntil(() => Get<Button>(window, "PhoneButton").IsEnabled);
            Assert.Equal("已查找到，在：沙发上", Get<TextBlock>(window, "PhoneResult").Text);
            Assert.False(Get<Image>(window, "PhonePicture").IsVisible);

            Get<CheckBox>(window, "AdvancedSearch").IsChecked = true;
            Click(window, "PhoneButton");
            await WaitUntil(() => Get<Button>(window, "PhoneButton").IsEnabled);
            Assert.True(Get<Image>(window, "PhonePicture").IsVisible);

            Click(window, "PhoneButton");
            window.Close();
            await WaitUntil(() => Get<Button>(window, "PhoneButton").IsEnabled);
            Assert.Contains("取消", Get<TextBlock>(window, "PhoneResult").Text);
        }
        finally { if (window.IsVisible) window.Close(); }
    }

    [AvaloniaFact]
    public void NormalGameModeKeepsOriginalLosingMoveJoke()
    {
        var window = OpenWindow();
        try
        {
            Get<TabControl>(window, "Tools").SelectedIndex = 6;
            Get<ComboBox>(window, "Difficulty").SelectedIndex = 1;
            for (int round = 0; round < 12; round++)
            {
                Click(window, "NewRoundButton");
                var moves = new[] { "ScissorsButton", "RockButton", "PaperButton" }
                    .Select(name => Get<Button>(window, name));
                Button move = Assert.Single(moves, button => button.IsEnabled);
                move.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("你输了", Get<TextBlock>(window, "GameResult").Text);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task AudioFailureDoesNotBreakMeritOrReaderAndMuteWorks()
    {
        var desktop = new FakeDesktop { SoundError = new IOException("测试播放器失败") };
        var window = OpenWindow(desktop);
        try
        {
            Get<TabControl>(window, "Tools").SelectedIndex = 1;
            Click(window, "ReadCardButton");
            await WaitUntil(() => Get<TextBlock>(window, "StatusText").Text?.Contains("测试播放器失败") == true);
            Assert.Equal("dú  kā  qì", Get<TextBlock>(window, "CardResult").Text);
            Assert.Equal("N1.mp3", desktop.SoundFiles.Last());

            Get<TabControl>(window, "Tools").SelectedIndex = 7;
            Click(window, "MeritButton");
            Assert.Equal("功德数：1", Get<TextBlock>(window, "MeritCount").Text);
            Assert.Equal("NBm.mp3", desktop.SoundFiles.Last());
            Get<CheckBox>(window, "SoundToggle").IsChecked = false;
            int calls = desktop.SoundFiles.Count;
            Click(window, "MeritButton");
            Assert.Equal("功德数：2", Get<TextBlock>(window, "MeritCount").Text);
            Assert.Equal(calls, desktop.SoundFiles.Count);
            Click(window, "ResetMeritButton");
            Assert.Equal("功德数：0（已重置）", Get<TextBlock>(window, "MeritCount").Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task TimeViewerUsesBaiduAndReportsLaunchFailures()
    {
        var desktop = new FakeDesktop();
        var window = OpenWindow(desktop);
        try
        {
            Get<TabControl>(window, "Tools").SelectedIndex = 3;
            Get<ComboBox>(window, "TimeLanguage").SelectedIndex = 1;
            Assert.Equal("Time Viewer", Get<TextBlock>(window, "TimeTitle").Text);
            Click(window, "TimeButton");
            Assert.StartsWith("https://www.baidu.com/s?wd=", desktop.OpenedTargets.Single());
            desktop.OpenError = new IOException("没有默认浏览器");
            Click(window, "TimeButton");
            await WaitUntil(() => Get<TextBlock>(window, "StatusText").Text?.Contains("没有默认浏览器") == true);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task NavigationCancelsActiveSound()
    {
        var desktop = new FakeDesktop { WaitForCancellation = true };
        var window = OpenWindow(desktop);
        try
        {
            Get<TabControl>(window, "Tools").SelectedIndex = 1;
            Click(window, "ReadCardButton");
            Get<TabControl>(window, "Tools").SelectedIndex = 0;
            await WaitUntil(() => desktop.SoundCancelled);
            Assert.Equal("准备就绪", Get<TextBlock>(window, "StatusText").Text);
        }
        finally { window.Close(); }
    }

    private static MainWindow OpenWindow(FakeDesktop? desktop = null)
    {
        var window = new MainWindow(desktop ?? new FakeDesktop());
        window.Show();
        return window;
    }

    private static T Get<T>(MainWindow window, string name) where T : Control =>
        window.FindControl<T>(name) ?? throw new InvalidOperationException($"Missing control: {name}");

    private static void Click(MainWindow window, string name) =>
        Get<Button>(window, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
            await Task.Delay(20, timeout.Token);
    }

    private sealed class FakeDesktop : IDesktopServices
    {
        public List<string> SoundFiles { get; } = [];
        public List<string> OpenedTargets { get; } = [];
        public Exception? SoundError { get; init; }
        public Exception? OpenError { get; set; }
        public bool WaitForCancellation { get; init; }
        public bool SoundCancelled { get; private set; }

        public Task OpenAsync(string target, CancellationToken cancellationToken)
        {
            OpenedTargets.Add(target);
            return OpenError is { } error ? Task.FromException(error) : Task.CompletedTask;
        }

        public async Task PlaySoundAsync(string fileName, CancellationToken cancellationToken)
        {
            SoundFiles.Add(fileName);
            if (SoundError is { } error)
                throw error;
            if (WaitForCancellation)
            {
                try { await Task.Delay(Timeout.Infinite, cancellationToken); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    SoundCancelled = true;
                    throw;
                }
            }
        }
    }
}
