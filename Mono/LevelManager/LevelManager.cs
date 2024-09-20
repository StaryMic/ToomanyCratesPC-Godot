using Godot;
using System;

public partial class LevelManager : Node
{
	public static LevelManager instance { get; set; }
	
	// Internal Variables
	
	
	// External References
	
	// Signals
	[Signal] public delegate void RequestLevelChangeEventHandler(string levelToLoad, bool triggerSave);

	[Signal] public delegate void LevelFinishedLoadingEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RequestLevelChange += OnRequestLevelChange;

		instance = this;
	}

	private void OnRequestLevelChange(string levelToLoad, bool triggerSave)
	{
		if (triggerSave)
		{
			GD.Print("save triggered but not implemented rn.");
			// Call save function later
		}

		if (GetTree().ChangeSceneToFile(levelToLoad) != Error.Ok)
		{
			OS.Alert($"Failed to load level {levelToLoad}", "TooManyCrates has crashed!");
			GD.PrintErr($"{levelToLoad} has failed to load!");
		}
		
		EmitSignal(SignalName.LevelFinishedLoading);
	}
}
