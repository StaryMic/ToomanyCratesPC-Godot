using Godot;
using System;

[GlobalClass]
public partial class Destructable : Node3D
{
	// Exported Variables and Scenes
	[ExportGroup("Health")]
	[Export] private int _health;
	[Export] private int _maxHealth; // May go unused
	
	[ExportGroup("Effects")]
	[Export] private PackedScene _gibParticlePackedScene;
	[Export] private AudioStreamRandomizer _damageAudioStreams;
	[Export] private AudioStreamRandomizer _breakAudioStreams;
	
	// Signals
	[Signal] public delegate void DamageEventHandler(int damage);
	[Signal] public delegate void BreakEventHandler();
	// To be used by a future crate manager or anything that needs to detect a break
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Damage += OnDamage;
	}

	private void OnDamage(int damage)
	{
		_health -= damage;

		if (_health <= 0 && !this.GetParent().IsQueuedForDeletion())
		{
			if (_breakAudioStreams != null)
			{
				// Play breaking audio
				ImpactAudioPlayer3D breakAudio = new ImpactAudioPlayer3D();
				breakAudio.Autoplay = true;
				breakAudio.Stream = _breakAudioStreams;
				this.GetTree().Root.GetChild(-1).AddChild(breakAudio);
				breakAudio.GlobalPosition = this.GetParent<Node3D>().GlobalPosition;
			}
			
			if(_gibParticlePackedScene != null)
			{
				ImpactGPUParticles impactGPUParticles = _gibParticlePackedScene.Instantiate<ImpactGPUParticles>();
				this.GetTree().Root.GetChild(-1).AddChild(impactGPUParticles);
				impactGPUParticles.GlobalPosition = this.GetParent<Node3D>().GlobalPosition;
			}
			
			// Queue deletion
			this.GetParent().QueueFree();
		}
		
		if (_damageAudioStreams != null && !this.GetParent().IsQueuedForDeletion())
		{
			ImpactAudioPlayer3D impactAudioPlayer3D = new ImpactAudioPlayer3D();
			// Select a random damage sound
			impactAudioPlayer3D.Stream = _damageAudioStreams;
			impactAudioPlayer3D.Autoplay = true;
			this.GetTree().Root.GetChild(-1).AddChild(impactAudioPlayer3D);
			impactAudioPlayer3D.GlobalPosition = this.GetParent<Node3D>().GlobalPosition;
		}
	}
}
