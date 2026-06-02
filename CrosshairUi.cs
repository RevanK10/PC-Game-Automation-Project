using Godot;
using System;

// FIX: Changed "CrosshairUI" to "CrosshairUi" to exactly match your file system name
public partial class CrosshairUi : Control
{
	public override void _Ready()
	{
		// Center the dot mathematically to bypass any inspector layout issues
		var centerDot = GetNodeOrNull<ColorRect>("CenterDot");
		if (centerDot != null)
		{
			centerDot.Position = -centerDot.Size / 2.0f;
		}
	}

	public override void _Process(double delta)
	{
		// Only display the crosshair overlay if the arcade match has officially started
		Visible = ArcadeSaveSystem.IsGamePlaying;
	}
}
