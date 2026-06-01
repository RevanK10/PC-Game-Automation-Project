using Godot;
using System;

public partial class Main : Node3D
{
	public override void _Ready()
	{
		
	}

	public override void _Input(InputEvent @event)
	{
		// 1. ESCAPE KEY: Explicit emergency mouse release
		if (@event.IsActionPressed("ui_cancel") || (@event is InputEventKey escapeEvent && escapeEvent.Keycode == Key.Escape))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		// 2. F KEY: Reversible visibility toggle 
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.F)
			{
				// If the cursor is currently locked, free it. Otherwise, hide it.
				if (Input.MouseMode == Input.MouseModeEnum.Captured)
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
				}
				else
				{
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
			}
		}
	}
}
