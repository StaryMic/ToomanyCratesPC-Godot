using Godot;
using System;

[GlobalClass]
public partial class Act_LevelChange : Node
{
	// External References
	[Export] private Interactable _interactable;

	[Export] private string _levelToLoad;
	[Export] private bool _triggerSave;
	
	public override void _Ready()
	{
		_interactable.Interact += InteractableOnInteract;
	}

	private void InteractableOnInteract()
	{
		LevelManager.instance.EmitSignal(LevelManager.SignalName.RequestLevelChange, _levelToLoad, _triggerSave);
	}
}
