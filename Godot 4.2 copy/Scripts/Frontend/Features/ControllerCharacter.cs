using Godot;
using System;
using MathPlus;

public partial class ControllerCharacter : CharacterBody3D {
	// General config cuz theres way to much shit too keep track of....
	public static readonly Godot.Collections.Dictionary<string,Variant> ConfigStruct = new Godot.Collections.Dictionary<string,Variant> {         
		["General"] = new Godot.Collections.Dictionary<string,Variant> {
			["CameraHeight"] = 10,
			["CameraSpeedNormal"] = 8,
			["CameraSpeedFast"] = 16,
		},

		["Interpolations"] = new Godot.Collections.Dictionary<string,Variant> {
		},
	};
	// privs
	private Vector3 MovementInterp = Vector3.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double _Delta)
	{
	}
}
