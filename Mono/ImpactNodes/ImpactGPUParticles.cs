using Godot;
using System;

[GlobalClass]
public partial class ImpactGPUParticles : GpuParticles3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Emitting = true;
		this.Finished += OnFinished;
	}

	private void OnFinished()
	{
		this.QueueFree();
	}
}
