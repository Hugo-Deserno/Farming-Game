using Godot;
using System;
using System.Collections.Generic;

public partial class CommandConsole : Node
{
	/*
		Quick expo for my smooth brain

		The command dicts are for creatings paths
		so for example Game:loadSave
		would be
		Stack1["Game"] : Stack2["LoadSave"]
	*/

	// First nest in command
	public static Dictionary<string,Variant> COMMAND_STACK = new Dictionary<string, Variant>{
		["Stack1"] = new Godot.Collections.Dictionary<string,string>{
			["Grid"] = null,
			["Placement"] = null,
			["Inventory"] = null,
			["Data"] = null,
		},
		["Stack2"] = new Godot.Collections.Dictionary<string,string>{
			["LoadArchive"] = "Data", // loads file
			["SaveArchive"] = "Data", // saves file
			
			["InventoryArchive"] = "Inventory", // prints the entire inventory;
			["Add"] = "Inventory", // Adds a item to the inventory

			["ShowVoxels"] = "Grid", // shows voxel grid
			["ShowGrid"] = "Grid", // shows normal grid
			["ShowVoxelState"] = "Grid", // shows voxel states

			["StartPlacing"] = "Placement", // starts placement
		}
	};

	// Config
	public static readonly string DEFAULT_TEXT = "Press '/' to start typing.";
	// Main garbage
	public static bool CONSOLE_ENABLED = false;
	public static bool GAME_ENABLED = false;
	// Objects
	public static TextEdit CONSOLE;
	public static TextEdit LSP_CONSOLE;
	public static Node3D NODE_WORKSPACE;

	// startup
    public override void _Ready() {
		CONSOLE = GetNode<TextEdit>("ConsoleController/Console");
		LSP_CONSOLE = GetNode<TextEdit>("ConsoleController/LSP");
		NODE_WORKSPACE = GetNode<Node3D>("/root/Workspace");

		// Resets the console
		void CleanConsole() {
			CONSOLE.Text = CONSOLE.Text == "" ? DEFAULT_TEXT : CONSOLE.Text;
			LSP_CONSOLE.Text = CONSOLE.Text;
		}
		CONSOLE.FocusEntered += () => CONSOLE.Text = "";
		CONSOLE.FocusExited += () => CleanConsole();
		CONSOLE.TextChanged += () => CommandLSP.UpdateLSPConsole();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double _Delta) {
		if(Input.IsActionJustPressed("Command_Console")) {
			CONSOLE_ENABLED = CONSOLE_ENABLED ? false : true;
			CONSOLE.Visible = CONSOLE_ENABLED;
			LSP_CONSOLE.Visible = CONSOLE_ENABLED;

			CONSOLE.Text = DEFAULT_TEXT;
		}

		CommandLSP.ParseLSP();
	}

    public override void _Input(InputEvent _InputEvent) {
		if(_InputEvent is InputEventKey CurrentKey) {
			if(CurrentKey.PhysicalKeycode == Key.Enter && CONSOLE.HasFocus()) {
				CONSOLE.ReleaseFocus();
				CommandExecuter.ExecuteCommand(CONSOLE.Text);
			} else if(CurrentKey.PhysicalKeycode == Key.Slash) {
				CONSOLE.GrabFocus();
				CONSOLE.Text = "";
				LSP_CONSOLE.Text = CONSOLE.Text;
			} else if (CurrentKey.PhysicalKeycode == Key.Space && CONSOLE.HasFocus()) {
				string LSPCorrection = CommandLSP.MergeLockOn();
				CONSOLE.Text = LSPCorrection;
				CONSOLE.Text = CONSOLE.Text.Replace(" ","");

				// Moves the cursor into the scope
				if(CONSOLE.Text[CONSOLE.Text.Length - 1] == ')') {
					CONSOLE.SetCaretColumn(CONSOLE.Text.Length - 1);
				} else {
					CONSOLE.SetCaretColumn(CONSOLE.Text.Length);
				}
			}
		}
    }
}

// Executes the commands
public partial class CommandExecuter : CommandConsole {
	// Converts the execution stack to paramters
	public static void SolveExecutionStack(string _CurrentText, string _Command) {
		char[] CommandIndents = new char[]{',','(',')'};
		string[] Parameters = CONSOLE.Text.ToString().Split(CommandIndents);
		Dictionary<int,object> ParametersVariablized = new Dictionary<int, object>();

		// loop through the parameters, but we start at 1 instead of 0
		// beacuse 0 is the command in this case		
		for(int CurrentParameterIndex = 1; CurrentParameterIndex <10; CurrentParameterIndex++) { 
			string CurrentParameter = Parameters[CurrentParameterIndex];

			// Settings Vars
			if(CurrentParameter.ToLower() == "true") {
				ParametersVariablized[ParametersVariablized.Count] =  true;
			} else if(CurrentParameter.ToLower() == "false") {
				ParametersVariablized[ParametersVariablized.Count] =  false;
			} else if(float.TryParse(CurrentParameter, out _) ) {
				ParametersVariablized[ParametersVariablized.Count] = (float)Convert.ToSingle(CurrentParameter);
			} else {
				ParametersVariablized[ParametersVariablized.Count] =  CurrentParameter;
			}

			// Detects when it needs to be executed
			if(Parameters.Length - 1 == CurrentParameterIndex) {
				ExecuteStack(ParametersVariablized,_Command);
				break;
			}
		}
	}

	// determins and gather all the shit needed to fire a command
	public static void ExecuteCommand(string _CurrentText) {
		char[] CommandIndents = new char[]{':','('};
		string[] CommandChunks = CONSOLE.Text.ToString().Split(CommandIndents);
		
		for(int StackNest = 1; StackNest < 10; StackNest++) {
			Godot.Collections.Dictionary<string,string> StackIndent = (Godot.Collections.Dictionary<string,string>)COMMAND_STACK["Stack" + StackNest];

			// Sifts Through the command stack if its a valid command
			if(StackIndent.ContainsKey(CommandChunks[StackNest - 1]) && _CurrentText[_CurrentText.Length - 1] == ')') {
				if(!COMMAND_STACK.ContainsKey("Stack" + (1 + StackNest))) {
					SolveExecutionStack(_CurrentText,CommandChunks[StackNest - 1]);
					break;
				}
			} else {
				break;
			}
		}
	}

	// Executes cmds
	public static void ExecuteStack(Dictionary<int,object> _Parameters, string _Command) {
		if(_Command == "LoadArchive") {
			// Checks if number is int
			if(((Variant)((int)Convert.ToInt32(_Parameters[0]))).VariantType == Variant.Type.Int) {
				int SaveNumber = (int)Convert.ToInt32(_Parameters[0]);
				if(GAME_ENABLED) {return;}
				GAME_ENABLED = true;
				// Load File
				DataRepository.IntializeRepo(SaveNumber);

				// Load Scene
				PackedScene CurrentSelectedItemScene = GD.Load<PackedScene>("res://Packages/Game.tscn");
				Node3D CurrentSelectedItem = CurrentSelectedItemScene.Instantiate<Node3D>();
				NODE_WORKSPACE.AddChild(CurrentSelectedItem);
			}
		} else if(_Command == "SaveArchive") {
			if(!GAME_ENABLED) {return;}
			DataRepository.SaveDataToJson();
		} else if(_Command == "ShowVoxels") {
			if(!GAME_ENABLED) {return;}
			VoxelGrid.VisualizeAffectedVoxelGrid(false);
			VoxelGrid.VisualizeNormalGrid(false);
			VoxelGrid.VisualizeVoxelGrid((bool) _Parameters[0]);
		} else if(_Command == "ShowVoxelState") {
			if(!GAME_ENABLED) {return;}
			VoxelGrid.VisualizeVoxelGrid(false);
			VoxelGrid.VisualizeNormalGrid(false);
			VoxelGrid.VisualizeAffectedVoxelGrid((bool)_Parameters[0]);
		} else if(_Command == "ShowGrid") {
			if(!GAME_ENABLED) {return;}
			VoxelGrid.VisualizeAffectedVoxelGrid(false);
			VoxelGrid.VisualizeVoxelGrid(false);
			VoxelGrid.VisualizeNormalGrid((bool)_Parameters[0]);
		} else if(_Command == "StartPlacing") {
			if(!GAME_ENABLED) {return;}

			GridHandler.PLACING_SYSTEM = new GridSystem3D((string)_Parameters[0]);
			GridHandler.PLACING_SYSTEM.Enable();
		} else if(_Command == "InventoryArchive") {
			if(!GAME_ENABLED) {return;}
			InventoryData.DebugInventory(Inventory.INVENTORY);
		} else if(_Command == "Add") {
			if(!GAME_ENABLED) {return;}

			int Amount = (int)Convert.ToInt32(_Parameters[1]);
			InventoryData.AddItemToDataBase((string)_Parameters[0],Amount,null,true);
			GD.Print("Added " + Amount + " Of " + (string)_Parameters[0] + " To your inventory");
		}
	}
}

// Language support for command console
public partial class CommandLSP : CommandConsole {
	private static string CurrentLockOnAnswer = null;

	// Gets the option
	public static void ParseLSP() {
		char[] CommandIndents = new char[]{':','('};
		string[] CommandChunks = CONSOLE.Text.ToString().Split(CommandIndents);
		CurrentLockOnAnswer = "";
		int MaxCompStack = 0;

		if(!COMMAND_STACK.ContainsKey("Stack" + CommandChunks.Length)) {
			CurrentLockOnAnswer = "";
			UpdateLSPConsole();
			return;
		}

		// Line gets the needed command stack
		Godot.Collections.Dictionary<string,string> CurrentCommandStack = (Godot.Collections.Dictionary<string,string>)COMMAND_STACK["Stack" + CommandChunks.Length];
		foreach((string CommandName, string Descendants) in CurrentCommandStack) {
			string LowerdCommandName = CommandName.ToLower();
			string LowerCommandStack = (CommandChunks[CommandChunks.Length - 1]).ToLower();

			// check iof its in the right tree and a valid descendant
			// or just makes a run for it if index is out of range :)
			if(CommandChunks.Length - 2 >= 0 && CommandChunks[CommandChunks.Length - 2] == Descendants || CommandChunks.Length - 2 < 0) {
				// Do Actual matching
				int CompStack = 0; // keeping track of many matches string has
				for(int Increment = 0; Increment < LowerCommandStack.Length; Increment++) {
					// cut short if line is invalid
					if(Increment > LowerdCommandName.Length - 1) {
						break;
					}

					char CurrentLetter = LowerdCommandName[Increment];
					char CurrentStackLetter = LowerCommandStack[Increment];

					// check each letter and compare if they match
					// if not then die
					if(CurrentLetter == CurrentStackLetter) {
						CompStack++;
					} else {
						CompStack = 0;
						break;
					}
				}

				// if its higher then the last stack, update it
				if(CompStack > MaxCompStack) {
					MaxCompStack = CompStack;
					CurrentLockOnAnswer = CommandName;
				}
			}
		}

		// updatesTheText
		UpdateLSPConsole();
	}

	// Locks onto the asnwer
	public static string MergeLockOn() {
		char[] CommandIndents = new char[]{':','('};
		string[] CommandChunks = CONSOLE.Text.ToString().Split(CommandIndents);

		string FinalizedString = "";

		if(CurrentLockOnAnswer == "") {
			return CONSOLE.Text;
		}

		// Combines all the elements
		for(int Index = 0; Index < CommandChunks.Length; Index++) {
			if(Index + 1 == CommandChunks.Length) {
				if(IsAtEnOfStack(CurrentLockOnAnswer)) {
					FinalizedString += CurrentLockOnAnswer + "()";
				} else {
					FinalizedString += CurrentLockOnAnswer + ":";
				}
			} else {
				FinalizedString += CommandChunks[Index];
				FinalizedString += ":";
			}
		}

		return FinalizedString;
	}

	// Determins if its the last stack of descandent (for addign brackets to lsp)
	public static bool IsAtEnOfStack(string _CommandName) {
		for(int StackIncrement = 0; StackIncrement < COMMAND_STACK.Count; StackIncrement++) {
			if(!COMMAND_STACK.ContainsKey("Stack" + (StackIncrement + 1))) {
				break;
			} else {
				Godot.Collections.Dictionary<string,string> StackDict = (Godot.Collections.Dictionary<string,string>)COMMAND_STACK["Stack" + (StackIncrement + 1)];
			
				foreach((string _,string Refrence) in StackDict) {
					if(Refrence == _CommandName) {
						return false;
					}
				}
			}
		}
		return true;
	}

	// Updates the lsp text
	public static void UpdateLSPConsole() {
		LSP_CONSOLE.Text = CONSOLE.Text;

		// AND WE DO IT AGAIN
		string MergeCommands(string[] CommandChunks) {
			string FinalizedString = "";

			// Combines all the elements
			for(int Index = 0; Index < CommandChunks.Length; Index++) {
				if(Index + 1 != CommandChunks.Length) {
					FinalizedString += CommandChunks[Index];
					FinalizedString += ":";
				}
			}

			return FinalizedString;
		}

		// we use mergelock on cuz im a lazy bitch and id wanna make another algo
		// for sorting that shit out. So deal with it.

		// JK, i did it anyways
		if(CurrentLockOnAnswer != null) {
			//LSP_CONSOLE.Text = MergeLockOn();

			char[] CommandIndents = new char[]{':','('};
			string[] CommandChunks = CONSOLE.Text.ToString().Split(CommandIndents);

			if(CommandChunks.Length == 0 || CurrentLockOnAnswer == "" || CONSOLE.Text.Length > 0 && CONSOLE.Text[CONSOLE.Text.Length - 1] == ')') {
				return;
			}

			// Creates a new string from the command which is used to trim of the stack lock on
			string RecentCommand = CommandChunks[CommandChunks.Length - 1];
			string ReformedCommand = "";

			// welds the stuff togheter
			for(int CaretPosition = 0; CaretPosition < RecentCommand.Length; CaretPosition++) {
				ReformedCommand += RecentCommand[CaretPosition];
			}

			// Formats the capitialization correctly so that trimming can read its
			string FinalizedCommand = "";
			for(int Increment = 0; Increment < ReformedCommand.Length; Increment++) {
				FinalizedCommand += CurrentLockOnAnswer[Increment];
			}

			// remove the existing part of the command from the lockon
			string StackString = CurrentLockOnAnswer.TrimPrefix(FinalizedCommand);

			/*
			char[] SliceChars = new char[]{CutoffChar};

			// Get both slices
			string[] SlicedCommandLockOn = CurrentLockOnAnswer.ToString().Split(SliceChars);
			string[] SlicedCommand = RecentCommand.ToString().Split(SliceChars);*/

			// merge That Shit			
			LSP_CONSOLE.Text = MergeCommands(CommandChunks) + ReformedCommand + StackString;
			if(IsAtEnOfStack(CurrentLockOnAnswer)) {
				LSP_CONSOLE.Text += "()";
			}
		}
	}
}