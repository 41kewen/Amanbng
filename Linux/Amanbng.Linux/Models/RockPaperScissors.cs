namespace Amanbng.Linux.Models;

public enum Hand
{
    Scissors,
    Rock,
    Paper
}

public enum RoundResult
{
    Draw,
    Win,
    Lose
}

public static class RockPaperScissors
{
    public static Hand RandomHand() => (Hand)Random.Shared.Next(3);

    public static Hand WinningHand(Hand hand) => hand switch
    {
        Hand.Scissors => Hand.Rock,
        Hand.Rock => Hand.Paper,
        Hand.Paper => Hand.Scissors,
        _ => throw new ArgumentOutOfRangeException(nameof(hand))
    };

    public static RoundResult Evaluate(Hand player, Hand opponent)
    {
        if (!Enum.IsDefined(player))
            throw new ArgumentOutOfRangeException(nameof(player));
        if (!Enum.IsDefined(opponent))
            throw new ArgumentOutOfRangeException(nameof(opponent));

        if (player == opponent)
            return RoundResult.Draw;
        return player == WinningHand(opponent) ? RoundResult.Win : RoundResult.Lose;
    }

    public static string DisplayName(Hand hand) => hand switch
    {
        Hand.Scissors => "剪刀",
        Hand.Rock => "石头",
        Hand.Paper => "布",
        _ => throw new ArgumentOutOfRangeException(nameof(hand))
    };
}
