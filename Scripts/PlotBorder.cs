using Godot;
using System;
using System.Collections.Generic;

public partial class PlotBorder : Node
{
	// Just general Storage For All The Voxels
	public static Dictionary<Node3D,int> VOXEL_GRID_TYPE_STORAGE = new Dictionary<Node3D, int>();
	public static Dictionary<Node3D,bool> VOXEL_GRID_STATE_STORAGE = new Dictionary<Node3D, bool>();
	public static Dictionary<string,Node3D> FENCE_COLLECTIONS_STORAGE = new Dictionary<string, Node3D>();
	// Objects
	public static Node FENCING_STORAGE;

	//private static float OffsetFromBoundary = 0.5f;

	// Creates A Fence Post
	private static void CreateFencePost(Vector3 _FencePostPosition, Node3D _Storage) {
		PackedScene FencePostScene = GD.Load<PackedScene>("res://packages/Fence_Post.tscn");
		Node3D FencePostPrefab = FencePostScene.Instantiate<Node3D>();

		FencePostPrefab.Position = new Vector3(_FencePostPosition.X,GridHandler.CELL_HEIGHT,_FencePostPosition.Z);
		FencePostPrefab.Name = "FencePost_" + _FencePostPosition.ToString();
		_Storage.AddChild(FencePostPrefab);
	}

	// Creates a connecting 2 posts with each other
	private static void CreateFenceBridge(Vector3 _PostPosition01, Vector3 _PostPosition02, Node3D _Storage) {
		PackedScene FenceBridgeScene = GD.Load<PackedScene>("res://packages/Fence_Middle.tscn");
		Node3D FenceBridgePrefab = FenceBridgeScene.Instantiate<Node3D>();
		_Storage.AddChild(FenceBridgePrefab);

		Vector3 BridgeCenter = (_PostPosition01 + _PostPosition02) / 2;

		FenceBridgePrefab.Position = new Vector3(BridgeCenter.X,GridHandler.CELL_HEIGHT,BridgeCenter.Z);
		FenceBridgePrefab.RotationDegrees =  new Vector3(0,(float)Math.Atan2(_PostPosition02.X - _PostPosition01.X, _PostPosition02.Z - _PostPosition01.Z) * (float)(180 / Math.PI),0);
		FenceBridgePrefab.Name = "FenceBridge_" + BridgeCenter.ToString();
	}

	// Generates the positioning for the fences
	private static void GenerateFencing(Vector3 _Start, Vector3 _End, Node3D _Storage) {
		Vector3 Direction = (_Start - _End).Normalized();
		/*Vector3 OffsetLookVector = new Vector3(OffsetFromBoundary,0,OffsetFromBoundary) * Direction;
		Vector3 inv = new Vector3(OffsetFromBoundary,0,OffsetFromBoundary) * new Vector3(1 - Math.Abs(Direction.X), 1 - Math.Abs(Direction.Y), 1 - Math.Abs(Direction.Z));

		_Start += inv;
		_End += inv;
		_Start += OffsetLookVector;
		_End -= OffsetLookVector;*/

		Vector3 PositionDifference = _End - _Start;

		float Magnitude = (float)Math.Sqrt(
			PositionDifference.X * PositionDifference.X + 
			PositionDifference.Y * PositionDifference.Y +
			PositionDifference.Z * PositionDifference.Z
		);

		// Generate the increments
		float FenceItterations = (float)Math.Round(Magnitude / (GridHandler.CELL_SIZE * 2),0);
		Vector3 PositionIncrements = PositionDifference / FenceItterations;
		Vector3 PreviousPostPosition = Vector3.Zero;

		for(int Increment = 0; Increment < FenceItterations + 1; Increment++) {
			//Vector3 OffsetLookVector = new Vector3(OffsetFromBoundary,OffsetFromBoundary,OffsetFromBoundary) * Direction;
			//NextPosition += OffsetLookVector / (FenceItterations + 1) * Increment;
			Vector3 NextPosition = _Start + (PositionIncrements * Increment);
			CreateFencePost(NextPosition,_Storage);

			// BRidges the gaps between the posts
			if(PreviousPostPosition != Vector3.Zero) {
				CreateFenceBridge(PreviousPostPosition,NextPosition,_Storage);
			}
			PreviousPostPosition = NextPosition;
		}
	}

	// Creates New Fencing For Certain Voxels
	private static void ReCalebrateVoxelFencing(Node3D _Voxel) {
		// Clear the old One
		if(FENCE_COLLECTIONS_STORAGE.ContainsKey(_Voxel.Position.ToString())) {
			FENCE_COLLECTIONS_STORAGE[_Voxel.Position.ToString()].QueueFree();
			FENCE_COLLECTIONS_STORAGE.Remove(_Voxel.Position.ToString());
		}

		// Stop if the voxel isnt purchased
		if(!(bool)_Voxel.GetMeta("VoxelIsPurchased",false)) {
			return;
		}

		// Create Teh Group Node
		Node3D Fence_Collection = new Node3D();

		FENCING_STORAGE.AddChild(Fence_Collection);
		Fence_Collection.Name = _Voxel.Position.ToString();
		FENCE_COLLECTIONS_STORAGE.Add(_Voxel.Position.ToString(),Fence_Collection);

		// Checks the sides And Creates The VOundaries
		void CreateFenceSection(List<string> __KeyDirections) {
			// Idk wtf i did here
			// i guess grabbing to the and bottom corner of each voxel
			Dictionary<String,Vector3> TopKeyToBoundaryConverter = new Dictionary<string, Vector3> {
				["Left"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2),
				["Right"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1),
				["Front"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2),
				["Back"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2),
			};
			Dictionary<String,Vector3> BottomKeyToBoundaryConverter = new Dictionary<string, Vector3> {
				["Left"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2),
				["Right"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1),
				["Front"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1),
				["Back"] = _Voxel.Position + new Vector3(GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1,0,GridHandler.CELL_SIZE * GridHandler.VOXEL_CLUSTER_SIZE / 2 *- 1),
			};

			// Sift through the sides and create them
			foreach(string DirectionalKey in __KeyDirections) {
				Node3D DirectionalCollection = new Node3D();

				Fence_Collection.AddChild(DirectionalCollection);
				DirectionalCollection.Name = DirectionalKey;

				GenerateFencing(TopKeyToBoundaryConverter[DirectionalKey],BottomKeyToBoundaryConverter[DirectionalKey],DirectionalCollection);
			}
		}

		// The same painful thing again
		switch((int)_Voxel.GetMeta("VoxelIDType",0)) {
			case 0: CreateFenceSection(new List<string>{"Left","Right","Front","Back"}); break;
			case 1: CreateFenceSection(new List<string>{"Right","Back","Front"}); break;
			case 2: CreateFenceSection(new List<string>{"Back","Front","Left"}); break;
			case 3: CreateFenceSection(new List<string>{"Right","Back","Left"}); break;
			case 4: CreateFenceSection(new List<string>{"Front","Right","Left"}); break;
			case 5: CreateFenceSection(new List<string>{"Front","Back"}); break;
			case 6: CreateFenceSection(new List<string>{"Right","Back"}); break;
			case 7: CreateFenceSection(new List<string>{"Right","Front"}); break;
			case 8: CreateFenceSection(new List<string>{"Left","Back"}); break;
			case 9: CreateFenceSection(new List<string>{"Left","Front"}); break;
			case 10: CreateFenceSection(new List<string>{"Left","Right"}); break;
			case 11: CreateFenceSection(new List<string>{"Back"}); break;
			case 12: CreateFenceSection(new List<string>{"Front"}); break;
			case 13: CreateFenceSection(new List<string>{"Right"}); break;
			case 14: CreateFenceSection(new List<string>{"Left"}); break;
		}
	}
	
	// Updates The WHole Shabang
	// Gets Called in the voxelgridhandler
	public static void Update() {
		List<Node3D> VoxelUpdateLog = new List<Node3D>();

		// Check teh shitty diffrences
		foreach((Node3D Voxel, int LastVoxelType) in VOXEL_GRID_TYPE_STORAGE) {
			if((int)Voxel.GetMeta("VoxelIDType",0) != LastVoxelType) {
				VoxelUpdateLog.Add(Voxel);
				VOXEL_GRID_TYPE_STORAGE[Voxel] = (int)Voxel.GetMeta("VoxelIDType",0);

				// If purchase state changes
			} else if((bool)Voxel.GetMeta("VoxelIsPurchased",false) != VOXEL_GRID_STATE_STORAGE[Voxel]) {
				VOXEL_GRID_STATE_STORAGE[Voxel] = (bool)Voxel.GetMeta("VoxelIsPurchased",false);
				VoxelUpdateLog.Add(Voxel);
			}
		}

		// update The Updated Voxels
		foreach(Node3D Voxel in VoxelUpdateLog) {
			ReCalebrateVoxelFencing(Voxel);
		}
	}

	// Sets Everything Up
	private void ConstructDataCasing() {
		foreach((string _ ,Node3D Voxel) in VoxelGridHandler.VOXEL_GRID_LIBRARY) {
			VOXEL_GRID_TYPE_STORAGE.Add(Voxel,(int)Voxel.GetMeta("VoxelIDType",0));
			VOXEL_GRID_STATE_STORAGE.Add(Voxel,(bool)Voxel.GetMeta("VoxelIsPurchased",false));

			ReCalebrateVoxelFencing(Voxel);
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		FENCING_STORAGE = this;
		ConstructDataCasing();
	}
}
