using Godot;
using System;

// Controller IG
public partial class DemolitionTool : Node
{
	// Config trash
	public static string PATH_TO_VISUAL_STORAGE = "/root/Workspace/Game/ImportantResources/DemolitionTool/VisualStorage";
	public static string PATH_TO_CELL_TEXTURE = "res://Images/Grid_Cell_Error.png";
	public static string PATH_TO_OVERLAY_TEXTURE = "res://Images/Grid_Overlay_Error.png";
	public static string PATH_TO_OVERLAY_ALPHA_TEXTURE = "res://Images/Grid_Overlay_Error_Alpha.png";
	public static Color HIGHLIGHT_COLOR = Color.Color8(180,35,19,150);
	// general Data trash
	public static bool DEMOLITION_TOOL_ENABLED = false;
	public static MeshInstance3D CURRENT_SELECTED_ITEM;
	// Objects
	public static Node NODE_VISUAL_STORAGE;
	public static Node3D NODE_WORKSPACE;
	// Privates
	private static Camera3D NodeCharacterCamera;
	private static Viewport RayCastViewPort;
	private static Vector2 RayCastLastScreenSpaceMousePosition = Vector2.Zero;

	// RayCasts to find which object the cursor is currently on
	public static MeshInstance3D RayCastPlacedObject(Godot.Collections.Array<Godot.Rid> _ExclusionList, string _DesiredTraget) {
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
				return RayCastPlacedObject(_ExclusionList,_DesiredTraget);
			} else {
				Node3D Object = ((StaticBody3D)RayCastProperties["collider"]).GetParent<MeshInstance3D>().GetParent<Node3D>().GetParent<Node3D>();
				(MeshInstance3D ObjectHitbox, Node3D ObjectPrefab) = GridSystem3D.FindNesecaryNodes(Object);

				return ObjectHitbox;
			}
		}
		return null;
	}

	// Fires Everything Up
    public override void _Ready() {
		NODE_WORKSPACE = GetNode<Node3D>("/root/Workspace/Game");
		//NODE_PLANE_STORAGE = GetNode<Node>(PATH_TO_PLANE_STORAGE);
		NODE_VISUAL_STORAGE = GetNode<Node>(PATH_TO_VISUAL_STORAGE);
		RayCastViewPort = GetViewport();
		NodeCharacterCamera = GetNode<Camera3D>("/root/Workspace/Game/ImportantResources/Character/CharacterBody/CameraRoot/Camera");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double _DeltaTime) {
		// logic control crap
		if(Input.IsActionJustPressed("Demolition_Start") && GetViewport().GuiGetFocusOwner() == null) {
			DEMOLITION_TOOL_ENABLED = !DEMOLITION_TOOL_ENABLED ? true : false;

			if(DEMOLITION_TOOL_ENABLED) {
				// placingstuff here
				// SOoooooo i just disable palcing
				if(GridHandler.IS_PLACING && GridHandler.PLACING_SYSTEM != null) {
					GridHandler.PLACING_SYSTEM.Disable();
				}

				Demolition.Enable();
			} else {
				Demolition.Disable();
			}
		} else if(Input.IsActionJustPressed("Demolition_Demolish") && CURRENT_SELECTED_ITEM != null && DEMOLITION_TOOL_ENABLED && GetViewport().GuiGetFocusOwner() == null) {
			Demolition.Destroy(CURRENT_SELECTED_ITEM.GetParent<Node3D>());
		}

		Demolition.Update(_DeltaTime);
	}
}

// Just asthetic crap
public partial class DemolitionVisuals : DemolitionTool {
	private static Node3D LastHighlightedItem;
	private static Node3D LastOverlayedItem;
	private static string ObjectIDString = ""; // used for trakcing the last highlighted item

	/*
	public static void CreateVoxelPlane(Vector3 _VoxelPosition) {
		MeshInstance3D VoxelPlane = new MeshInstance3D();
        PlaneMesh VisibleMesh = new PlaneMesh();
        OrmMaterial3D FillMaterial = new OrmMaterial3D();

        // material Junk
        FillMaterial.Transparency = Godot.BaseMaterial3D.TransparencyEnum.Alpha;
        FillMaterial.AlbedoTexture = GD.Load<Texture2D>(PATH_TO_ERROR_GRID);
        FillMaterial.AlbedoColor = Color.Color8(255,255,255);
        FillMaterial.Uv1Triplanar = true;
        FillMaterial.Uv1WorldTriplanar = true;
        FillMaterial.Uv1Scale = new Vector3(1 / GridHandler.CELL_SIZE,1 / GridHandler.CELL_SIZE,1 / GridHandler.CELL_SIZE);
        FillMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;

        VoxelPlane.Mesh = VisibleMesh;
        VoxelPlane.MaterialOverride = FillMaterial;

        VoxelPlane.Scale = new Vector3(GridHandler.VOXEL_CLUSTER_SIZE * GridHandler.CELL_SIZE / 2,0.1f,GridHandler.VOXEL_CLUSTER_SIZE * GridHandler.CELL_SIZE / 2);
        VoxelPlane.Position = _VoxelPosition;
        VoxelPlane.Transparency = 1f;
        
        NODE_PLANE_STORAGE.AddChild(VoxelPlane);
        VoxelPlane.Name = _VoxelPosition.ToString();

        VOXEL_PLANE_LIBRARY.Add(VoxelPlane.Position.ToString(),VoxelPlane);
	}*/

	// Creates da tiles ig
	private static MeshInstance3D CreateGridTile(Vector2 _TilePosition) {
		MeshInstance3D GridTile = new MeshInstance3D();
		PlaneMesh VisibleMesh = new PlaneMesh();
		OrmMaterial3D FillMaterial = new OrmMaterial3D();

		// material Junk
		FillMaterial.Transparency = Godot.BaseMaterial3D.TransparencyEnum.Alpha;
		FillMaterial.AlbedoTexture = GD.Load<Texture2D>(PATH_TO_CELL_TEXTURE);
		FillMaterial.Uv1Triplanar = true;
		FillMaterial.Uv1WorldTriplanar = true;
		FillMaterial.Uv1Scale = new Vector3(1 / GridHandler.CELL_SIZE,1 / GridHandler.CELL_SIZE,1 / GridHandler.CELL_SIZE);
		FillMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;

		GridTile.Mesh = VisibleMesh;
		GridTile.MaterialOverride = FillMaterial;

		GridTile.Scale = new Vector3(GridHandler.CELL_SIZE / 2,0.1f,GridHandler.CELL_SIZE / 2);
		GridTile.Position = new Vector3(_TilePosition.X,GridHandler.CELL_HEIGHT,_TilePosition.Y);
		GridTile.Transparency = 0f;
		
		return GridTile;
	}

	/*
	// A sub class for the material Dictornary
	private struct materialCapsule {
		public Dictionary<Node,BaseMaterial3D> MaterialStorage = new Dictionary<Node, BaseMaterial3D>();
		public Node HomeNode;

		// Constructor SHit
		public materialCapsule(Node _PrefabNode) {
			HomeNode = _PrefabNode;
			MaterialOverlayStorage.Add(HomeNode, this); // this resembles the struct class itself
		}

		// Adds to the sub-dictonary
		public void Add(Node _SelectedNode, BaseMaterial3D _NodeMaterial) {
			MaterialStorage.Add(_SelectedNode,_NodeMaterial);
		}
	}*/

	public static void HighlightDemolishedItem(Node3D _Object) {
		// Creates The Material
		void DefineMaterial(MeshInstance3D __Object, ShaderMaterial __Material) {
			__Material.Shader = GD.Load<Shader>("res://Shaders/DemolitionHover.gdshader");
			__Material.SetShaderParameter("Scale",new Vector2(17,17));
			__Material.SetShaderParameter("Direction",new Vector2(0,-1));
			__Material.SetShaderParameter("Speed",0.3f);
			__Material.SetShaderParameter("Albedo",GD.Load<Texture2D>(PATH_TO_OVERLAY_TEXTURE));
			__Material.SetShaderParameter("Alpha",GD.Load<Texture2D>(PATH_TO_OVERLAY_ALPHA_TEXTURE));

			StandardMaterial3D ShaderPass = new StandardMaterial3D();
			__Material.NextPass = ShaderPass;

			ShaderPass.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
			ShaderPass.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
			ShaderPass.AlbedoColor = HIGHLIGHT_COLOR;

			__Object.MaterialOverlay = __Material;
		}

		if(LastOverlayedItem == _Object) {
			return;
		}
		LastOverlayedItem = _Object;

		// Dstiorys The Old one
		foreach(Node3D Objects in GridHandler.NODE_PLACED_STORAGE.GetChildren()) {
			if(Objects.ToString() == ObjectIDString) {
				(MeshInstance3D ObjectHitbox, Node3D ObjectPrefab) = GridSystem3D.FindNesecaryNodes(Objects);
				ObjectIDString = "";

				foreach(object Object in ObjectPrefab.GetChildren()) {
					var ObjectType = Object.GetType();

					if(ObjectType.GetProperty("MaterialOverride") != null) {
						((MeshInstance3D)Object).MaterialOverlay = null;
					}
				}
			}
		}

		// Creates New One
		if(_Object != null) {
			(MeshInstance3D ObjectHitbox, Node3D ObjectPrefab) = GridSystem3D.FindNesecaryNodes(_Object);
			ObjectIDString = _Object.ToString();

			foreach(object Object in ObjectPrefab.GetChildren()) {
				var ObjectType = Object.GetType();

				if(ObjectType.GetProperty("MaterialOverride") != null) {
					ShaderMaterial StructMaterial = new ShaderMaterial();
					DefineMaterial((MeshInstance3D)Object,StructMaterial);
				}
			}
		}
	}

	// Huighlights said items grid
	public static void HighlightItem(Node3D _CurrentItem) {
		// So it updates only when it has to
		if(LastHighlightedItem == _CurrentItem) {
			return;
		}
		LastHighlightedItem = _CurrentItem;
		
		//Destroys The Old Tiles
		foreach(var Object in NODE_VISUAL_STORAGE.GetChildren()) {
			if(Object.Name.ToString().Contains("DemolitionTile")) {
				Object.QueueFree();
			}
		}

		if(_CurrentItem != null) {
			Vector3 NormalizedScale = GridSystem3D.NormalizeScale((Vector3)_CurrentItem.GetMeta("Size",Vector3.Zero),(float)_CurrentItem.GetMeta("Rotation",0f));
			
			NormalizedScale = new Vector3(
				Math.Abs(NormalizedScale.X),
				Math.Abs(NormalizedScale.Y),
				Math.Abs(NormalizedScale.Z)
			);
			
			int TileAmountX = (int)Math.Round(NormalizedScale.X / GridHandler.CELL_SIZE,0);
			int TileAmountZ = (int)Math.Round(NormalizedScale.Z / GridHandler.CELL_SIZE,0);

			for(var XRows = 0; XRows < TileAmountX; XRows++) {
				for(var ZRows = 0; ZRows < TileAmountZ; ZRows++) {
					// Rasterize said position
					Vector3 RasterizedCellPosition = GridSystem3D.RasterizeObjectPosition(
						new Vector3(_CurrentItem.Position.X,0,_CurrentItem.Position.Z),
						(Vector3)_CurrentItem.GetMeta("Size",Vector3.Zero),
						(float)_CurrentItem.GetMeta("Rotation",0f),
						GridHandler.CELL_SIZE
					);

					// Do this shit so that 1/2 size ratio dont unalign themselve from the grid
					Vector2 UnevenOffsetExtraction = new Vector2(
						Math.Abs(NormalizedScale.X) % (GridHandler.CELL_SIZE * 2) == GridHandler.CELL_SIZE ? GridHandler.CELL_SIZE / 2 : 0,
						Math.Abs(NormalizedScale.Z) % (GridHandler.CELL_SIZE * 2) == GridHandler.CELL_SIZE ? GridHandler.CELL_SIZE / 2 : 0
					);

					// Create The offset
					RasterizedCellPosition -= new Vector3(
						((Vector3)_CurrentItem.GetMeta("Size",Vector3.Zero)).X / 2,
						0,
						((Vector3)_CurrentItem.GetMeta("Size",Vector3.Zero)).Z / 2
					) - new Vector3(
						UnevenOffsetExtraction.X,
						0,
						UnevenOffsetExtraction.Y
					);


					MeshInstance3D Tile = CreateGridTile(new Vector2(RasterizedCellPosition.X,RasterizedCellPosition.Z) + new Vector2(XRows * GridHandler.CELL_SIZE + (GridHandler.CELL_SIZE / 2),ZRows * GridHandler.CELL_SIZE + (GridHandler.CELL_SIZE / 2)));
					Tile.Name = "DemolitionTile" + Tile.Position;

					NODE_VISUAL_STORAGE.AddChild(Tile);
				}
			}
		}
	}

	private static void ConstructGrid() {

	}
} 

// DOES THE ACTUAL THINKING
public partial class Demolition : DemolitionTool {
	// Destroys Shit
	public static void Destroy(Node3D _Object) {
		if(_Object != null) {
			_Object.QueueFree();
		}
	}

	// Main Loop
	public static void Update(double _DeltaTime) {
		if(!DEMOLITION_TOOL_ENABLED) {
			return;
		}

	    CURRENT_SELECTED_ITEM = RayCastPlacedObject(new Godot.Collections.Array<Godot.Rid>{},"ItemCollision");

		Node3D SelectedNode = CURRENT_SELECTED_ITEM != null ? CURRENT_SELECTED_ITEM.GetParent<Node3D>() : null;
		DemolitionVisuals.HighlightItem(SelectedNode);
		DemolitionVisuals.HighlightDemolishedItem(SelectedNode);
	}

	// start the shit
    public static void Enable() {
		// We re use this line so whe can enable and disable it from other classes then its own class
		DEMOLITION_TOOL_ENABLED = true;
	}

	// Disbales the whole stuff
	public static void Disable() {
		// We re use this line so whe can enable and disable it from other classes then its own class
		DEMOLITION_TOOL_ENABLED = false;
		DemolitionVisuals.HighlightItem(null);
		DemolitionVisuals.HighlightDemolishedItem(null);
	}

	/*
	public static void ConvertVoxelsToGrid(bool _CurrentState) {
		foreach((string VoxelCords,Node3D Voxel) in VoxelGridHandler.VOXEL_GRID_LIBRARY) {
			if((bool)Voxel.GetMeta("VoxelIsPurchased")) {
				MeshInstance3D PlaneGridVoxel = VOXEL_PLANE_LIBRARY[VoxelCords];

				if(PlaneGridVoxel != null && PlaneGridVoxel.Transparency == (_CurrentState ? 1f : 0f)) {
					// Fuck tweens
					if(_CurrentState) {
						Tween tween = GridHandler.NODE_WORKSPACE.CreateTween();
						tween.TweenProperty(PlaneGridVoxel,"transparency",_CurrentState ? 0f : 1f,0.3f);
					} else {
						PlaneGridVoxel.Transparency = 1f;
					}	
				}
			} else {
				MeshInstance3D PlaneGridVoxel = VOXEL_PLANE_LIBRARY[VoxelCords];
				PlaneGridVoxel.Transparency = 1f;
			}
		}
	}

	// Sets Up Teh Voxel Grid
	public static void SetupVoxels() {
		if(VOXEL_PLANE_LIBRARY.Count == 0) {
			foreach((string _,Node3D Voxel) in VoxelGridHandler.VOXEL_GRID_LIBRARY) {
				DemolitionVisuals.CreateVoxelPlane((Vector3)Voxel.GetMeta("VoxelPosition",Vector3.Zero));
			}
		}
	}*/
}