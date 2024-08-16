using Godot;
using System;

public partial class InteractableGrab : Interactable
{
	public override void _Ready()
	{
		Interact += OnInteract;
	}

	private void OnInteract()
	{
		
	}
}
