using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

public partial class Inventory : Control
{
	// Config
	public static float SCROLL_MODULA = 10; // how long it takes to scroll to the next row
	public static float MAX_SCROLL = 10;
	public static int INDEX_PER_COLLUM = 7;
	// General Data
	public static bool IS_ON_MAIN_UI_ELEMENT = false;
	// Objects
	public static Panel NODE_BACKGROUND;
	public static MouseRect MOUSE_RECT;

	/*
		So quick thought here, cuz i have short term memory loss

		Inventory will be stored in a dict
		in every entry of the dict there will we another dict which consists
		of rows which will be the items in the inv

		to display it get mousemodula and get the 4 rows beneath them
		draw those
		aaannnnnnddd

		done :D
	*/
	public static Dictionary<int,Dictionary<int,string>> INVENTORY = new Dictionary<int, Dictionary<int, string>> {};

	// Privates
	private float ModulaDelta = 0;
	private float Mousedelta = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Loads the data to the inventory
		// Sorting can be done afterwards
		void DataBuffer(Dictionary<string,int> _DataPacket) {
			foreach((string ItemName, int Amount) in _DataPacket) {
				InvAnalytics.WriteBufferData(ItemName,Amount,InvAnalytics.PLAYER_HISTORY_DATA[ItemName]); // write to buffer so it fills missing data gaps
				if(Amount > 0) {
					InventoryData.AddItemToDataBase(ItemName,Amount,null);
				}
			}
			InventoryHandler.DrawInventory();
		}

        // Load That shit up
        InvAnalytics.BufferInventoryData();
		DataBuffer(InvAnalytics.PLAYER_DATA);
		NODE_BACKGROUND = GetNode<Panel>("Background");

		// Set up mous rect
		MOUSE_RECT = new MouseRect(NODE_BACKGROUND);
	}

	// handles the mouse movement
    public override void _Input(InputEvent _Event) {
		if(_Event is InputEventMouseMotion MouseEvent) {
			MOUSE_RECT.Step(MouseEvent.Position);
		}
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double _Delta) {
		IS_ON_MAIN_UI_ELEMENT = MOUSE_RECT.Get();

		// Mouse wheel input Junk
		if(Input.IsActionJustReleased("Movement_Zoom_Down") && IS_ON_MAIN_UI_ELEMENT) {
			Mousedelta = Math.Clamp(Mousedelta + 1,0,MAX_SCROLL * SCROLL_MODULA);
        } else if(Input.IsActionJustReleased("Movement_Zoom_Up") && IS_ON_MAIN_UI_ELEMENT) {
			Mousedelta = Math.Clamp(Mousedelta - 1,0,MAX_SCROLL * SCROLL_MODULA);
        }

		// Modula stuff
		// Rounds it off to the SCroll modula
		ModulaDelta = Mousedelta % SCROLL_MODULA == 0 ? 
		Mousedelta / SCROLL_MODULA :
		ModulaDelta;
		
		ModulaDelta = Math.Clamp(ModulaDelta,0,MAX_SCROLL);
	}

	// Updates A Certain Slot
	public static void UpdateInventorySlot(string _slotName) {
		if(!InvAnalytics.PLAYER_DATA.ContainsKey(_slotName)) {
			GD.PushError("Item named " + _slotName + " Does not exist");
		} else {
			// REmove slot if it doesnt exist
			if(InvAnalytics.PLAYER_DATA[_slotName] == 0) {

			} else {

			}
		}
	}
}

// Handles data garbage
// more more more
public partial class InventoryData : Inventory {
	// Adds items to inventory
	// handles minus aswell as posetive values
	public static void AddItemToDataBase(string _ItemName, int _DesiredValue, int? _History) { // pass in null in history if you want it to retain its value
		// Only write when needed
		if(InvAnalytics.PLAYER_DATA[_ItemName] != _DesiredValue || InvAnalytics.PLAYER_HISTORY_DATA[_ItemName] != _History) {
			_History = _History == null ? InvAnalytics.PLAYER_HISTORY_DATA[_ItemName] : _History;
			InvAnalytics.WriteBufferData(_ItemName,_DesiredValue,(int)_History);
		}
		
		// Decides what to do
		if(_DesiredValue > 0) {
			InventoryHandler.AddToInventory(_ItemName);
		} else {
			InventoryHandler.RemoveFromInventory(_ItemName);
		}
		UpdateInventorySlot(_ItemName);
	}

	// Prints the inventory for debugging case
	// mainly cuz godots response is weird and unclear
	public static void DebugInventory() {
		foreach((int _, Dictionary<int,string> Collum) in INVENTORY) {
			string CompiledString = "";

			foreach((int Index, string ItemName) in Collum) {
				CompiledString += "[{" + Index + "} : " + ItemName + "]  ";
			}
			GD.Print(CompiledString);
		}
	}
}

public partial class InventoryHandler : Inventory {
	// Sorts all the garabage
	public static void DrawInventory() {

	}

	// Removes shit that aint wnated
	public static void RemoveFromInventory(string _ItemName) {
		(bool IsValid, int? _, int? _) = IsInInvetory(_ItemName);

		// Check if item already exists in the inventory
		if(IsValid) {
			int? RowEntry = null;
			int? CollumEntry = null;

			// Finds the item in the inv
			foreach((int CollumIndex, Dictionary<int,string> Collum) in INVENTORY) {
				foreach((int RowIndex, string ItemName) in Collum) {
					if(ItemName == _ItemName) {
						RowEntry = RowIndex;
						CollumEntry = CollumIndex;
						break;
					}
				}
			}
			// DENIED BITCH
			if(RowEntry == null || CollumEntry == null) {
				GD.PushWarning("Error: item named " + _ItemName + "Does not exist in inventory");
				return;
			}

			// Does the removing itself
			INVENTORY[(int)CollumEntry].Remove((int)RowEntry);

			// moves all the rows one lower
			// ^ be careful with this one, but the main idea is that
			// when a entry gets removed there will be a empty space at the end of the row
			// so we need to pick the next collum and move the first entry to that collum
			// so on and so on
			
			// move the history one down
		}
	}

	// Adds a selecetyed item to inventory becaus it likes
	public static void AddToInventory(string _ItemName) {
		(bool IsValid, int? _, int? _) = IsInInvetory(_ItemName);

		// Check if item already exists in the inventory
		if(!IsValid) {
			// Adds to the inventory
			void Add(int __SlotNumber, int __CollumIndex) {
				INVENTORY[__CollumIndex][__SlotNumber] = _ItemName;
				InvAnalytics.WriteBufferData(_ItemName,InvAnalytics.PLAYER_DATA[_ItemName],1); // wirte to set history
				DrawInventory();
			}
			
			// Pushes The History of other items
			void Push() {
				foreach((int Index, Dictionary<int,string> Collum) in INVENTORY) {
					foreach((int CollumIndex, string ItemName) in Collum) {
						if(ItemName != _ItemName) {
							InvAnalytics.WriteBufferData(_ItemName,InvAnalytics.PLAYER_DATA[_ItemName],InvAnalytics.PLAYER_HISTORY_DATA[_ItemName] + 1); // write to set history
						}
					}
				}
			}

			int CollumIndex = INVENTORY.Count == 0 ? 
			0 : 
			INVENTORY.Count - 1;

			// Create a row if it doesnt exist
			if(!INVENTORY.ContainsKey(CollumIndex)) {
				INVENTORY.Add(CollumIndex,new Dictionary<int, string>{});
			}
			// Row is avalaibla
			if(!INVENTORY[CollumIndex].ContainsKey(INDEX_PER_COLLUM - 1)) {
				for(int IncrementalIndex = 0; IncrementalIndex < INDEX_PER_COLLUM; IncrementalIndex++) {
					if(!INVENTORY[CollumIndex].ContainsKey(IncrementalIndex)) {
						Add(IncrementalIndex,CollumIndex); // create entry
						break;
					}
				}
			} else { //  Create a new row
				INVENTORY.Add(INVENTORY.Count,new Dictionary<int, string>{});
				Add(0,INVENTORY.Count);
			}

			Push();
			DrawInventory();
		} else {
			return;
		}
	}

	// Checks if entry exists in inventory
	public static (bool, int?, int?) IsInInvetory(string _ItemName) {
		// Loop through the collums to check
		foreach((int Index, Dictionary<int,string> Collum) in INVENTORY) {
			foreach((int CollumIndex, string ItemName) in Collum) {
				if(ItemName == _ItemName) {
					return (true,Index,CollumIndex);
				}
			}
		}
		return (false,null,null);
	}
}