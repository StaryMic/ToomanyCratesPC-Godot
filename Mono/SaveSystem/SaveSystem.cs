using Godot;
using System;

public partial class SaveSystem : Node
{
	// Allow global autoload stuff
	public static SaveSystem instance { get; set; }
	
	// Internal Variables
	
	
	// Node references
	
	
	// Signals
	
	public override void _Ready()
	{
		instance = this;
	}
	
	public void OnSave()
	{
		throw new NotImplementedException();
	}

	public void OnLoad()
	{
		throw new NotImplementedException();
	}
}
