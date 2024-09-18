using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class DyingFadeOut : Node
{
	[Export] private float _fadeDelay;
	[Export] private float _fadeTime;
	[Export] private MeshInstance3D meshToFade;
	private Tween tween;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		tween = CreateTween();
		tween.TweenInterval(_fadeDelay);
		tween.TweenProperty(meshToFade, "transparency", 1, _fadeTime);
		tween.SetEase(Tween.EaseType.Out);
		tween.Finished += TweenOnFinished;
	}

	private void TweenOnFinished()
	{
		this.GetParent().QueueFree();
	}
}
