using Godot;
using System;

public partial class LevelManager : Node
{
	public static LevelManager instance { get; set; }
	
	// Internal Variables
	public string LevelName;
	
	// External References
	
	// Signals
	[Signal] public delegate void RequestLevelChangeEventHandler(string levelToLoad, bool triggerSave);

	[Signal] public delegate void LevelFinishedLoadingEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		instance = this;
		RequestLevelChange += OnRequestLevelChange;
		LevelName = GetTree().CurrentScene.Name;
	}

	private void OnRequestLevelChange(string levelToLoad, bool triggerSave)
	{
		if (triggerSave)
		{
			GD.Print("SAVE CALLED");
			SaveSystem.instance.Save().Wait();
		}

		if (GetTree().ChangeSceneToFile(levelToLoad) != Error.Ok)
		{
			OS.Alert($"Failed to load level {levelToLoad}", "Please report to the dev!");
			GD.PrintErr($"{levelToLoad} has failed to load!");
		}
		
		EmitSignal(SignalName.LevelFinishedLoading);
	}
}
