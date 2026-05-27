using Godot;

public class EnemyData
{
	// These must be public and match your JSON keys exactly!
	public string name { get; set; }
	public float health { get; set; }
	public float speed { get; set; }
	public string scene_path { get; set; }
	
	public float scale_uniform { get; set; }
	public float scale_y { get; set; }
}
