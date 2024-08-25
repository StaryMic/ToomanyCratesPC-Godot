using Godot;
using System;

public partial class TeleportTrigger : Area3D
{
	[Export] public Node3D TeleportPosition;

	public override void _Ready()
	{
		this.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is PlayerCharacter player)
		{
			player.GlobalPosition = TeleportPosition.GlobalPosition;
		}
	}
}
