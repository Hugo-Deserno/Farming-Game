using Godot;
using System;
using System.Collections.Generic;

// send help
// what kind of crack did i smoke

public partial class Inventory : Control
{
	// Config
	public static float SCROLL_MODULA = 10; // how long it takes to scroll to the next row
	public static float MAX_SCROLL = 10;
	public static int INDEX_PER_COLLUM = 7;
	// General Data
	public static bool IS_ON_MAIN_UI_ELEMENT = false;
	public static int SORTING_TYPE = 1; // 1: default, 2: Inverse default, 3: Name
	// Objects
	public static Panel NODE_BACKGROUND;
	public static Panel ITEM_TEMPLATE;
	public static GridContainer ITEM_STORAGE;
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
					InventoryData.AddItemToDataBase(ItemName,Amount,null,false);
				}
			}

			InventoryHandler.DrawInventory();
		}

		// Gets nodes first otherwise shit cannot be found
		NODE_BACKGROUND = GetNode<Panel>("Background");
		ITEM_TEMPLATE = GetNode<Panel>("Contents/Template/Template");
		ITEM_STORAGE = GetNode<GridContainer>("Contents");

        // Load That shit up
		// Laods the i9nvenytory and history
        InvAnalytics.BufferInventoryData();
		//'fixes missing gaps in the history
		InventoryHandler.PatchInventoryData();
		// we load it all up
		DataBuffer(InvAnalytics.PLAYER_DATA);

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

	// Sorts the visual inventory
	public static void SortVisualInventory() {

	}

	// Updates A Certain Slot
	public static void UpdateInventorySlot(string _slotName) {
		if(!InvAnalytics.PLAYER_DATA.ContainsKey(_slotName)) {
			GD.PushError("Item named " + _slotName + " Does not exist");
		} else {
			// REmove slot if it doesnt exist
			if(InvAnalytics.PLAYER_DATA[_slotName] == 0) {
				Node SelectedItem = ITEM_STORAGE.FindChild(_slotName,true,false);

				if(SelectedItem != null) {
					SelectedItem.QueueFree();
				}
			} else {
				// check if it already exists
				if(ITEM_STORAGE.FindChild(_slotName,true,false) == null) {
					Node TemplateDuplicate =  ITEM_TEMPLATE.Duplicate();
					ITEM_STORAGE.AddChild(TemplateDuplicate);

					TemplateDuplicate.Name = _slotName;
					((Panel)TemplateDuplicate).Visible = true;
				}
			}
		}
	}
}

// Handles data garbage
// more more more
public partial class InventoryData : Inventory {
	// Adds items to inventory
	// handles minus aswell as posetive values
	public static void AddItemToDataBase(string _ItemName, int _DesiredValue, int? _History, bool _FuckWithHistory) { // pass in null in history if you want it to retain its value
		// Only write when needed
		if(InvAnalytics.PLAYER_DATA[_ItemName] != _DesiredValue || InvAnalytics.PLAYER_HISTORY_DATA[_ItemName] != _History) {
			_History = _History == null ? InvAnalytics.PLAYER_HISTORY_DATA[_ItemName] : _History;
			InvAnalytics.WriteBufferData(_ItemName,_DesiredValue,(int)_History);
		}
		
		// Decides what to do
		if(_DesiredValue > 0) {
			InventoryHandler.AddToInventory(_ItemName,_FuckWithHistory);
		} else {
			InventoryHandler.RemoveFromInventory(_ItemName);
		}
		UpdateInventorySlot(_ItemName);
	}

	// Prints the inventory for debugging case
	// mainly cuz godots response is weird and unclear
	public static void DebugInventory(Dictionary<int,Dictionary<int,string>> InventoryInstance) {
		for(int CollumIncrement = 0; CollumIncrement < InventoryInstance.Count; CollumIncrement++) {
			string CompiledString = "";

			for(int RowIncrement = 0; RowIncrement < InventoryInstance[CollumIncrement].Count; RowIncrement++) {
				CompiledString += "[{" + RowIncrement + "} : " + InventoryInstance[CollumIncrement][RowIncrement] + "]  ";
			}
			GD.Print(CompiledString);
		}

		/*
		foreach((int _, Dictionary<int,string> Collum) in InventoryInstance) {
			string CompiledString = "";

			foreach((int Index, string ItemName) in Collum) {
				CompiledString += "[{" + Index + "} : " + ItemName + "]  ";
			}
			GD.Print(CompiledString);
		}*/
	}

	// Debugs the history
	// so my dumb brain doesnt have to search through the json
	public static void DebugHistory() {
		for(int CollumIncrement = 0; CollumIncrement < INVENTORY.Count; CollumIncrement++) {
			string CompiledString = "";

			for(int RowIncrement = 0; RowIncrement < INVENTORY[CollumIncrement].Count; RowIncrement++) {
				CompiledString += "[{" + INVENTORY[CollumIncrement][RowIncrement] + "} : " + InvAnalytics.PLAYER_HISTORY_DATA[INVENTORY[CollumIncrement][RowIncrement]] + "]  ";
			}
			GD.Print(CompiledString);
		}

		/*
		foreach((int _, Dictionary<int,string> Collum) in INVENTORY) {
			string CompiledString = "";

			foreach((int _, string ItemName) in Collum) {
				CompiledString += "[{" + ItemName + "} : " + InvAnalytics.PLAYER_HISTORY_DATA[ItemName] + "]  ";
			}
			GD.Print(CompiledString);
		}*/
	}
}

public partial class InventoryHandler : Inventory {
	// Sorts all the garabage
	public static void DrawInventory() {
		// Dont fuck with history
		// method is meant to sort inventory based on history OR Alphabetical order
		Dictionary<int,Dictionary<int,string>> InventoryClone = new Dictionary<int, Dictionary<int, string>>();

		int CurrentIndex = SORTING_TYPE == 2 ? InvAnalytics.PLAYER_HISTORY_DATA.Count : 1;
		int CurrentRowIndex = 0;
		int CurrentCollumIndex = 0;

		// Holy shit this a mouth full

		// Basically get all the entries history
		for(int IndexIncrement = 0; IndexIncrement < InvAnalytics.PLAYER_HISTORY_DATA.Count; IndexIncrement++) {
			// Loop though all the entries in the inventory
			foreach((int CollumIndex, Dictionary<int,string> CollumEntry) in INVENTORY) {
				foreach((int RowIndex, string ItemName) in CollumEntry) {

					// do some weird shit if it matches
					if(InvAnalytics.PLAYER_HISTORY_DATA[ItemName] == CurrentIndex) {
						if(!InventoryClone.ContainsKey(CurrentCollumIndex)) {
							InventoryClone.Add(CurrentRowIndex, new Dictionary<int, string>());
						}

						InventoryClone[CurrentCollumIndex].Add(CurrentRowIndex,ItemName);
						CurrentRowIndex++;

						if(CurrentRowIndex > INDEX_PER_COLLUM) {
							CurrentRowIndex = 0;
							CurrentRowIndex++;
						}
						
						if(SORTING_TYPE == 2) {
							CurrentIndex--;
						} else {
							CurrentIndex++;
						}
					}
				}
			}
		}

		if(InventoryClone.Count == 0) {
			return;
		}
		INVENTORY = new Dictionary<int, Dictionary<int, string>>(InventoryClone);
		Inventory.SortVisualInventory();
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

			// determins if we have to do wierd shit with our content
			bool IndexFill = INVENTORY[(int)CollumEntry].Count == INDEX_PER_COLLUM ? true : false;
			int SortIndex = InvAnalytics.PLAYER_HISTORY_DATA[_ItemName];

			// Does the removing itself
			INVENTORY[(int)CollumEntry].Remove((int)RowEntry);

			// moves all the rows one lower
			// ^ be careful with this one, but the main idea is that
			// when a entry gets removed there will be a empty space at the end of the row
			// so we need to pick the next collum and move the first entry to that collum
			// so on and so on

			// OORRRRR

			// we construct a entire new inventory
			// with a weird janky ass algorithm
			// yeaaa we do that
			if(IndexFill) {
				Dictionary<int,Dictionary<int,string>> InventoryClone = new Dictionary<int, Dictionary<int, string>>();
				int CurrentCollum = 0;
				int CurrentRow = 0;

				// SO what happens here exactly?

				/*
					basically create a inv clone
					then loop through every entry in the original inventory
					then fill in the clone with every entry of the old one
				*/
				
				for(int IndexRowIncrement = 0; IndexRowIncrement < INVENTORY.Count; IndexRowIncrement++) {
					for(int IndexCollumIncrement = 0; IndexCollumIncrement < INDEX_PER_COLLUM; IndexCollumIncrement++) {
						if(!InventoryClone.ContainsKey(CurrentRow)) {
							InventoryClone.Add(CurrentRow, new Dictionary<int, string>());
						}

						if(INVENTORY[IndexRowIncrement].ContainsKey(IndexCollumIncrement)) {
							InventoryClone[CurrentRow].Add(CurrentCollum,INVENTORY[IndexRowIncrement][IndexCollumIncrement]);
							CurrentCollum++;

							if(CurrentCollum > INDEX_PER_COLLUM - 1) {
								CurrentRow++;
								CurrentCollum = 0;
							}
						}
					}
				}
				// overwrite that shit
				INVENTORY = InventoryClone;
				/*
				bool IndexSwitch = false; // determins if it has to make contact with last row

				// Start at the splitting point of the dict
				for(int IndexIncrement = (int)CollumEntry + 1; IndexIncrement < INVENTORY.Count; IndexIncrement++) {
					for(int CollumIndex = 0; CollumIndex < INDEX_PER_COLLUM; CollumIndex++) {
						if(CollumIndex == 0) {
							IndexSwitch = true;
						}

						if(IndexSwitch) { // moves it to the last layer
							INVENTORY[IndexIncrement].Add(INVENTORY.Count - 1,InventoryClone[IndexIncrement - 1][0]); // applies to the last row
							IndexSwitch = false;
						} else {
							INVENTORY[IndexIncrement].Add(CollumIndex,InventoryClone[IndexIncrement][CollumIndex + 1]);
						}
					}
				}*/
			}

			//GODAMMIT, FIX THE FUCKING SORTING ORDER
			// thank you -future quas
			foreach((string ItemName, int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
				if(HistoryIndex > SortIndex) {
					InvAnalytics.PLAYER_HISTORY_DATA[ItemName]--;
				}
			}
			DrawInventory();
		}
	}

	// Adds a selecetyed item to inventory becaus it likes
	public static void AddToInventory(string _ItemName, bool _FuckWithHistory) { // for clarification fuckwithHistory means if it should change the item age, aka plz i dont my data to be overwriten
		(bool IsValid, int? _, int? _) = IsInInvetory(_ItemName);

		// Check if item already exists in the inventory
		if(!IsValid) {
			// Adds to the inventory
			void Add(int __SlotNumber, int __CollumIndex) {
				INVENTORY[__CollumIndex][__SlotNumber] = _ItemName;
				if(_FuckWithHistory) {
					InvAnalytics.WriteBufferData(_ItemName,InvAnalytics.PLAYER_DATA[_ItemName],1); // wirte to set history
				}
			}
			
			// Pushes The History of other items
			void Push() {
				if(!_FuckWithHistory) {
					return;
				}
				
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
				Add(0,INVENTORY.Count - 1); // we do -1 cuz the line above we add one
			}

			Push();
			DrawInventory();
		} else {
			// Move said item to the first entry of this shit
			// this means that there will need to be a push till the gap is reached
			int Breakpoint = InvAnalytics.PLAYER_HISTORY_DATA[_ItemName];

			foreach((string ItemName, int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
				if(HistoryIndex < Breakpoint) {
					InvAnalytics.PLAYER_HISTORY_DATA[ItemName]++;
				}
			}

			InvAnalytics.PLAYER_HISTORY_DATA[_ItemName] = 1;
			DrawInventory();

			return;
		}
	}
	
	// FIxxes gapos in the inventory
	public static void PatchInventoryData() {
		int CurrentHistoryIndex = 1;
		bool FireSaveEvent = false;
		
		for(int HistoryIncerement = 0; HistoryIncerement < InvAnalytics.PLAYER_HISTORY_DATA.Count - 1; HistoryIncerement++) {
			bool PatchCorrecting = true;

			// Check if the history index exists
			foreach ((string ItemName,int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
				if(HistoryIndex == CurrentHistoryIndex) {
					PatchCorrecting = false;
					FireSaveEvent = true;

					CurrentHistoryIndex++;
					break;
				}
			}

			// if said item is missing, we do some funky shit
			// bascially just throwing it out and shoving everything down
			if(PatchCorrecting) {
				GD.PushWarning("Fixing History Corruption");

				foreach ((string ItemName,int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
					if(HistoryIndex > CurrentHistoryIndex) {
						InvAnalytics.PLAYER_HISTORY_DATA[ItemName]--;
					}
				}
			}
		}

		// Saves all of this shit
		if(FireSaveEvent) {
			DataRepository.SaveDataToJson();
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
			} {

			}
		}
		return (false,null,null);
	}
}