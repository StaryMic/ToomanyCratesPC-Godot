using Godot;
using System;

[GlobalClass]
public partial class DevText : MeshInstance3D
{
	
	// External refs
	[Export] private string _text;
	[Export] private StandardMaterial3D _devMaterial;
	
	// Internal refs
	private PlayerCharacter _player;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_player = (PlayerCharacter)this.GetTree().CurrentScene.GetNode("PlayerCharacter");
		this.Mesh = new TextMesh();
		this.Mesh.SurfaceSetMaterial(0,_devMaterial);
		if (this.Mesh is TextMesh textMesh)
		{
			textMesh.Text = _text;
			textMesh.Depth = 0f;
		}
	}

	public override void _Process(double delta)
	{
		if (_player != null)
		{
			LookAt(_player.CameraAnchor.GlobalPosition, Vector3.Up, true);
		}
	}
}
