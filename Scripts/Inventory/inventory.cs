using Godot;
using System;

public partial class inventory : Control
{
	// General Data
	public static bool IS_ON_MAIN_UI_ELEMENT = false;
	// Objects
	public static Panel NODE_BACKGROUND;
	public static MouseRect MOUSE_RECT;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Load That shit up
		InvAnalytics.BufferInventoryData();

		NODE_BACKGROUND = GetNode<Panel>("Background");

		// Set up mous rect
		MOUSE_RECT = new MouseRect(NODE_BACKGROUND.Position,NODE_BACKGROUND.Size,new Vector4(
			NODE_BACKGROUND.AnchorTop,
			NODE_BACKGROUND.AnchorBottom,
			NODE_BACKGROUND.AnchorLeft,
			NODE_BACKGROUND.AnchorRight
		));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double _Delta) {
		GD.Print(IS_ON_MAIN_UI_ELEMENT);

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
public partial class InventoryData : inventory {
	// Adds items to inventory
	// handles minus aswell as posetive values
	public static void AddItemToDataBase(string _ItemName, int _DesiredValue) {
		InvAnalytics.WriteBufferData(_ItemName,_DesiredValue);
		UpdateInventorySlot(_ItemName);
	}
}