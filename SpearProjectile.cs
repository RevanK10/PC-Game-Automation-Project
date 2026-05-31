using Godot;

public partial class SpearProjectile : RigidBody3D
{
	private bool _stuck = false;
	[Export] public float Lifetime = 15.0f;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		// Always start the despawn timer on spawn
		GetTree().CreateTimer(Lifetime).Timeout += () => 
		{ 
			if (IsInstanceValid(this)) QueueFree(); 
		};
	}

	public void Launch(Vector3 direction, float force)
	{
		ApplyCentralImpulse(direction * force);
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player) return;

		if (!_stuck)
		{
			_stuck = true;
			Freeze = true; 
			SetDeferred("contact_monitor", false);

			// --- UPGRADED: SPEAR AUDIO TRIGGER ENGINE ---
			// Locate our sound component and play it immediately on contact
			var impactSound = GetNodeOrNull<AudioStreamPlayer3D>("ImpactAudio");
			if (impactSound != null)
			{
				impactSound.Play();
			}

			// Check if the entity we ran into belongs to our dynamic enemy group
			if (body.IsInGroup("enemies"))
			{
				// Deal damage immediately
				if (body is Enemy enemy)
				{
					enemy.TakeDamage(25.0f);
				}

				// Defer the parent switch so the physics engine doesn't panic
				CallDeferred(MethodName.StickToTarget, body);
			}
		}
	}

	private void StickToTarget(Node3D targetBody)
	{
		// Safety check to ensure the enemy hasn't already been destroyed by the damage above
		if (!IsInstanceValid(targetBody)) return;

		// 1. Capture exactly where the spear is in global 3D space right now
		Vector3 currentGlobalPosition = GlobalPosition;
		Basis currentGlobalBasis = GlobalBasis;

		// 2. Shut off any visual/physics loops on this spear node so it behaves strictly as a cosmetic attachment
		// NOTE: ProcessModeEnum.Disabled is perfectly fine here! The AudioStreamPlayer3D will finish 
		// playing its sound because it was already triggered and started running on the native audio thread.
		ProcessMode = ProcessModeEnum.Disabled;

		// Disable the spear's collision shapes so it doesn't bump into other flying objects
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D collisionShape)
			{
				collisionShape.Disabled = true;
			}
		}

		// 3. Unhook from the map floor, and attach directly onto the enemy structure
		if (GetParent() != null)
		{
			GetParent().RemoveChild(this);
		}
		targetBody.AddChild(this);

		// 4. Force the spear back to its exact impact point. Godot will automatically recalculate
		// these values relative to the moving enemy parent node!
		GlobalPosition = currentGlobalPosition;
		GlobalBasis = currentGlobalBasis;
	}
}
