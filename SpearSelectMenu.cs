using Godot;
using System;

public partial class SpearSelectMenu : Control
{
	[Export] public Button LightningButton;
	[Export] public Button GravityButton;
	[Export] public Button ExplosiveButton;
	[Export] public Button StartGameButton;

	private readonly Color _selectedColor = new Color(1.0f, 0.5f, 0.0f); // Bright Orange
	private readonly Color _defaultColor = new Color(1.0f, 1.0f, 1.0f);  // White

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		ArcadeSaveSystem.SelectedSpear = SpearType.None;
		if (StartGameButton != null) StartGameButton.Disabled = true;

		if (LightningButton != null) LightningButton.Pressed += () => SelectWeapon(SpearType.Lightning, LightningButton);
		if (GravityButton != null) GravityButton.Pressed += () => SelectWeapon(SpearType.Gravity, GravityButton);
		if (ExplosiveButton != null) ExplosiveButton.Pressed += () => SelectWeapon(SpearType.Explosive, ExplosiveButton);
		
		if (StartGameButton != null)
		{
			StartGameButton.Pressed += () => 
			{
				// --- THE VISIBILITY FIX ---
				// 1. Instantly hide the full UI layer structure so it doesn't linger on screen
				Visible = false;
				
				// 2. Set the global arcade parameters running
				ArcadeSaveSystem.IsGamePlaying = true;
				ArcadeSaveSystem.IsGameOver = false;

				// 3. Lock the mouse cursor back to standard look-around mode for gameplay
				Input.MouseMode = Input.MouseModeEnum.Captured;
				
				GD.Print("🎮 MATCH START: Loadout menu hidden, entering arena.");
			};
		}
	}

	private void SelectWeapon(SpearType type, Button pressedButton)
	{
		ArcadeSaveSystem.SelectedSpear = type;
		if (StartGameButton != null) StartGameButton.Disabled = false;

		if (LightningButton != null) LightningButton.SelfModulate = _defaultColor;
		if (GravityButton != null) GravityButton.SelfModulate = _defaultColor;
		if (ExplosiveButton != null) ExplosiveButton.SelfModulate = _defaultColor;

		pressedButton.SelfModulate = _selectedColor;
	}
}
