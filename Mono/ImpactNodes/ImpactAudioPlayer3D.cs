using Godot;
using System;

[GlobalClass]
public partial class ImpactAudioPlayer3D : AudioStreamPlayer3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Finished += OnFinished;
	}

	private void OnFinished()
	{
		this.QueueFree();
	}
}
