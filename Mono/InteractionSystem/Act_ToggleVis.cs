using Godot;
using System;

[GlobalClass]
public partial class Act_ToggleVis : Node
{
	[Export] private Interactable _interactable;
	[Export] private Node3D _target;
	public override void _Ready()
	{
		_interactable.Interact += InteractableOnInteract;
	}

	private void InteractableOnInteract()
	{
		_target.Visible = !_target.Visible;
	}

	public override void _Process(double delta)
	{
		DebugDraw3D.DrawLine(this.GetParent<Node3D>().GlobalPosition, _interactable.GetParent<Node3D>().GlobalPosition);
	}
}
