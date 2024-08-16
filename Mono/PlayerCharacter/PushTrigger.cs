using Godot;
using System;
using System.Collections.Generic;

public partial class PushTrigger : Area3D
{
	// Pushy Pushy list
	private List<RigidBody3D> _pushList = new List<RigidBody3D>();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.BodyEntered += OnBodyEntered;
		this.BodyExited += OnBodyExited;
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is RigidBody3D _rigidBody)
		{
			if (_pushList.Contains(_rigidBody))
			{
				_pushList.Remove(_rigidBody);
				GD.Print("removed from list");
			}
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is RigidBody3D _rigidBody)
		{
			_pushList.Add(_rigidBody);
			GD.Print("added to list");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (var body in _pushList)
		{
			body.Sleeping = false;
			body.ApplyForce((body.GlobalPosition - this.GlobalPosition) * 4);
		}
	}
}
