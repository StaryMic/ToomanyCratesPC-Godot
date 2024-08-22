using Godot;
using System;

[GlobalClass]
public partial class Interactable : Node
{
	[Signal] public delegate void InteractEventHandler();

	public override void _Ready()
	{
		Interact += OnInteract;
	}

	private void OnInteract()
	{
		GD.Print("Interacting!");
	}
}
