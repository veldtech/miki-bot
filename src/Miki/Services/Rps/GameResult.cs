namespace Miki.Services.Rps
{
    /// <summary>
    /// Result of a game, can be reused on other services if desired.
    /// </summary>
    public enum GameResult
    {
        Draw = 0,
        Win = 1,
        Lose = 2,
    }
}