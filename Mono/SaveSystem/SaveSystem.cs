using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class SaveSystem : Node
{
	// Allow global autoload stuff
	public static SaveSystem instance { get; set; }
	
	// Internal Variables
	private Array<RigidBody3DPlus> _crateList = new();
	private Array<Dictionary<string, Variant>> _saveData = new();
	
	// Node References
	LevelManager _levelManager;
	
	public override void _Ready()
	{
		LevelManager.instance.LevelFinishedLoading += Load;
		instance = this;
	}
	
	public Task Save()
	{
		// Make directory for saves if it doesn't exist yet.
		if (!DirAccess.DirExistsAbsolute("user://saves"))
		{
			DirAccess.MakeDirAbsolute("user://saves");
		}
		
		// Get level name and make a file with the name of the level. (Or open if it already exists.)
		string levelname = GetTree().CurrentScene.Name;
		FileAccess saveFile = FileAccess.Open($"user://saves/{levelname}.json", FileAccess.ModeFlags.WriteRead);
		
		// DEBUG: Check for errors in FileAccess
		GD.Print(saveFile.ToString());
		
		// If the file does exist. Null the whole file.
		saveFile.Resize(0);
		
		// Grab all savable nodes and put them in a list.
		foreach (var node in GetTree().GetNodesInGroup("Crate"))
		{ 
			GD.Print("SaveSystem: Added crate to list"); 
			_crateList.Add(node as RigidBody3DPlus);
		}
		
		// Make a dictionary entry for each crate and store the necessary data
		foreach (var crate in _crateList)
		{
			// Make Dict.
			Dictionary<string, Variant> data = new();
			
			// Grab data and store it. Vectors don't work in JSON.
			data.Add("Path", crate.GetPath().ToString());
			data.Add("Position", new Array{crate.GlobalPosition.X, crate.GlobalPosition.Y, crate.GlobalPosition.Z});
            data.Add("Rotation", new Array{crate.GlobalRotation.X, crate.GlobalRotation.Y, crate.GlobalRotation.Z});
            data.Add("LinearVelocity", new Array{crate.LinearVelocity.X, crate.LinearVelocity.Y, crate.LinearVelocity.Z});
            data.Add("AngularVelocity", new Array{crate.AngularVelocity.X, crate.AngularVelocity.Y, crate.AngularVelocity.Z});
            data.Add("Sleeping", crate.Sleeping);
            data.Add("Freeze", crate.Freeze);
            data.Add("Health", crate.GetNode<Destructable>("Destructable").Health);
            
            // Store in list
            _saveData.Add(data);
		}
		
		// DEBUG: Print out save data.
		foreach (var dict in _saveData)
		{
			GD.Print(dict["Path"]);
		}
		
		// Convert dict to JSON.
		Json json = new();
		string stringData = Json.Stringify(_saveData, "\t", false);
		
		// Store JSON-ified data to file.
		saveFile.StoreString(stringData);
		
		// Close the file.
		saveFile.Close();

		return Task.CompletedTask;
	}

	public void Load()
	{
		throw new NotImplementedException();
	}
}
