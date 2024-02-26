using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

// send help
// what kind of crack did i smoke

// Update, yep i definitely smoked something

// anyways, LITTLE NOTE

// if History DOES fuck up again
// DO NOT, and i repeat , DO NOT glance over it
// IDK what causes it, and i'm frankly to fucking lazy to search through this pile of spaghetti code for said problem

public partial class Inventory : Control
{
	// Config / readonlies
	public readonly static float SCROLL_MODULA = 10; // how long it takes to scroll to the next row
	public readonly static int INDEX_PER_COLLUM = 7;
	public readonly static int SLOT_AMOUNT = INDEX_PER_COLLUM * 4 + 1;
	// General Data
	public static bool IS_ON_MAIN_UI_ELEMENT = false;
	public static string SEARCH_QUERY = "";
	public static int SORTING_TYPE = 3; // 1: default, 2: Inverse default, 3: Name
	// Objects
	public static Panel NODE_BACKGROUND;
	public static Panel ITEM_TEMPLATE;
	public static GridContainer ITEM_STORAGE;
	public static TextEdit SEARCH_BAR;

	public static MouseRect SEARCH_MOUSE_RECT;
	public static MouseRect MOUSE_RECT;

	/*
		So quick thought here, cuz i have short term memory loss

		Inventory will be stored in a dict
		in every entry of the dict there will we another dict which consists
		of rows which will e the items in the inv

		to display it get mousemodula and get the 4 rows beneath them
		draw those
		aaannnnnnddd

		done :D
	*/
	public static Dictionary<int,Dictionary<int,string>> INVENTORY = new Dictionary<int, Dictionary<int, string>> {};
	public static int[] DISPLAYED_ROWS = new int[4]{0,1,2,3}; // rows that will be displayed

	// Privates
	private float ModulaDelta = 0;
	private float Mousedelta = 0;

	private delegate void ProcessComputeLoop();
	ProcessComputeLoop Loop;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Gets nodes first otherwise shit cannot be found
		NODE_BACKGROUND = GetNode<Panel>("Background");
		ITEM_TEMPLATE = GetNode<Panel>("Contents/Template/Template");
		ITEM_STORAGE = GetNode<GridContainer>("Contents");
		SEARCH_BAR = GetNode<TextEdit>("SearchBar/TextBar");
		// Set up mous rect
		MOUSE_RECT = new MouseRect(NODE_BACKGROUND);
		SEARCH_MOUSE_RECT = new MouseRect(SEARCH_BAR);

		// First off load the slots because we need them to be ready
		SetupInventorySlots();
		// Loads the data to the inventory
		// Sorting can be done afterwards
		void DataBuffer(Dictionary<string,int> _DataPacket) {
			foreach((string ItemName, int Amount) in _DataPacket) {
				InvAnalytics.WriteBufferData(ItemName,Amount,InvAnalytics.PLAYER_HISTORY_DATA[ItemName]); // write to buffer so it fills missing data gaps
				if(Amount > 0) {
					InventoryData.AddItemToDataBase(ItemName,Amount,null,false,true);
				}
			}

			InventoryHandler.DrawInventory();
		}
		// Cleans the searchbar
		void CleanSearchBar() {
			if(SEARCH_BAR.Text == "") {
				SEARCH_BAR.Text = "Search here...";
			}
		}
		void InitiateSearch() {
			SEARCH_BAR.Text = "";
			SEARCH_QUERY = "";
		}

        // Load That shit up
		// Laods the i9nvenytory and history
        InvAnalytics.BufferInventoryData();
		//'fixes missing gaps in the history
		InventoryHandler.PatchInventoryData();
		// we load it all up
		DataBuffer(InvAnalytics.PLAYER_DATA);

		// Delegate Shit
		Loop += HandleScrollModula;
		Loop += RenderInventory;

		SEARCH_BAR.FocusExited += () => CleanSearchBar();
		SEARCH_BAR.FocusEntered += () => InitiateSearch();
	}

	// handles the mouse movement
    public override void _Input(InputEvent _Event) {
		// handles disconnecting when clicking outside of the box
		if(_Event is InputEventMouseButton MouseButton) {
			if(MouseButton.ButtonIndex == Godot.MouseButton.Left) {
				if(SEARCH_MOUSE_RECT.Get() == false && SEARCH_BAR.HasFocus()) {
					SEARCH_BAR.ReleaseFocus();
				}
			}
		}
		if(_Event is InputEventMouseMotion MouseEvent) {
			MOUSE_RECT.Step(MouseEvent.Position);
			SEARCH_MOUSE_RECT.Step(MouseEvent.Position);
		}
		if(_Event is InputEventKey CurrentKey) {
			if(CurrentKey.PhysicalKeycode == Key.Enter && SEARCH_BAR.HasFocus()) {
				SEARCH_BAR.ReleaseFocus();
			}
			if(SEARCH_BAR.HasFocus()) {
				SEARCH_QUERY = SEARCH_BAR.Text;
			}
		}
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double _Delta) {
		IS_ON_MAIN_UI_ELEMENT = MOUSE_RECT.Get();
		
		Loop();
	}

	private void HandleScrollModula() {
		// Mouse wheel input Junk
		if(Input.IsActionJustReleased("Movement_Zoom_Down") && IS_ON_MAIN_UI_ELEMENT) {
			Mousedelta = Math.Clamp(Mousedelta + 1,0,(INVENTORY.Count - 4) * SCROLL_MODULA);
        } else if(Input.IsActionJustReleased("Movement_Zoom_Up") && IS_ON_MAIN_UI_ELEMENT) {
			Mousedelta = Math.Clamp(Mousedelta - 1,0,(INVENTORY.Count - 4) * SCROLL_MODULA);
        }

		// Modula stuff
		// Rounds it off to the SCroll modula
		ModulaDelta = Mousedelta % SCROLL_MODULA == 0 ? 
		Mousedelta / SCROLL_MODULA :
		ModulaDelta;
		
		ModulaDelta = !INVENTORY.ContainsKey(3) ? 0 : Math.Clamp(ModulaDelta,0,INVENTORY.Count - 4); // - 4 cuz there are 4 visible rows in the inventory at all times

		// Apply it
		for(int Increment = 0; Increment < DISPLAYED_ROWS.Length; Increment++) {
			DISPLAYED_ROWS[Increment] = Increment + (int)ModulaDelta;
		}
	}

	// Sets the inventory up
	private void SetupInventorySlots() {
		for(int SlotIncrement = 1; SlotIncrement < SLOT_AMOUNT; SlotIncrement++) {
			Node TemplateDuplicate =  ITEM_TEMPLATE.Duplicate();
			ITEM_STORAGE.AddChild(TemplateDuplicate);
			
			TemplateDuplicate.Name = (SlotIncrement - 1).ToString();
			((Panel)TemplateDuplicate).Visible = false;
		}
	}

	// Sorts the visual inventory

	// update, HOLY SHIT, It actually works
	public static void RenderInventory() {
		// we first of store all the shit
		// cuz IDK if theres a layout order

		// Empties a certain slots data
		void EmptySelectedSlot(Panel SelectedSlot) {
			((RichTextLabel)SelectedSlot.FindChild("Amount",true,false)).Text = "100";
			SelectedSlot.Visible = false;
			return;
		}

		/* Okay so little me idea
			we first fill the entire inventory with templates
			then assign to those templates based on which position they are in the hieracy
			so no generating slots when items get added
			all the slots are there, but they are invisible
			and once a item is added it becomes visible
		*/

		// Empty all the slots
		foreach(Node TemplateInstance in ITEM_STORAGE.GetChildren()) {
			if(TemplateInstance.GetClass() == "Panel") {
				EmptySelectedSlot((Panel)TemplateInstance);
			}
		}

		int CurrentSlotSelector = 0; // Based on name
		// Loop through all the entries in the Displayed rows
		for(int DisplayedIncrement = 0; DisplayedIncrement < DISPLAYED_ROWS.Length; DisplayedIncrement++) {
			// Loop though all the slots on that row
			for(int RowIncrement = 0; RowIncrement < INDEX_PER_COLLUM; RowIncrement++) {
				if(!INVENTORY.ContainsKey(DISPLAYED_ROWS[DisplayedIncrement])) {
					return;
				}


				// Check if key exist
				if(INVENTORY[DISPLAYED_ROWS[DisplayedIncrement]].ContainsKey(RowIncrement)) {
					if(SEARCH_QUERY == "" || INVENTORY[DISPLAYED_ROWS[DisplayedIncrement]][RowIncrement].ToLower().Contains(SEARCH_QUERY.ToLower())) {
						Node SelectedSlot = ITEM_STORAGE.FindChild(CurrentSlotSelector.ToString(),true,false);

						if(SelectedSlot == null) {
							GD.PushWarning("Inventory Warning: Slot number " + CurrentSlotSelector + " Doesn't exist");
						} else {
							((Panel)SelectedSlot).Visible = true;
							((RichTextLabel)SelectedSlot.FindChild("Amount",true,false)).Text = INVENTORY[DISPLAYED_ROWS[DisplayedIncrement]][RowIncrement].ToString(); // DEBUG;
						}
						CurrentSlotSelector++;
					}
				}
			}
		}

		// TOTALLY FUCKING USLESS
		/*
		foreach(Node InvetorySlot in ITEM_STORAGE.GetChildren()) {
			string ObjectType = InvetorySlot.GetClass();

			if(ObjectType == "Panel") {
				ITEM_STORAGE.RemoveChild(InvetorySlot);
				ITEM_CACHE.AddChild(InvetorySlot);
			}
		}

		for(int CollumIncrement = 0; CollumIncrement < INVENTORY.Count; CollumIncrement++) {
			int TargetCollum = INVENTORY.Count - (CollumIncrement + 1);

			for(int RowIncrement = 0; RowIncrement < INVENTORY[TargetCollum].Count; RowIncrement++) {
				int TargetRow = INVENTORY[TargetCollum].Count - (RowIncrement + 1);

				string ItemName = INVENTORY[CollumIncrement][TargetRow];

				Node CurrentItem = ITEM_CACHE.FindChild(ItemName,true,false);

				if(CurrentItem != null) {
					ITEM_CACHE.RemoveChild(CurrentItem);
					ITEM_STORAGE.AddChild(CurrentItem);
				} else {
					GD.PushWarning("Cannot find itemslot named " + ItemName);
				}
			}
		}*/
	}

	// Updates A Certain Slot
	/*
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
					((RichTextLabel)TemplateDuplicate.FindChild("Amount",true,false)).Text = InvAnalytics.EncryptDataEntry(_slotName).ToString(); // DEBUG
					((Panel)TemplateDuplicate).Visible = true;
				}
			}
		}
	}*/
}

// Handles data garbage
// more more more
public partial class InventoryData : Inventory {
	// Adds items to inventory
	// handles minus aswell as posetive values
	public static void AddItemToDataBase(string _ItemName, int _DesiredValue, int? _History, bool _FuckWithHistory,bool _Static) { // pass in null in history if you want it to retain its value
		// _ItemName : name of the item
		// _Desiredvalue : how much of said item needs to be added
		// _History : if theres a desired history value
		// _FuckWithHistory : should it change stuff with history
		// _Static : disables drawing the inventory

		// static is mainly called when loading up
		// because we load the items up non-chronological
		// so the history isnt loaded chronologically
		// this results in data breaking
		// becuase its missing gaps

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
		// Check for static, just in case we dont wanna do weird stuff with history
		if(!_Static) {
			InventoryHandler.DrawInventory();
		}
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
		int HighestIndex = 1;
		int CurrentRowIndex = 0;
		int CurrentCollumIndex = 0;

		// determin the highest history index possible
		foreach((string ItemName, int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
			if(HistoryIndex > HighestIndex) {
				HighestIndex = HistoryIndex;
			}
			if(HistoryIndex == InvAnalytics.PLAYER_HISTORY_DATA.Count) {
				break;
			}
		}

		int CurrentIndex = SORTING_TYPE == 2 ? HighestIndex : 1;
		// Holy shit this a mouth full
		if(SORTING_TYPE == 1 || SORTING_TYPE == 2) {
			// Basically get all the entries history
			for(int IndexIncrement = 0; IndexIncrement < HighestIndex; IndexIncrement++) {
				bool Matched = false;

				// Loop though all the entries in the inventory
				foreach((int CollumIndex, Dictionary<int,string> CollumEntry) in INVENTORY) {
					foreach((int RowIndex, string ItemName) in CollumEntry) {						
						// do some weird shit if it matches
						if(InvAnalytics.PLAYER_HISTORY_DATA[ItemName] == CurrentIndex) {
							if(!InventoryClone.ContainsKey(CurrentCollumIndex)) {
								InventoryClone.Add(CurrentCollumIndex, new Dictionary<int, string>());
							}

							InventoryClone[CurrentCollumIndex].Add(CurrentRowIndex,ItemName);
							CurrentRowIndex++;
							Matched = true;

							if(CurrentRowIndex > INDEX_PER_COLLUM - 1) {
								CurrentRowIndex = 0;
								CurrentCollumIndex++;
							}
							
							if(SORTING_TYPE == 2) {
								CurrentIndex--;
							} else {
								CurrentIndex++;
							}
							break;
						}
					}
					if(Matched) {
						break;
					}
				}
			}
		} else if(SORTING_TYPE == 3) { // A-Z sorting
			// Removes all the nesting
			Dictionary<int,string> UnnestedDict = new Dictionary<int, string>();
			int DictCount = 0;
			for(int CollumIncrement = 0; CollumIncrement < INVENTORY.Count; CollumIncrement++) {
				for(int RowIncrement = 0; RowIncrement < INVENTORY[CollumIncrement].Count; RowIncrement++) {
					UnnestedDict.Add(DictCount,INVENTORY[CollumIncrement][RowIncrement]);
					DictCount++;
				}
			}

			// Determin length of inventory
			int InventoryLength = UnnestedDict.Count;

			// use a bubble sort algorithm to sort it alphabetically
			// im not gonna act like i know whats going on, but it works 👍👍👍
			for(int Increment = 0; Increment < InventoryLength; Increment++) {
				for(int SecondIncrement = 0; SecondIncrement < InventoryLength - 1; SecondIncrement++) {
					if(UnnestedDict[SecondIncrement].CompareTo(UnnestedDict[SecondIncrement + 1]) > 0) {
						string TempCache = UnnestedDict[SecondIncrement];

						UnnestedDict[SecondIncrement] = UnnestedDict[SecondIncrement + 1];
						UnnestedDict[SecondIncrement + 1] = TempCache;
					}
				}
			}

			// simple assigning stuff for the inventory format
			for(int TempInvIncrement = 0; TempInvIncrement < UnnestedDict.Count; TempInvIncrement++) {
				if(!InventoryClone.ContainsKey(CurrentCollumIndex)) {
					InventoryClone.Add(CurrentCollumIndex, new Dictionary<int, string>());
				}

				InventoryClone[CurrentCollumIndex].Add(CurrentRowIndex,UnnestedDict[TempInvIncrement]);
				CurrentRowIndex++;

				if(CurrentRowIndex > INDEX_PER_COLLUM - 1) {
					CurrentRowIndex = 0;
					CurrentCollumIndex++;
				}
				
				CurrentIndex++;
			}
		}
	
		if(InventoryClone.Count == 0) {
			return;
		}
		INVENTORY = new Dictionary<int, Dictionary<int, string>>(InventoryClone);
		Inventory.RenderInventory();
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
							InvAnalytics.WriteBufferData(ItemName,InvAnalytics.PLAYER_DATA[ItemName],InvAnalytics.PLAYER_HISTORY_DATA[ItemName] + 1); // write to set history
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
		} else {
			// Move said item to the first entry of this shit
			// this means that there will need to be a push till the gap is reached
			int Breakpoint = InvAnalytics.PLAYER_HISTORY_DATA[_ItemName];

			foreach((string ItemName, int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
				if(HistoryIndex < Breakpoint) {
					InvAnalytics.WriteBufferData(_ItemName,InvAnalytics.PLAYER_DATA[_ItemName],InvAnalytics.PLAYER_HISTORY_DATA[ItemName]++); // wirte to set history
				}
			}

			InvAnalytics.WriteBufferData(_ItemName,InvAnalytics.PLAYER_DATA[_ItemName],1); // wirte to set history

			return;
		}
	}
	
	// FIxxes gapos in the inventory
	public static void PatchInventoryData() {
		int CurrentHistoryIndex = 1;
		int HighestIndex = 1;
		bool FireSaveEvent = false;

		foreach((string ItemName, int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
			if(HistoryIndex > HighestIndex) {
				HighestIndex = HistoryIndex;
			}
		}
		
		for(int HistoryIncerement = 0; HistoryIncerement < HighestIndex; HistoryIncerement++) {
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
			// bascially just shoving everything down
			if(PatchCorrecting) {
				GD.PushWarning("Fixing History Corruption " + CurrentHistoryIndex);

				foreach ((string ItemName,int HistoryIndex) in InvAnalytics.PLAYER_HISTORY_DATA) {
					if(HistoryIndex > CurrentHistoryIndex) {
						InvAnalytics.WriteBufferData(ItemName,InvAnalytics.PLAYER_DATA[ItemName],InvAnalytics.PLAYER_HISTORY_DATA[ItemName] - 1); // wirte to set history
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
