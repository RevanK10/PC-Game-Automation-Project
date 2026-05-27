using Godot;

public partial class PlayerStats : Node
{
	public int CurrentLevel { get; private set; } = 1;
	public int CurrentExp { get; private set; } = 0;
	public int ExpNeeded { get; private set; } = 100;
	public int TotalKills { get; private set; } = 0; // Keeping this one!

	// Exported variables so you can link them directly in your Godot Editor
	[Export] public Label LevelLabel;
	[Export] public Label ExpLabel;
	[Export] public Label KillLabel;

	public override void _Ready()
	{
		// Display the starting values on screen right away
		UpdateTextUI();
	}

	public void RegisterKill(int expReward)
	{
		TotalKills++;
		CurrentExp += expReward;

		// Handle leveling up progression
		if (CurrentExp >= ExpNeeded)
		{
			CurrentExp -= ExpNeeded;
			CurrentLevel++;
			
			// Increase the difficulty requirements for the next level by 50%
			ExpNeeded = Mathf.RoundToInt(ExpNeeded * 1.5f);
			GD.Print($"[Progression] Level Up! You reached Level {CurrentLevel}");
		}

		// Push the new numbers to your screen text boxes
		UpdateTextUI();
	}

	private void UpdateTextUI()
	{
		if (LevelLabel != null) LevelLabel.Text = $"Level: {CurrentLevel}";
		if (ExpLabel != null) ExpLabel.Text = $"EXP: {CurrentExp} / {ExpNeeded}";
		if (KillLabel != null) KillLabel.Text = $"Kills: {TotalKills}";
	}
}
