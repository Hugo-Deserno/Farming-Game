using Godot;
using System;
using System.Collections.Generic;
using System.IO;

/*
	OK, so little thingie for my dumb incompetent brain,
	
	Why is my item floating?????
	well, go to the object node itself, not the hitbox or the prefab
	and chnagte it so that node is at the bottom of the object
	easy.
*/

public partial class PlacingGridHandler : Node
{
	// Paths
	public static string PATH_TO_ITEMS_STORAGE = "/root/Workspace/Game/ImportantResources/PlacingSystem/GridSystem3DStorage";
	public static string PATH_TO_VISUAL_STORAGE = "/root/Workspace/Game/ImportantResources/PlacingSystem/VisualStorage";
	public static string PATH_TO_PLACED_STORAGE = "/root/Workspace/Game/ImportantResources/PlacingSystem/PlacedStorage";
	public static string PATH_TO_ITEM_RESOURCES = "res://packages/Placeable/";
	public static string PATH_TO_CELL_TEXTURE = "res://Images/Grid_Cell.png";
	public static string PATH_TO_CELL_FILL_TEXTURE = "res://Images/Grid_Cell_Filled.png";
	// Placing info
	public readonly static float CELL_SIZE = 0.5f;
	public readonly static float CELL_HEIGHT = 0.5f;
	public readonly static int VOXEL_CLUSTER_SIZE = 12;
	public readonly static int VOXEL_CLUSTER_AMOUNT = 10;
	public readonly static int ROTATION_AMOUNT = 90;
	public readonly static Color NON_COLLISION_COLOR = Color.Color8(0,245,61,150);
	public readonly static Color COLLISION_COLOR = Color.Color8(245,0,61,150);
	// Runtime config
	public static bool SMOOTH_PLACING = true;
	public static int CURRENT_ROTATION = 0;
	// General Tracking Shit
	public static bool IS_PLACING = false;
	public static bool IS_COLLIDING = false;
	public static GridSystem3D PLACING_SYSTEM;
	public static string ITEM_NAME;
	// Objects
	public static Node NODE_PLACING_STORAGE;
	public static Node NODE_VISUAL_STORAGE;
	public static Node NODE_PLACED_STORAGE;
	public static Node3D NODE_WORKSPACE;
	// Privates
	private static Camera3D NodeCharacterCamera;
	private static Viewport RayCastViewPort;
	private static Vector2 RayCastLastScreenSpaceMousePosition = Vector2.Zero;

	/////////////////////////////////////////////////////////////////////////
	
	// Gets the grid floor and says fuck you to the other objects
	public static Vector3 RayCastFromCursor(Godot.Collections.Array<Godot.Rid> _ExclusionList, string _DesiredTraget) {
		// Gets the type of vector it needs to return
		// So when youre dragging the mouse still functions
		Vector2 GetScreenSpaceMousePosition() {
			if(Input.MouseMode is Input.MouseModeEnum.Visible) {
				return RayCastViewPort.GetMousePosition();
			} else {
				return RayCastLastScreenSpaceMousePosition;
			}
		}

		// get Descandants to exclude here
		PhysicsDirectSpaceState3D SpaceDirection = NODE_WORKSPACE.GetWorld3D().DirectSpaceState;

		// Get directionals and stuff
		RayCastLastScreenSpaceMousePosition = GetScreenSpaceMousePosition();
		Vector3 GlobalSpaceMousePosition = NodeCharacterCamera.ProjectRayOrigin(RayCastLastScreenSpaceMousePosition);

		// more garbage
		Vector3 AllignedSpacePosition = GlobalSpaceMousePosition + NodeCharacterCamera.ProjectRayNormal(RayCastLastScreenSpaceMousePosition) * 100;
		Godot.PhysicsRayQueryParameters3D PhysicsRayCastQuery = PhysicsRayQueryParameters3D.Create(GlobalSpaceMousePosition,AllignedSpacePosition);
		
		PhysicsRayCastQuery.CollideWithAreas = true;
		PhysicsRayCastQuery.Exclude = _ExclusionList;

		Godot.Collections.Dictionary RayCastProperties = SpaceDirection.IntersectRay(PhysicsRayCastQuery);

		if(RayCastProperties.Count != 0) {
			// If there are any other static bodies in the way then fire the ray again with the object on the exclude list
			if(((StaticBody3D)RayCastProperties["collider"]).Name != _DesiredTraget) {
				_ExclusionList.Add((Rid)RayCastProperties["collider"]);
				return RayCastFromCursor(_ExclusionList,_DesiredTraget);
			} else {
				return (Vector3)RayCastProperties["position"];
			}
		}
		return Vector3.Zero;
	}

	// Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		NODE_PLACING_STORAGE = GetNode<Node>(PATH_TO_ITEMS_STORAGE);
		NODE_VISUAL_STORAGE = GetNode<Node>(PATH_TO_VISUAL_STORAGE);
		NODE_PLACED_STORAGE = GetNode<Node>(PATH_TO_PLACED_STORAGE);
		NODE_WORKSPACE = GetNode<Node3D>("/root/Workspace/Game");
		RayCastViewPort = GetViewport();
		NodeCharacterCamera = GetNode<Camera3D>("/root/Workspace/Game/ImportantResources/Character/CharacterBody/CameraRoot/Camera");

		DataRepository.DecompilePlacingData();
	}

    // COntrols
	// Do Note it can handle mutiple placing instances at once buty here its setup so it can only handle one cuz
	// why tf would you need two of em running at the same time
    public override void _Process(double _DeltaTime) {
		// Updates The System
		if(PLACING_SYSTEM != null) {
			PLACING_SYSTEM.Update(_DeltaTime);
		}

        if(Input.IsActionJustPressed("Placing_Rotate") && IS_PLACING && GetViewport().GuiGetFocusOwner() == null) {
			PLACING_SYSTEM.Rotate(null); // pass null cuz itll just add 90 deg
		}
		if(Input.IsActionJustPressed("Placing_Cancel") && IS_PLACING && GetViewport().GuiGetFocusOwner() == null) {
			PLACING_SYSTEM.Disable();
		}
		if(Input.IsActionJustPressed("Placing_Place") && IS_PLACING && GetViewport().GuiGetFocusOwner() == null) {
			PLACING_SYSTEM.Place(GridSystem3D.CURRENT_ITEM_HITBOX.Position,GridSystem3D.CURRENT_ITEM_HITBOX.Scale,PlacingGridHandler.CURRENT_ROTATION,false);
		}
    }
}

// Does The Actual placing
public class GridSystem3D {
	// Resources
	public static MeshInstance3D CURRENT_ITEM_HITBOX;
	public static Node3D CURRENT_ITEM_PREFAB;
	//SPrangs
	private Spring MovementSpring = new Spring(0.7f,3.2f, Vector3.Zero);
	private Spring RotationSpring = new Spring(0.6f,2.7f, Vector3.Zero);
	// Just General Garbage
	private Node3D LastSnappedToVoxel = null; // snapping to bounds if mouse is on purchased plot
	private bool CanUsePlacingUpdateFunction = false;
	private bool CanOverShootSprings = false;

	// Statics
	private static PackedScene CurrentSelectedItemScene;
	private static Node3D CurrentSelectedItem;
	private static PlacingGridVisualization PlacingGridVisualization;
	// MATHHHH

	// Converts Rotation To Normal Size
	public static Vector3 NormalizeScale(Vector3 _ObjectSize, float _ObjectOrientationY) {
		Vector3 ConvertedSize = _ObjectSize;

		switch(_ObjectOrientationY % 360) {

			case 0 : ConvertedSize = _ObjectSize; break;
			case 90: ConvertedSize = new Vector3(_ObjectSize.Z,_ObjectSize.Y,_ObjectSize.X); break;
			case 180: ConvertedSize = _ObjectSize *- 1; break;
			case 270: ConvertedSize = new Vector3(_ObjectSize.Z,_ObjectSize.Y,_ObjectSize.X) *- 1; break;
		}
		return ConvertedSize;
	}

	// locks The Object to a grid
	public static Vector3 RasterizeObjectPosition(Vector3 _ObjectPosition, Vector3 _ObjectSize, float _ObjectRotationY, float _CustomGridSize) {
		// Rounds the number correctly
		float RoundPositionToGrid(float __PositionOnAxis) {
			if(Math.Abs(__PositionOnAxis) % _CustomGridSize < _CustomGridSize / 2) {
				return Math.Sign(__PositionOnAxis) * (Math.Abs(__PositionOnAxis) - (Math.Abs(__PositionOnAxis) % _CustomGridSize));
			} else {
				return Math.Sign(__PositionOnAxis) * (Math.Abs(__PositionOnAxis) - (Math.Abs(__PositionOnAxis) % _CustomGridSize) + _CustomGridSize);
			}
		}
		
		// locks It To The Grid
		float RasterizeVectorAxis(string __VectorPositionAxis) {
			Vector3 NormalizedSize = NormalizeScale(_ObjectSize,_ObjectRotationY);

			float ObjectPositionAxis = __VectorPositionAxis == "X" ? _ObjectPosition.X : __VectorPositionAxis == "Y" ? _ObjectPosition.Y : __VectorPositionAxis == "Z"  ? _ObjectPosition.Z : 0;
			float ObjectScaleAxis = __VectorPositionAxis == "X" ? NormalizedSize.X : __VectorPositionAxis == "Y" ? NormalizedSize.Y : __VectorPositionAxis == "Z"  ? NormalizedSize.Z : 0;

			float AxisVectorAmount = RoundPositionToGrid(ObjectPositionAxis);

			if(Math.Abs(ObjectScaleAxis) == _CustomGridSize) {
				AxisVectorAmount = ObjectPositionAxis - ObjectPositionAxis % _CustomGridSize;
			}
			return AxisVectorAmount;
		}

		return new Vector3(
			RasterizeVectorAxis("X"),
			0,
			RasterizeVectorAxis("Z")
		);
	}

		// Ok so some reasoning here
	/*
		why cant you just get every direction cuz it goes voxel from voxel,
		nope, cuz size is also calculated
		this means that you cant place inbetween voxels
	*/

	// Clamps A Object To The Side Of A Voxel
	public static Vector3 ClampObjectOnVoxel(Node3D _Voxel, Vector3 _ObjectPosition, Vector3 _NormalizedObjectSize, List<string> _Directions) {
		Dictionary<string,float> VoxelExtends = VoxelGrid.GetVoxelExtends(_Voxel);
		_NormalizedObjectSize = new Vector3(
			Math.Abs(_NormalizedObjectSize.X),
			Math.Abs(_NormalizedObjectSize.Y),
			Math.Abs(_NormalizedObjectSize.Z)
		);

		// SHitty tables to strore info
		Dictionary<string,bool> CollidingWithBoundaries = new Dictionary<string, bool> {
			["Max_X"] = false,							["Max_Y"] = false,
			["Min_X"] = false,							["Min_Y"] = false,
		};
		Dictionary<string,float> ObjectBoundaries = new Dictionary<string, float> {
			["Max_X"] = _ObjectPosition.X + (_NormalizedObjectSize.X / 2),	["Max_Y"] = _ObjectPosition.Z + (_NormalizedObjectSize.Z / 2),
			["Min_X"] = _ObjectPosition.X - (_NormalizedObjectSize.X / 2),	["Min_Y"] = _ObjectPosition.Z - (_NormalizedObjectSize.Z / 2),
		};

		// Check if Extruding
		if(ObjectBoundaries["Max_X"] >= VoxelExtends["Max_X"] && _Directions.Contains("Front")) {
			CollidingWithBoundaries["Max_X"] = true;
		}
		if(ObjectBoundaries["Min_X"] <= VoxelExtends["Min_X"] && _Directions.Contains("Back")) {
			CollidingWithBoundaries["Min_X"] = true;
		}
		if(ObjectBoundaries["Max_Y"] >= VoxelExtends["Max_Y"] && _Directions.Contains("Left")) {
			CollidingWithBoundaries["Max_Y"] = true;
		}
		if(ObjectBoundaries["Min_Y"] <= VoxelExtends["Min_Y"] && _Directions.Contains("Right")) {
			CollidingWithBoundaries["Min_Y"] = true;
		}

		Vector3 FinalizedVector = new Vector3(
			(CollidingWithBoundaries["Max_X"] == true) ? VoxelExtends["Max_X"] - (_NormalizedObjectSize.X / 2) : (CollidingWithBoundaries["Min_X"] == true) ? VoxelExtends["Min_X"] + (_NormalizedObjectSize.X / 2) : _ObjectPosition.X,
			_ObjectPosition.Y,
			(CollidingWithBoundaries["Max_Y"] == true) ? VoxelExtends["Max_Y"] - (_NormalizedObjectSize.Z / 2) : (CollidingWithBoundaries["Min_Y"] == true) ? VoxelExtends["Min_Y"] + (_NormalizedObjectSize.Z / 2) : _ObjectPosition.Z
		);

		//bool TopLeftPurchased = (bool)VoxelGrid.GetVoxelFromPosition(new Vector3(ObjectBoundaries["Max_X"] - (GridSystemGridSize / 2),0,ObjectBoundaries["Max_Y"] - (GridSystemGridSize / 2))).GetMeta("VoxelIsPurchased",false);
		
		// CHECKS CORNERS IF THEYRE PURCHASED
		// also disables corner clipping
		
		// Checks If The corner is valid and clipping
		void CheckBoundsSide(Vector3 __DeterminedVoxelPosition, string __BoundaryName) {
			Node3D NewVoxel = VoxelGrid.GetVoxelFromPosition(__DeterminedVoxelPosition);

			if(NewVoxel != null) {
				if(!(bool)NewVoxel.GetMeta("VoxelIsPurchased",false)) {
					CollidingWithBoundaries[__BoundaryName] = true;
				}
			}
		}
	
		// Top Corners
		CheckBoundsSide(new Vector3(
			FinalizedVector.X + (_NormalizedObjectSize.X / 2) - (PlacingGridHandler.CELL_SIZE / 2),
			_ObjectPosition.Y,
			FinalizedVector.Z + (_NormalizedObjectSize.Z / 2) - (PlacingGridHandler.CELL_SIZE / 2)
		),"Max_X");
		CheckBoundsSide(new Vector3(
			FinalizedVector.X + (_NormalizedObjectSize.X / 2) - (PlacingGridHandler.CELL_SIZE / 2),
			_ObjectPosition.Y,
			FinalizedVector.Z - (_NormalizedObjectSize.Z / 2) - (PlacingGridHandler.CELL_SIZE / 2)
		),"Min_Y");
		// Bottom Corners
		CheckBoundsSide(new Vector3(
			FinalizedVector.X - (_NormalizedObjectSize.X / 2) + (PlacingGridHandler.CELL_SIZE / 2),
			_ObjectPosition.Y,
			FinalizedVector.Z + (_NormalizedObjectSize.Z / 2) + (PlacingGridHandler.CELL_SIZE / 2)
		),"Max_Y");
		CheckBoundsSide(new Vector3(
			FinalizedVector.X - (_NormalizedObjectSize.X / 2) + (PlacingGridHandler.CELL_SIZE / 2),
			_ObjectPosition.Y,
			FinalizedVector.Z - (_NormalizedObjectSize.Z / 2) + (PlacingGridHandler.CELL_SIZE / 2)
		),"Min_X");

		FinalizedVector = new Vector3(
			(CollidingWithBoundaries["Max_X"] == true) ? VoxelExtends["Max_X"] - (_NormalizedObjectSize.X / 2) : (CollidingWithBoundaries["Min_X"] == true) ? VoxelExtends["Min_X"] + (_NormalizedObjectSize.X / 2) : _ObjectPosition.X,
			_ObjectPosition.Y,
			(CollidingWithBoundaries["Max_Y"] == true) ? VoxelExtends["Max_Y"] - (_NormalizedObjectSize.Z / 2) : (CollidingWithBoundaries["Min_Y"] == true) ? VoxelExtends["Min_Y"] + (_NormalizedObjectSize.Z / 2) : _ObjectPosition.Z
		);

		return FinalizedVector;
	}

	// collsions

	// handles the collision
	public Vector3 CollideWithBoundaries(Vector3 _RayCastPosition,Vector3 _ObjectPosition, Vector3 _ObjectSize, float _ObjectRotationY) {
		Vector3 NormalizedSize = NormalizeScale(_ObjectSize,_ObjectRotationY);
		Node3D CurrentVoxel = VoxelGrid.GetVoxelFromPosition(_RayCastPosition);

		Vector3 CheckVoxelID(Node3D __SelectedVoxel) {
			switch((int)__SelectedVoxel.GetMeta("VoxelIDType",0)) {
				case 0: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Right","Front","Back","Left"});
				case 1: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Right","Front","Back"});
				case 2: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Left","Front","Back"});
				case 3: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Left","Right","Back"});
				case 4: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Left","Right","Front"});
				case 5: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Back","Front"});
				case 6: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Back","Right"});
				case 7: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Front","Right"});
				case 8: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Back","Left"});
				case 9: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Front","Left"});
				case 10: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Right","Left"});
				case 11: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Back"});
				case 12: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Front"});
				case 13: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Right"});
				case 14: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Left"});
				case 15: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{});
				default: return ClampObjectOnVoxel(__SelectedVoxel, _ObjectPosition, NormalizedSize, new List<string>{"Right","Front","Back","Left"});
			}
		}

		if(CurrentVoxel != null && (bool)CurrentVoxel.GetMeta("VoxelIsPurchased",false)) {
			return CheckVoxelID(CurrentVoxel);
		} else { // if the mouse is outside a purchased voxel
			Node3D ClosestVoxel = null;
			float ClosestMagntiudeOfVoxel = 100000000000;

        	foreach((string _,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
				if((bool)Voxel.GetMeta("VoxelIsPurchased",false)) {
					Vector3 DifferencePositionBetweenMouseAndVoxel = _RayCastPosition - Voxel.Position;

					float MagnitudeBetweenVoxelAndMouse = (float)Math.Sqrt(DifferencePositionBetweenMouseAndVoxel.X * DifferencePositionBetweenMouseAndVoxel.X + DifferencePositionBetweenMouseAndVoxel.Y * DifferencePositionBetweenMouseAndVoxel.Y + DifferencePositionBetweenMouseAndVoxel.Z * DifferencePositionBetweenMouseAndVoxel.Z);
					if(MagnitudeBetweenVoxelAndMouse < ClosestMagntiudeOfVoxel) {
						ClosestVoxel = Voxel;
						ClosestMagntiudeOfVoxel = MagnitudeBetweenVoxelAndMouse;
					}
				}
			}

			if(ClosestVoxel != null) {
				// Makes it so springs cant pass over non purchased land
				
				void DiffirentailVoxelMagnitude() {
					// Set it if its null
					if(LastSnappedToVoxel == null) {
						LastSnappedToVoxel = ClosestVoxel;
					}

					//Vector3 NormalizedDiffirentialDirection = (LastSnappedToVoxel.Position - ClosestVoxel.Position).Normalized();
					Vector3 DiffirentialPosition = (LastSnappedToVoxel.Position - ClosestVoxel.Position) / (PlacingGridHandler.CELL_SIZE * PlacingGridHandler.VOXEL_CLUSTER_SIZE);

					// Check If the next voxel is seperated anbd if so disable the springs and snap to it
					if(Math.Abs(DiffirentialPosition.X) > 1 || Math.Abs(DiffirentialPosition.Z) > 1) {
						CanOverShootSprings = true;

						//Node3D RightSideVoxel = VoxelGrid.GetVoxelFromPosition(LastSnappedToVoxel.Position + new Vector3(GridSystemGridSize * GridSystemClusterSize,0,GridSystemGridSize * GridSystemClusterSize));
						//Node3D LeftSideVoxel = VoxelGrid.GetVoxelFromPosition(LastSnappedToVoxel.Position - new Vector3(GridSystemGridSize * GridSystemClusterSize,0,GridSystemGridSize * GridSystemClusterSize));
					}
					LastSnappedToVoxel = ClosestVoxel;
				}
				DiffirentailVoxelMagnitude();
				return CheckVoxelID(ClosestVoxel);
			}
		}

		return _ObjectPosition;
	}

	// Handles object to object collision
	public bool CollideWithOtherEntities(Vector3 _ObjectPositon, Vector3 _ObjectSize, float _CurrentYRotation) {
		Vector3 NormalizedScaleCurrentObject = NormalizeScale(_ObjectSize,_CurrentYRotation);
		float HitboxOffset = PlacingGridHandler.CELL_SIZE / 4; // system was being annoying

		NormalizedScaleCurrentObject = new Vector3(
			Math.Abs(NormalizedScaleCurrentObject.X),
			Math.Abs(NormalizedScaleCurrentObject.Y),
			Math.Abs(NormalizedScaleCurrentObject.Z)
		);
		
		Dictionary<string,float> BoundaryLibraryCurrentObject = new Dictionary<string, float> {
			["Front"] = _ObjectPositon.X - NormalizedScaleCurrentObject.X / 2,
			["Back"] = _ObjectPositon.X + NormalizedScaleCurrentObject.X / 2,
			["Left"] = _ObjectPositon.Z - NormalizedScaleCurrentObject.Z / 2,
			["Right"] = _ObjectPositon.Z + NormalizedScaleCurrentObject.Z / 2,
		};

		foreach(Node3D PlacedObject in PlacingGridHandler.NODE_PLACED_STORAGE.GetChildren()) {
			if(PlacedObject != null) {
				// We use meta cuz getting the object hitbox would be laggy
				// Figured that out the hard way :p
				Vector3 PlacedObjectPosition = (Vector3)PlacedObject.GetMeta("Position",PlacedObject.Position);
				Vector3 PlacedObjectSize = (Vector3)PlacedObject.GetMeta("Size",PlacedObject.Scale);

				Vector3 NormalizedScalePlacedObject = NormalizeScale(PlacedObjectSize,PlacedObject.RotationDegrees.Y);

				NormalizedScalePlacedObject = new Vector3(
					Math.Abs(NormalizedScalePlacedObject.X),
					Math.Abs(NormalizedScalePlacedObject.Y),
					Math.Abs(NormalizedScalePlacedObject.Z)
				);

				Dictionary<string,float> BoundaryLibraryPlacedObject = new Dictionary<string, float> {
					["Front"] = PlacedObjectPosition.X - NormalizedScalePlacedObject.X / 2,
					["Back"] = PlacedObjectPosition.X + NormalizedScalePlacedObject.X / 2,
					["Left"] = PlacedObjectPosition.Z - NormalizedScalePlacedObject.Z / 2,
					["Right"] = PlacedObjectPosition.Z + NormalizedScalePlacedObject.Z / 2,
				};

				if(BoundaryLibraryCurrentObject["Front"] < BoundaryLibraryPlacedObject["Back"] - HitboxOffset 
				&& BoundaryLibraryCurrentObject["Back"] - HitboxOffset > BoundaryLibraryPlacedObject["Front"]
				&& BoundaryLibraryCurrentObject["Left"] < BoundaryLibraryPlacedObject["Right"] - HitboxOffset
				&& BoundaryLibraryCurrentObject["Right"] - HitboxOffset > BoundaryLibraryPlacedObject["Left"]) {
					return true;
				}
			}
		}

		return false;
	}

	// Does Every thing
	public void Update(double _DeltaTime) {
		if(!CanUsePlacingUpdateFunction) {
			return;
		}
		ConvertVoxelsToGrid(CanUsePlacingUpdateFunction);

		// get world space mouse position
		Vector3 RayCastCursorPosition = PlacingGridHandler.RayCastFromCursor(new Godot.Collections.Array<Godot.Rid>{},"FloorColision");

		Vector3 RasterizedRayCastPosition = RasterizeObjectPosition(RayCastCursorPosition, CURRENT_ITEM_HITBOX.Scale, PlacingGridHandler.CURRENT_ROTATION, PlacingGridHandler.CELL_SIZE);
		Vector3 NormalizedRotation = NormalizeScale(CURRENT_ITEM_HITBOX.Scale,PlacingGridHandler.CURRENT_ROTATION);

		// Do this shit so that 1 by 3 dont unalign themselve from the grid
		Vector2 UnevenOffsetExtraction = new Vector2(
			Math.Abs(NormalizedRotation.X) % (PlacingGridHandler.CELL_SIZE * 2) == PlacingGridHandler.CELL_SIZE ? PlacingGridHandler.CELL_SIZE / 2 : 0,
			Math.Abs(NormalizedRotation.Z) % (PlacingGridHandler.CELL_SIZE * 2) == PlacingGridHandler.CELL_SIZE ? PlacingGridHandler.CELL_SIZE / 2 : 0
		);

		// Create position vector
		Vector3 NormalizedPosition = RasterizedRayCastPosition;
		// Apply the offset cuz degrees plus a 1/2 size ratio doesnt go hand in hand
		NormalizedPosition += new Vector3(UnevenOffsetExtraction.X,0,UnevenOffsetExtraction.Y);
		// And apply the height
		NormalizedPosition = new Vector3(NormalizedPosition.X,PlacingGridHandler.CELL_HEIGHT + (CurrentSelectedItem.Scale.Y / 2),NormalizedPosition.Z);

		// Collisions
		NormalizedPosition = CollideWithBoundaries(RayCastCursorPosition,NormalizedPosition,CURRENT_ITEM_HITBOX.Scale,PlacingGridHandler.CURRENT_ROTATION);

		// We Set The Garbage to the actual hitbox and not the model itself
		// Done for springs
		CURRENT_ITEM_HITBOX.RotationDegrees = new Vector3(0,PlacingGridHandler.CURRENT_ROTATION,0);
		CURRENT_ITEM_HITBOX.Position = NormalizedPosition;

		// Visualization and collision stuff
		PlacingGridHandler.IS_COLLIDING = CollideWithOtherEntities(NormalizedPosition,CURRENT_ITEM_HITBOX.Scale,PlacingGridHandler.CURRENT_ROTATION);
		PlacingGridVisualization.Update(NormalizedPosition,CURRENT_ITEM_HITBOX.Scale);
		PlacingGridVisualization.CheckCollisions();

		if(PlacingGridHandler.SMOOTH_PLACING) {
				// So that the spriung locks on when starting placing
			_DeltaTime = CanOverShootSprings ? 10000000000 : _DeltaTime;
			CanOverShootSprings = false;

			// Spring Stuff
			MovementSpring.SetGoal(NormalizedPosition);
			RotationSpring.SetGoal(new Vector3(0,PlacingGridHandler.CURRENT_ROTATION,0));

			Vector3 SpringedMovementPosition = MovementSpring.Step(_DeltaTime);
			Vector3 SpringedRotationPosition = RotationSpring.Step(_DeltaTime);

			CURRENT_ITEM_PREFAB.Position = SpringedMovementPosition;
			CURRENT_ITEM_PREFAB.RotationDegrees = SpringedRotationPosition;
		} else {
			CURRENT_ITEM_PREFAB.Position = NormalizedPosition;
			CURRENT_ITEM_PREFAB.RotationDegrees = new Vector3(0,PlacingGridHandler.CURRENT_ROTATION,0);;
		}
	}

	// Other helping stuff

	// Makes the godamn Grid Visible
	// Fuck godot instancing.. it sucks
	void ConvertVoxelsToGrid(bool _CurrentState) {
		foreach((string VoxelCords,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
			if((bool)Voxel.GetMeta("VoxelIsPurchased")) {
				MeshInstance3D PlaneGridVoxel = PlacingVoxelGridHandler.VOXEL_PLANE_LIBRARY[VoxelCords];

				if(PlaneGridVoxel != null && PlaneGridVoxel.Transparency == (_CurrentState ? 1f : 0f)) {
					// Fuck tweens
					if(_CurrentState) {
						Tween tween = PlacingGridHandler.NODE_WORKSPACE.CreateTween();
						tween.TweenProperty(PlaneGridVoxel,"transparency",_CurrentState ? 0f : 1f,0.3f);
					} else {
						PlaneGridVoxel.Transparency = 1f;
					}	
				}
			} else {
				MeshInstance3D PlaneGridVoxel = PlacingVoxelGridHandler.VOXEL_PLANE_LIBRARY[VoxelCords];
				PlaneGridVoxel.Transparency = 1f;
			}
		}

		if(_CurrentState) {
			foreach((string _,MeshInstance3D Tile) in PlacingGridVisualization.GRID_TILE_STORAGE) {
				if(Tile.Transparency == 1f) {
					Tween tween = PlacingGridHandler.NODE_WORKSPACE.CreateTween();
					tween.TweenProperty(Tile,"transparency",_CurrentState ? 0f : 1f,0.3f);
				}
			}
		}
	}

	// Gets the required resources
	public static (MeshInstance3D,Node3D) FindNesecaryNodes(Node3D _Object) {
		MeshInstance3D Hitbox = null;
		Node3D Prefab = null;

		foreach(var NodeInstance in _Object.GetChildren()) {
			if(NodeInstance.Name == "Hitbox") {
				Hitbox = (MeshInstance3D)NodeInstance;
			} else if(NodeInstance.Name == "Prefab") {
				Prefab = (Node3D)NodeInstance;
			}
		}
		
		return (Hitbox,Prefab);
	}

	// Gets Item meta Data
	public static Node GetItemMetaData(Node3D _Object) {
		foreach(var NodeInstance in _Object.GetChildren()) {
			if(NodeInstance.Name == "ItemMeta") {
				return NodeInstance;
			}
		}
		return null;
	}

	// TOOLS 	

	// Enables and sets the entire thing up
	public void Enable() {
		// return shit if it just doesnt work, cuz sometimes it likes to do weird thing
		if(PlacingGridHandler.ITEM_NAME == "") {
			GD.PushWarning("Called item doesnt exist");
			return;
		}

		CanUsePlacingUpdateFunction = true;

		if(DemolitionTool.DEMOLITION_TOOL_ENABLED) {
			Demolition.Disable();
		}

		// Moves the object into the scene
		if(CurrentSelectedItemScene != null) {
			CurrentSelectedItem = CurrentSelectedItemScene.Instantiate<Node3D>();
			CanOverShootSprings = true; // so that it snaps to the starting position

			PlacingGridHandler.NODE_PLACING_STORAGE.AddChild(CurrentSelectedItem);

			(CURRENT_ITEM_HITBOX,CURRENT_ITEM_PREFAB) = FindNesecaryNodes(CurrentSelectedItem);
			PlacingGridVisualization = new PlacingGridVisualization(CurrentSelectedItem.Name, CURRENT_ITEM_HITBOX.Scale);
		}
		PlacingGridHandler.IS_PLACING = true;
		ConvertVoxelsToGrid(true);
	}

	// die
	public void Disable() {
		ConvertVoxelsToGrid(false);
		PlacingGridVisualization.Remove();
		CanUsePlacingUpdateFunction = false;
		PlacingGridHandler.IS_PLACING = false;

		if(CurrentSelectedItem != null) {
			CurrentSelectedItem.QueueFree();
			CURRENT_ITEM_HITBOX = null;
			CURRENT_ITEM_PREFAB = null;
		}
	}

	// Dumps the wshit down
	public Node3D Place(Vector3 _Position, Vector3 _Scale, float _Rotation, bool _Override) {
		void CreateStaticBodies(Node3D __ObjectPrefab) {
			void SearchStatics(Node3D ___ChildObject) {
				foreach(object Node in ___ChildObject.GetChildren()) {
					if(((Node3D)Node).GetChildCount() > 0) {
						SearchStatics((Node3D)Node);
					}

					// Create a statiuc body
					// create a physics body
					// assign the collision mask to 2
					var ObjectType = Node.GetType();
					if(ObjectType.GetProperty("MaterialOverride") != null) {
						((MeshInstance3D)Node).CreateTrimeshCollision();
						
						((MeshInstance3D)Node).GetChild<StaticBody3D>(0).Name = "ItemCollision";
					}
				}
			}

			SearchStatics(__ObjectPrefab);
		}

		if(CurrentSelectedItem != null || _Override) {
			bool ObjectIsColliding = CollideWithOtherEntities(_Position,_Scale,_Rotation);

			if(!PlacingGridHandler.IS_COLLIDING && !ObjectIsColliding) {
				Node3D DesiredPlacingItem = CurrentSelectedItemScene.Instantiate<Node3D>();				
				PlacingGridHandler.NODE_PLACED_STORAGE.AddChild(DesiredPlacingItem);
				Node ItemMeta = GetItemMetaData(DesiredPlacingItem);

				DesiredPlacingItem.Name = PlacingGridHandler.ITEM_NAME + "_";
			
				DesiredPlacingItem.Position = new Vector3(_Position.X,PlacingGridHandler.CELL_HEIGHT,_Position.Z);
				DesiredPlacingItem.RotationDegrees = new Vector3(0,_Rotation,0);

				// Simplified for system use
				DesiredPlacingItem.SetMeta("Size",_Scale);
				DesiredPlacingItem.SetMeta("Position",_Position);
				DesiredPlacingItem.SetMeta("Rotation",_Rotation % 360);
				// Also Set Meta
				ItemMeta.SetMeta("Size",_Scale);
				ItemMeta.SetMeta("Position",_Position);
				ItemMeta.SetMeta("Rotation",_Rotation % 360);

				(MeshInstance3D ObjectHitbox, Node3D ObjectPrefab) = FindNesecaryNodes(DesiredPlacingItem);
				CreateStaticBodies(ObjectPrefab);

				return DesiredPlacingItem;
			}
		}
		return null;
	}

	// Rotates The Object
	public void Rotate(int? _RotationAmount) {
		if(_RotationAmount != null) {
			PlacingGridHandler.CURRENT_ROTATION = (int)_RotationAmount;
		} else {
			PlacingGridHandler.CURRENT_ROTATION += PlacingGridHandler.ROTATION_AMOUNT;
		}
	}

	// Constructa
	public GridSystem3D(string _ItemName) {
		if(!File.Exists(ProjectSettings.GlobalizePath("res://") + "Packages/Placeable/" + _ItemName + ".tscn")) {
			PlacingGridHandler.ITEM_NAME = "";
			return;
		} else {
			PlacingGridHandler.ITEM_NAME = _ItemName;
			CurrentSelectedItemScene = GD.Load<PackedScene>(PlacingGridHandler.PATH_TO_ITEM_RESOURCES + _ItemName + ".tscn");
		}
	}
}