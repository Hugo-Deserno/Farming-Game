using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

// Does Main high Level Data Requests
public class DataRepository : DataHandler {
	/*
		 so for storing object specific meta tags

		 just add the tag under here along with th shortcut it needs to represent
		 and all your stuff is sorted
	*/

	// easy encrypytion method for storing tags with shortcuts
	public static Dictionary<string,string> ENCRYPTION_LIBRARY = new Dictionary<string, string>{
		["Position"] = "P",
		["Size"] = "S",
		["Rotation"] = "R",
		["Name"] = "N",
	};

	// Inv Encryptions junk
    public static Dictionary<int,string> INVENTORY_ENCRYPTION_KEY = new Dictionary<int, string>{
        [1] = "Wheat", [2] = "Stone", [3] = "Wood",
[4] = "Epic1",
[5] = "Epic2",
[6] = "Epic3",
[7] = "Epic4",
[8] = "Epic5",
[9] = "Epic6",
[10] = "Epic7",
[11] = "Epic8",
[12] = "Epic9",
[13] = "Epic10",
[14] = "Epic11",
[15] = "Epic12",
[16] = "Epic13",
[17] = "Epic14",
[18] = "Epic15",
[19] = "Epic16",
[20] = "Epic17",
[21] = "Epic18",
[22] = "Epic19",
[23] = "Epic20",
[24] = "Epic21",
[25] = "Epic22",
[26] = "Epic23",
[27] = "Epic24",
[28] = "Epic25",
[29] = "Epic26",
[30] = "Epic27",
[31] = "Epic28",
[32] = "Epic29",
[33] = "Epic30",
    };


    // unused
    public static void Settings() {
        DataController.GenerateJsonFile(2); // Setting file, ID 2 means itll generate a settings file
    }

    // basic Controls and components

    // Initializes A Save Key
    public static void IntializeRepo(int _SaveKey) {
        // Set Keys
        DataController.SetKey(_SaveKey);
        DataController.SetDefaultKey(SetupDefaultData());

		// Loads Files
		DataController.GenerateJsonFile(1); // Data File
		DownloadData(); //  Download That shit so that its publicly available
        AutoSaveDataToJsonFile(); // Initialize the Auto Saving
    }

    // gathers the defaults json info and mixes it with the data struct
	private static Godot.Collections.Dictionary<string,Variant> SetupDefaultData() {
		// Loads The Default Json file
		string JsonText = File.ReadAllText(Path.Join(ProjectSettings.GlobalizePath("res://"),"Scripts/Data/DefaultData.json"));

		Json JsonInstance =  new Json();
		Error ParsedResult = JsonInstance.Parse(JsonText);

		if(ParsedResult == Error.Ok) {
			// pactches it
			// For my dumb brain
			// Implements all the things noted in  the default to a fresh datastruct
			// So it downloads all the info from the default onto a clean template
			// So if a value doesnt need touching, no need to throw it into the default file
			Godot.Collections.Dictionary<string,Variant> DefaultDataPatch = StaticDataHandler.DataPatcher(
				(Godot.Collections.Dictionary<string,Variant>)JsonInstance.Data,
				new DataStruct().Data
			);

			return DefaultDataPatch;
		} else {
            GD.PushWarning("ERROR Reading Default Json");
			return new DataStruct().Data;
		}
	}

    // Handles the autosaving
	private async static void AutoSaveDataToJsonFile() {
		while (true) {
			await Task.Delay(70000);
			SaveDataToJson();
		}
	}

    // Compiling and Saving

    // Saves All the Players Data To A Json File
	// _ClassCaller is a special fucntion froma namespace which track which functions calls this method
	public static void SaveDataToJson([CallerMemberName] string _ClassCaller = "") {
		GD.Print("Data save request made from [" + _ClassCaller + "]");
        // placement ANd Voxels
		DataHandler.Set(new System.Collections.Generic.Dictionary<int, string>{[0] = "P",},"V",CompileVoxelGrid(),DataHandler.DATA_FILE);
		DataHandler.Set(new System.Collections.Generic.Dictionary<int, string>{[0] = "P",},"P",CompilePlacedItems(),DataHandler.DATA_FILE);

		DataHandler.UploadData();
	}

	// voxel compiler
	private static Godot.Collections.Dictionary<int,Variant> CompileVoxelGrid() {
		Godot.Collections.Dictionary<int,Variant> CompiledVoxelGrid = new Godot.Collections.Dictionary<int, Variant>();
		int VoxelCountTracker = 0; //  for assigning dictonary element names

		// loop through all the voxels
		foreach((string _, Node3D Voxel) in VoxelGridHandler.VOXEL_GRID_LIBRARY) {
			Vector3 VoxelPosition = (Vector3)Voxel.GetMeta("VoxelPosition",Vector3.Zero);
			int VoxelIDType = (int)Voxel.GetMeta("VoxelIDType",0);
			int VoxelID = (int)Voxel.GetMeta("VoxelID",0); 
			bool Purchased = (bool)Voxel.GetMeta("VoxelIsPurchased",false);

			// Dont Save Untouched Voxels
			if(Purchased) {
				CompiledVoxelGrid.Add(VoxelCountTracker,new Godot.Collections.Dictionary<string, Variant>());
				Godot.Collections.Dictionary<string, Variant> SelectedVoxelCompile = (Godot.Collections.Dictionary<string, Variant>)CompiledVoxelGrid[VoxelCountTracker];
				VoxelCountTracker++;

				SelectedVoxelCompile.Add("P",new Godot.Collections.Array<float>{
					VoxelPosition.X,
					VoxelPosition.Y,
					VoxelPosition.Z
				});
				SelectedVoxelCompile.Add("B",Purchased);
				SelectedVoxelCompile.Add("I",VoxelIDType);
			}
		}

		return CompiledVoxelGrid;
	}

	/*
		so uhhhhh, quick me thing.

		Download the metadata of said object.
		oh, yea should also create a ignore list to not take up uneeded memory.
		And yes, the size component is needed, 
		becaussssssssss there is no way to get object size without instantiating a scene
	*/

	// stores all the object metadata so we can have custom feild serialized
	public static Godot.Collections.Dictionary<int,Variant> CompilePlacedItems() {
		Godot.Collections.Dictionary<int,Variant> PlacementSerializer = new Godot.Collections.Dictionary<int, Variant>();

		// Sort trough all the garbage
		int CurrentItemTally = 0;
		foreach(Node3D PlacedItem in GridHandler.NODE_PLACED_STORAGE.GetChildren()) {
			PlacementSerializer.Add(CurrentItemTally,new Godot.Collections.Dictionary<string, Variant>());
			Godot.Collections.Dictionary<string, Variant> SelectedItemCompile = (Godot.Collections.Dictionary<string, Variant>)PlacementSerializer[CurrentItemTally];
			CurrentItemTally++;

			// Get All the metadata
			Node MetaItem = GridSystem3D.GetItemMetaData(PlacedItem);
			Godot.Collections.Array<Godot.StringName> MetaDataList = MetaItem.GetMetaList();

			// Gets The Item Name
			// And engrains it
			char[] DistectionChars = {'_'};
			string[] DisectedStrings = PlacedItem.Name.ToString().Split(DistectionChars);
			SelectedItemCompile.Add(ENCRYPTION_LIBRARY["Name"],DisectedStrings[0]);

			// Loop through meta list
			List<string> DeniedMetaTags = new List<string>{"Id","Cost","Type"}; // deny this shit cuz it takes up uneeded memory
			foreach(string MetaDataEntry in MetaDataList) {
				if(!DeniedMetaTags.Contains(MetaDataEntry)) {
					Variant ObjectType = MetaItem.GetMeta(MetaDataEntry);
					Variant MemoryAllocation;

					// Set Vec3 To array cuz godot is a little bitch
					// Reason: converting variant to vectors DOES NOT WORK.... :D
					if(ObjectType.VariantType == Variant.Type.Vector3) {
						Vector3 ConvertedVector = (Vector3)MetaItem.GetMeta(MetaDataEntry,Vector3.Zero);

						MemoryAllocation = new Godot.Collections.Array<float>{
							ConvertedVector.X,
							ConvertedVector.Y,
							ConvertedVector.Z
						};
					// Same shitty rule applies here
					} else if(ObjectType.VariantType == Variant.Type.Vector2) {
						Vector2 ConvertedVector = (Vector2)MetaItem.GetMeta(MetaDataEntry,Vector2.Zero);

						MemoryAllocation = new Godot.Collections.Array<float>{
							ConvertedVector.X,
							ConvertedVector.Y
						};
					} else {
						MemoryAllocation = ObjectType;
					}

					// ensures that a encoded version of the data type exists
					if(!ENCRYPTION_LIBRARY.ContainsKey(MetaDataEntry)) {
						GD.PushError("DATA Encryption key of " + MetaDataEntry + " Not found, please add itttttt");
					} else {
						// We apply all the meta
						SelectedItemCompile.Add(ENCRYPTION_LIBRARY[MetaDataEntry],MemoryAllocation);
					}
				}
			}
		}

		return PlacementSerializer;
	}

	// Reads The shit
	// hard descision to put it here
	public static void DecompilePlacingData() {
		Godot.Collections.Dictionary<int,Variant> PlacementData = (Godot.Collections.Dictionary<int,Variant>)DataHandler.Get(
	        new System.Collections.Generic.Dictionary<int, string>{
                [0] = "P",
            },
			"P",
            DataHandler.DATA_FILE
        );

		// Loop thtough all the shit and place it down
		foreach((int _, Variant Data) in PlacementData) {
			Godot.Collections.Dictionary<string,Variant> DataCorrected = (Godot.Collections.Dictionary<string,Variant>)Data;
			// Dk if this is memory conservative, but well roll with it for now...
			GridSystem3D PlacingAspect = new GridSystem3D((string)DataCorrected["N"]);

			Node3D PlacedItem = PlacingAspect.Place(
				new Vector3(
					(float)((Godot.Collections.Array<float>)DataCorrected["P"])[0],
					(float)((Godot.Collections.Array<float>)DataCorrected["P"])[1],
					(float)((Godot.Collections.Array<float>)DataCorrected["P"])[2]
				),
				new Vector3(
					(float)((Godot.Collections.Array<float>)DataCorrected["S"])[0],
					(float)((Godot.Collections.Array<float>)DataCorrected["S"])[1],
					(float)((Godot.Collections.Array<float>)DataCorrected["S"])[2]
				),
				(int)DataCorrected["R"],
				true
			);
		
			// Loop through all the meta data + Name
			Node MetaNode = GridSystem3D.GetItemMetaData(PlacedItem);
			Godot.Collections.Array<Godot.StringName> MetaDataList = MetaNode.GetMetaList();

			// load All The Meta Data
			foreach(string MetaDataEntry in MetaDataList) {
				if(ENCRYPTION_LIBRARY.ContainsKey(MetaDataEntry) && DataCorrected.ContainsKey(ENCRYPTION_LIBRARY[MetaDataEntry])) {
					MetaNode.SetMeta(MetaDataEntry,DataCorrected[ENCRYPTION_LIBRARY[MetaDataEntry]]);
				}
			}
		}
	}
}