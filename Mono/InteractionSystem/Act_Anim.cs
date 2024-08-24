using Godot;

[GlobalClass]
public partial class Act_Anim : Node
{
	[Export] private Interactable Interactable;
	[Export] private AnimationPlayer AnimationPlayer;
	[Export] private string AnimationName;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Interactable is null)
		{
			GD.PrintErr(this.Name + ": Interactable Source is not set.");
		}

		if (AnimationPlayer is null)
		{
			GD.PrintErr(this.Name + ": AnimationPlayer Source is not set.");
		}

		if (AnimationPlayer is not null && Interactable is not null)
		{
			Interactable.Interact += InteractableOnInteract;
		}
		
	}

	private void InteractableOnInteract()
	{
		if(!AnimationPlayer.IsPlaying())
		{
			AnimationPlayer.Play(AnimationName);
		}
	}

	public override void _Process(double delta)
	{
		DebugDraw3D.DrawLine(this.GetParent<Node3D>().GlobalPosition, Interactable.GetParent<Node3D>().GlobalPosition);
	}
}
