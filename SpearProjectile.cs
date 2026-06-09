using Godot;
using System;

public partial class SpearProjectile : RigidBody3D
{
	private bool _stuck = false;
	[Export] public float Lifetime = 15.0f;

	// --- LOADOUT PROPERTIES ---
	public SpearType ProjectileType { get; set; } = SpearType.None;

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

	// Helper method expected by the menu transition hooks
	public void SetSpecialGlow(bool active)
	{
		// 1. FIXED: If it's a standard spear, exit instantly!
		// This leaves your imported .obj and 2 .png textures completely alone.
		if (ProjectileType == SpearType.None)
		{
			return;
		}

		// 2. Otherwise, find the mesh to apply the arcade special tint effects
		var mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D") ?? 
				   GetNodeOrNull<MeshInstance3D>("SpearMesh");

		if (mesh == null) return;

		StandardMaterial3D mat = mesh.MaterialOverride as StandardMaterial3D;
		
		// Safe texture-tint fallback for special variants using the base mesh surfaces
		if (mat == null && mesh.Mesh != null)
		{
			var surfaceMat = mesh.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
			if (surfaceMat != null)
			{
				mat = (StandardMaterial3D)surfaceMat.Duplicate();
				mesh.MaterialOverride = mat;
			}
		}
		else if (mat != null)
		{
			mat = (StandardMaterial3D)mat.Duplicate();
			mesh.MaterialOverride = mat;
		}

		if (mat == null) return;

		// Apply the arcade glowing properties onto the special variations
		if (active)
		{
			mat.EmissionEnabled = true;
			mat.EmissionEnergyMultiplier = 3.0f;

			switch (ProjectileType)
			{
				case SpearType.Lightning:
					mat.AlbedoColor = new Color(0.4f, 0.8f, 1.0f); // Translucent blue tint
					mat.Emission = new Color(0.0f, 0.6f, 1.0f);
					break;

				case SpearType.Gravity:
					mat.AlbedoColor = new Color(0.7f, 0.2f, 1.0f); // Purple tint
					mat.Emission = new Color(0.4f, 0.0f, 0.8f);
					break;

				case SpearType.Explosive:
					mat.AlbedoColor = new Color(1.0f, 0.4f, 0.2f); // Orange/Red tint
					mat.Emission = new Color(1.0f, 0.2f, 0.0f);
					break;
			}
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player) return;

		if (!_stuck)
		{
			_stuck = true;
			Freeze = true; 
			SetDeferred("contact_monitor", false);

			var impactSound = GetNodeOrNull<AudioStreamPlayer3D>("ImpactAudio");
			if (impactSound != null) impactSound.Play();

			// --- DETECT ARCADE MODIFIER MECHANICS ---
			if (ProjectileType == SpearType.Explosive)
			{
				TriggerExplosionEffect();
			}
			else if (body.IsInGroup("enemies") && body is Enemy enemy)
			{
				if (ProjectileType == SpearType.Lightning)
				{
					TriggerLightningStun(enemy);
				}
				else if (ProjectileType == SpearType.Gravity)
				{
					TriggerGravityVortex();
				}
				else
				{
					// Default normal spear damage
					enemy.TakeDamage(25.0f);
				}

				CallDeferred(MethodName.StickToTarget, body);
			}
			else if (ProjectileType == SpearType.Gravity)
			{
				// If gravity spear hits the floor or wall instead of an enemy, still pull them in!
				TriggerGravityVortex();
			}
		}
	}

	private void StickToTarget(Node3D targetBody)
	{
		if (!IsInstanceValid(targetBody)) return;

		Vector3 currentGlobalPosition = GlobalPosition;
		Basis currentGlobalBasis = GlobalBasis;

		ProcessMode = ProcessModeEnum.Disabled;

		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D collisionShape)
			{
				collisionShape.Disabled = true;
			}
		}

		if (GetParent() != null)
		{
			GetParent().RemoveChild(this);
		}
		targetBody.AddChild(this);

		GlobalPosition = currentGlobalPosition;
		GlobalBasis = currentGlobalBasis;
	}
	
	// ⚡ 1. LIGHTNING STUN MECHANIC
	private void TriggerLightningStun(Enemy enemy)
	{
		enemy.TakeDamage(15.0f); // Light impact damage
		
		// Temporal speed freeze override
		float originalSpeed = enemy.Speed;
		enemy.Speed = 0.0f; 
		GD.Print($"⚡ {enemy.EnemyName} is electrocuted and paralyzed!");

		// Release from stun after 3 full seconds
		GetTree().CreateTimer(3.0f).Timeout += () =>
		{
			if (IsInstanceValid(enemy))
			{
				enemy.Speed = originalSpeed;
				GD.Print($"⚡ {enemy.EnemyName} recovered from electrocution.");
			}
		};
	}

	// 🌌 2. GRAVITY VORTEX PULL MECHANIC
	private void TriggerGravityVortex()
	{
		float pullRadius = 12.0f;
		float pullForce = 18.0f;
		
		GD.Print("🌌 Gravity singularity activated! Pulling enemies...");
		
		Vector3 vortexCenter = GlobalPosition;
		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node node in enemies)
		{
			if (node is Enemy enemy && IsInstanceValid(enemy))
			{
				float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
				if (distance <= pullRadius && distance > 0.1f)
				{
					Vector3 pullDirection = vortexCenter - enemy.GlobalPosition;
					pullDirection.Y = 0.0f;
					pullDirection = pullDirection.Normalized();
					
					float distanceScale = Mathf.Clamp(distance/pullRadius, 0.2f, 1.0f);
					Vector3 appliedForce = pullDirection * pullForce * distanceScale;
					enemy.ApplyExternalForce(appliedForce, 0.5f);
				}
			}
		}
	}

	// 💥 3. HIGH EXPLOSIVE AOE MECHANIC
	private void TriggerExplosionEffect()
	{
		float blastRadius = 8.0f;
		float maxBlastDamage = 75.0f;

		GD.Print("💥 BOOM! Detonating payload...");

		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node node in enemies)
		{
			if (node is Enemy enemy && IsInstanceValid(enemy))
			{
				float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
				if (distance <= blastRadius)
				{
					float falloffFactor = 1.0f - (distance / blastRadius);
					float finalCalculatedDamage = maxBlastDamage * falloffFactor;

					enemy.TakeDamage(Mathf.Max(finalCalculatedDamage, 15.0f));

					Vector3 knockbackDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
					knockbackDir.Y = 0.2f; 
					enemy.ApplyExternalForce(knockbackDir * 25.0f, 0.4f);
				}
			}
		}

		QueueFree();
	}
}
