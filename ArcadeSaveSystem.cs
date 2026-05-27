public static class ArcadeSaveSystem
{
	public static int HighestScore { get; set; } = 0;
	public static int MostRecentScore { get; set; } = 0;
	public static float DifficultyMultiplier { get; set; } = 1.0f;
	public static bool IsGamePlaying { get; set; } = false;
	public static bool IsGameOver { get; set; } = false; // Add this!
}
