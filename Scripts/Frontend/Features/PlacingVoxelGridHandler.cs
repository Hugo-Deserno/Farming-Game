using System;
using System.Collections.Generic;
using Godot;

// needed for placing system collsisionss

public partial class PlacingVoxelGridHandler : Node {
    // OBjects
    public static Node NODE_VOXEL_STORAGE;
    public static Node NODE_VOXEL_VISUALIZATION_STORAGE;
    // lib Storage
    public static Dictionary<string,Node3D> VOXEL_GRID_LIBRARY = new Dictionary<string, Node3D>();
    public static Dictionary<string,MeshInstance3D> VOXEL_PLANE_LIBRARY = new Dictionary<string, MeshInstance3D>();
	
    // Setup Up All The Tools For This nonsence
    // this stuff is wack
	private void SetupScene() {
		(float Max_X, float Min_X, float Max_Y, float Min_Y) = VoxelGrid.GetVoxelGridBounds();
        Vector3 VoxelGridCenter = new Vector3((Max_X + Min_X) / 2,0,(Max_Y + Min_Y) / 2);

		// Position the Godamn Floor
		MeshInstance3D RayCastFloorScanner = GetNode<MeshInstance3D>("/root/Workspace/Game/ImportantResources/PlacingSystem/RayCastScanner");
		Node CameraBoundaries = GetNode<Node>("/root/Workspace/Game/ImportantResources/CameraBounds");

		RayCastFloorScanner.Scale = new Vector3((Max_X - Min_X),1,(Max_Y - Min_Y));
		RayCastFloorScanner.Position = new Vector3(VoxelGridCenter.X,PlacingGridHandler.CELL_HEIGHT,VoxelGridCenter.Z);

		// Position The Boundaries
		CameraBoundaries.GetChild<Node3D>(0).Position = new Vector3(Max_X,PlacingGridHandler.CELL_HEIGHT,VoxelGridCenter.Z);
        CameraBoundaries.GetChild<Node3D>(1).Position = new Vector3(Min_X,PlacingGridHandler.CELL_HEIGHT,VoxelGridCenter.Z);
        CameraBoundaries.GetChild<Node3D>(2).Position = new Vector3(VoxelGridCenter.X,PlacingGridHandler.CELL_HEIGHT,Max_Y);
        CameraBoundaries.GetChild<Node3D>(3).Position = new Vector3(VoxelGridCenter.X,PlacingGridHandler.CELL_HEIGHT,Min_Y);
	}

    // Initliazes all the garbage
    public override void _Ready() {
        NODE_VOXEL_STORAGE = GetNode<Node>("/root/Workspace/Game/ImportantResources/PlacingSystem/VoxelGrid");
        NODE_VOXEL_VISUALIZATION_STORAGE = GetNode<Node>("/root/Workspace/Game/ImportantResources/PlacingSystem/VoxelVisualization");

        VoxelGrid.CreateNewVoxelGrid();
        SetupScene();
    }
}


// THE SYSTEM

public class VoxelGrid {
     // Gets basic voxel vector cords and normals
    static (Dictionary<string,Vector3>,Dictionary<string,Vector3>) GetVoxelInformation(Node3D _Voxel) {
        float VoxelIncrement = (float)Math.Round(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE,2);

        // Neighbour voxel locations
        Dictionary<string,Vector3> DirectionalDictonary = new Dictionary<string, Vector3>{
            ["Left"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")) + new Vector3(0,0,VoxelIncrement),
            ["Right"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")) + new Vector3(0,0,-VoxelIncrement),
            ["Front"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")) + new Vector3(VoxelIncrement,0,0),
            ["Back"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")) + new Vector3(-VoxelIncrement,0,0),
        };

        DirectionalDictonary["Left"] = new Vector3((float)Math.Round(DirectionalDictonary["Left"].X,2),(float)Math.Round(DirectionalDictonary["Left"].Y,2),(float)Math.Round(DirectionalDictonary["Left"].Z,2));
        DirectionalDictonary["Right"] = new Vector3((float)Math.Round(DirectionalDictonary["Right"].X,2),(float)Math.Round(DirectionalDictonary["Right"].Y,2),(float)Math.Round(DirectionalDictonary["Right"].Z,2));
        DirectionalDictonary["Front"] = new Vector3((float)Math.Round(DirectionalDictonary["Front"].X,2),(float)Math.Round(DirectionalDictonary["Front"].Y,2),(float)Math.Round(DirectionalDictonary["Front"].Z,2));
        DirectionalDictonary["Back"] = new Vector3((float)Math.Round(DirectionalDictonary["Back"].X,2),(float)Math.Round(DirectionalDictonary["Back"].Y,2),(float)Math.Round(DirectionalDictonary["Back"].Z,2));

        // Vector normal of voxel direction
        Dictionary<string,Vector3> DirectionalNormals = new Dictionary<string, Vector3>{
            ["Left"] = new Vector3(0,0,VoxelIncrement / 4),          ["Right"] = new Vector3(0,0,-VoxelIncrement / 4),
            ["Front"] = new Vector3(VoxelIncrement / 4,0,0),         ["Back"] = new Vector3(-VoxelIncrement / 4,0,0),
        };

        return (DirectionalDictonary,DirectionalNormals);
    }

    // Draws The Voxel Lines
    static void DrawVisualizationLine(Vector3 _Startpoint, Vector3 _Direction, string _VoxelName, Color _LineColor) { // direction should be to normal direction
        MeshInstance3D VoxelLineDraw = new MeshInstance3D();
        ImmediateMesh LineMesh = new ImmediateMesh();
        OrmMaterial3D LineMaterial = new OrmMaterial3D();

        VoxelLineDraw.Mesh = LineMesh;
        VoxelLineDraw.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        LineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines,LineMaterial);
        LineMesh.SurfaceAddVertex(_Startpoint);
        LineMesh.SurfaceAddVertex(_Startpoint + (_Direction * 1.25f));
        LineMesh.SurfaceEnd();

        LineMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        LineMaterial.AlbedoColor = _LineColor;
        
        PlacingVoxelGridHandler.NODE_VOXEL_VISUALIZATION_STORAGE.AddChild(VoxelLineDraw);
        VoxelLineDraw.Name = _VoxelName;
    }

    // Draws a square to indicate purchased land
    static void IndicatedActiveLandPiece(Vector3 _VoxelPosition, string _InstanceName) {
        MeshInstance3D VoxelActivationState = new MeshInstance3D();
        PlaneMesh VisibleMesh = new PlaneMesh();
        OrmMaterial3D FillMaterial = new OrmMaterial3D();

        VoxelActivationState.Scale = new Vector3(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE / 2,1,PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE / 2);
        VoxelActivationState.Position = _VoxelPosition;
        VoxelActivationState.Mesh = VisibleMesh;
        
        FillMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        FillMaterial.Transparency = Godot.BaseMaterial3D.TransparencyEnum.Alpha;
        FillMaterial.AlbedoColor = Color.Color8(255,255,255,50);
        VisibleMesh.Material = FillMaterial;

        PlacingVoxelGridHandler.NODE_VOXEL_VISUALIZATION_STORAGE.AddChild(VoxelActivationState);
        VoxelActivationState.Name = _InstanceName;
    }

    // Creates A Ready instance For the visible grid
    static void CreateVoxelGridInstance(Vector3 _VoxelPosition) {
        MeshInstance3D VoxelPlane = new MeshInstance3D();
        PlaneMesh VisibleMesh = new PlaneMesh();
        OrmMaterial3D FillMaterial = new OrmMaterial3D();

        // material Junk
        FillMaterial.Transparency = Godot.BaseMaterial3D.TransparencyEnum.Alpha;
        FillMaterial.AlbedoTexture = GD.Load<Texture2D>(PlacingGridHandler.PATH_TO_CELL_TEXTURE);
        FillMaterial.AlbedoColor = Color.Color8(255,255,255);
        FillMaterial.Uv1Triplanar = true;
        FillMaterial.Uv1WorldTriplanar = true;
        FillMaterial.Uv1Scale = new Vector3(1 / PlacingGridHandler.CELL_SIZE,1 / PlacingGridHandler.CELL_SIZE,1 / PlacingGridHandler.CELL_SIZE);
        FillMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;

        VoxelPlane.Mesh = VisibleMesh;
        VoxelPlane.MaterialOverride = FillMaterial;

        VoxelPlane.Scale = new Vector3(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE / 2,0.1f,PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE / 2);
        VoxelPlane.Position = _VoxelPosition;
        VoxelPlane.Transparency = 1f;
        
        PlacingGridHandler.NODE_VISUAL_STORAGE.AddChild(VoxelPlane);
        VoxelPlane.Name = _VoxelPosition.ToString();

        PlacingVoxelGridHandler.VOXEL_PLANE_LIBRARY.Add(VoxelPlane.Position.ToString(),VoxelPlane);
    }

     // Visualizes The Voxel Grid
    public static void VisualizeVoxelGrid(bool _CurrentState) {
        foreach(MeshInstance3D VisualVoxel in PlacingVoxelGridHandler.NODE_VOXEL_VISUALIZATION_STORAGE.GetChildren()) {
            if(VisualVoxel.Name.ToString().Contains("Grid")) {
                VisualVoxel.QueueFree();
            }
        }

        // Loop Through voxels and apply the effect
        if(_CurrentState) {
            foreach((string _,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
                (Dictionary<string,Vector3> DirectionalDictonary,Dictionary<string,Vector3> DirectionalNormals) = GetVoxelInformation(Voxel);

                foreach((string Direction, Vector3 DirectionalVector) in DirectionalDictonary) {
                    if(PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.ContainsKey(DirectionalVector.ToString())) {
                        DrawVisualizationLine(Voxel.Position,DirectionalNormals[Direction],"Grid",Color.Color8(255,1,1));
                    }
                }
            } 
        }
    }

    // Voxel neigbout lib for purchased land
    public static void VisualizeAffectedVoxelGrid(bool _CurrentState) {
        foreach(MeshInstance3D VisualVoxel in PlacingVoxelGridHandler.NODE_VOXEL_VISUALIZATION_STORAGE.GetChildren()) {
            if(VisualVoxel.Name.ToString().Contains("Purchased") || VisualVoxel.Name.ToString().Contains("StateIndication")) {
                VisualVoxel.QueueFree();
            }
        }

        if(_CurrentState) {
            foreach((string _,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
                (Dictionary<string,Vector3> DirectionalDictonary,Dictionary<string,Vector3> DirectionalNormals) = GetVoxelInformation(Voxel);

                void DrawLines(List<string> Directions) {
                    foreach(string DirectionalName in Directions) {
                        DrawVisualizationLine(Voxel.Position,DirectionalNormals[DirectionalName],"Purchased",Color.Color8(1,255,1));
                    }
                }  

                // Decides The State
                if((bool)Voxel.GetMeta("VoxelIsPurchased") == true) {
                    IndicatedActiveLandPiece(Voxel.Position,"StateIndication");
                    //GD.Print((int)Voxel.GetMeta("VoxelIDType",0));

                    switch((int)Voxel.GetMeta("VoxelIDType",0)) {
                        case 1: DrawLines(new List<string>{"Left"}); break;
                        case 2: DrawLines(new List<string>{"Right"}); break;
                        case 3: DrawLines(new List<string>{"Front"}); break;
                        case 4: DrawLines(new List<string>{"Back"}); break;
                        case 5: DrawLines(new List<string>{"Left","Right"}); break;
                        case 6: DrawLines(new List<string>{"Left","Front"}); break;
                        case 7: DrawLines(new List<string>{"Left","Back"}); break;
                        case 8: DrawLines(new List<string>{"Right","Front"}); break;
                        case 9: DrawLines(new List<string>{"Right","Back"}); break;
                        case 10: DrawLines(new List<string>{"Front","Back"}); break;
                        case 11: DrawLines(new List<string>{"Left","Right","Front"}); break;
                        case 12: DrawLines(new List<string>{"Left","Right","Back"}); break;
                        case 13: DrawLines(new List<string>{"Left","Front","Back"}); break;
                        case 14: DrawLines(new List<string>{"Right","Front","Back"}); break;
                        case 15: DrawLines(new List<string>{"Left","Right","Front","Back"}); break;
                    }
                }
            }
        }
    }

    // Visualizes The Default Placing Grid
    public static void VisualizeNormalGrid(bool _CurrentState) {
        foreach(MeshInstance3D VisualVoxel in PlacingVoxelGridHandler.NODE_VOXEL_VISUALIZATION_STORAGE.GetChildren()) {
            if(VisualVoxel.Name.ToString().Contains("CellGrid")) {
                VisualVoxel.QueueFree();
            }
        }

        if(_CurrentState) {
            // Data shit
            float CurrentXAxis = 0f;
            float CurrentYAxis = 0f;

            float VoxelIncrement = (float)Math.Round(PlacingGridHandler.VOXEL_CLUSTER_SIZE * (float)PlacingGridHandler.VOXEL_CLUSTER_AMOUNT,2);
            float VoxelIncrementOffset = (float)Math.Round(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE,2);

            // Y
            for(float Collum = 0; Collum <  VoxelIncrement + 1; Collum++) {
                DrawVisualizationLine(
                    new Vector3(
                        CurrentXAxis + (VoxelIncrementOffset / 2),
                        PlacingGridHandler.CELL_HEIGHT - 0.01f,
                        VoxelIncrementOffset / 2
                    ),
                    new Vector3(
                        0,
                        0,
                        (VoxelIncrement * PlacingGridHandler.CELL_SIZE) / 1.25f // 1.25 cuz draw line does /1.25
                    ),"CellGrid",Color.Color8(255,255,255)
                );
                CurrentXAxis += PlacingGridHandler.CELL_SIZE;
            }

            // X
            for(float Rows = 0; Rows <  VoxelIncrement + 1; Rows++) {
                DrawVisualizationLine(
                    new Vector3(
                        VoxelIncrementOffset / 2,
                        PlacingGridHandler.CELL_HEIGHT - 0.01f,
                        CurrentYAxis + (VoxelIncrementOffset / 2)
                    ),
                    new Vector3(
                        (VoxelIncrement * PlacingGridHandler.CELL_SIZE) / 1.25f, // 1.25 cuz draw line does /1.25,
                        0,
                        0
                    ),"CellGrid",Color.Color8(255,255,255)
                );
                CurrentYAxis += PlacingGridHandler.CELL_SIZE;
            }
            /*
            // X
            for(float Collum = 0; Collum < VoxelIncrement * 2 + 1; Collum++) {
                CurrentXAxis += GridSystem3D.GridSystemGridSize;
                DrawVisualizationLine(new Vector3(
                    CurrentXAxis + VoxelIncrementOffset,
                    GridSystem3D.VoxelGridHeight - 0.1f
                    ,VoxelIncrementOffset
                )
                ,new Vector3(
                    0,
                    0,
                    VoxelIncrement + (GridSystem3D.GridSystemGridSize * 2.4f)
                ),"CellGrid",Color.Color8(255,255,255));
            }
            
            // Y
            for(float Rows = 0; Rows < VoxelIncrement * 2 + 1; Rows++) {
                CurrentYAxis += GridSystem3D.GridSystemGridSize;
                DrawVisualizationLine(new Vector3(
                    VoxelIncrementOffset,
                    GridSystem3D.VoxelGridHeight - 0.1f,
                    CurrentYAxis + VoxelIncrementOffset - GridSystem3D.GridSystemGridSize
                )
                ,new Vector3(
                    VoxelIncrement - (VoxelIncrementOffset * 2) + (GridSystem3D.GridSystemGridSize * 2.5f)
                    ,
                    0,
                    0
                ),"CellGrid",Color.Color8(255,255,255));
            }*/
        }
    }
    /* Voxel Type Read
        0 > none;

        --------------------------------------------
        ONE TILE OPTIONS
        --------------------------------------------

        1 > Left;
        2 > Right;
        3 > Front;
        4 > Back;

        --------------------------------------------
        TWO TILE OPTIONS
        --------------------------------------------

        5 > Left Right;
        6 > Left Front;
        7 > Left Back;
        8 > Right Front;
        9 > Right Back;
        10 > Front Back

        --------------------------------------------
        THREE TILE OPTIONS
        --------------------------------------------

        11 > Left Right Front;
        12 > Left Right Back;
        13 > Left Front Back;
        14 > Right Front Back;

        --------------------------------------------
        FOUR TILE OPTIONS
        --------------------------------------------

        15 > Left Right Front Back;
    */

    // Defines the neighbout of each voxel
    // should be fired when land purchasing happens
    public static void DefineVoxelNeighbours() {
        foreach((string _,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
            (Dictionary<string,Vector3> DirectionalDictonary,Dictionary<string,Vector3> DirectionalNormals) = GetVoxelInformation(Voxel);
            Dictionary<string,bool> DirectionalVoxelAllowance = new Dictionary<string, bool>();
            Dictionary<string,bool> VoxelNeighbourActivationState = new Dictionary<string, bool>();

            // Check if voxels are valid
            foreach((string Direction, Vector3 DirectionalVector) in DirectionalDictonary) {                
                if(PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.ContainsKey(DirectionalVector.ToString())) {
                    Node3D VoxelNeighbour = PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY[DirectionalVector.ToString()];
                    VoxelNeighbourActivationState.Add(Direction,((bool)VoxelNeighbour.GetMeta("VoxelIsPurchased") == true) ? true : false);

                    DirectionalVoxelAllowance.Add(Direction,true);
                } else {
                    VoxelNeighbourActivationState.Add(Direction,false);
                    DirectionalVoxelAllowance.Add(Direction,false);
                }

            }

            // funny fucking choice library
            // THIS PAINS ME......
            if(VoxelNeighbourActivationState["Left"]) {
                Voxel.SetMeta("VoxelIDType",1);
            } if(VoxelNeighbourActivationState["Right"]) {
                Voxel.SetMeta("VoxelIDType",2);
            } if(VoxelNeighbourActivationState["Front"]) {
                Voxel.SetMeta("VoxelIDType",3);
            } if(VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",4);

            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Right"]) {
                Voxel.SetMeta("VoxelIDType",5);
            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Front"]) {
                Voxel.SetMeta("VoxelIDType",6);
            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",7);
            } if(VoxelNeighbourActivationState["Right"] && VoxelNeighbourActivationState["Front"]) {
                Voxel.SetMeta("VoxelIDType",8);
            } if(VoxelNeighbourActivationState["Right"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",9);
            } if(VoxelNeighbourActivationState["Front"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",10);

            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Right"] && VoxelNeighbourActivationState["Front"]) {
                Voxel.SetMeta("VoxelIDType",11);
            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Right"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",12);
            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Front"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",13);
            } if(VoxelNeighbourActivationState["Right"] && VoxelNeighbourActivationState["Front"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",14);

            } if(VoxelNeighbourActivationState["Left"] && VoxelNeighbourActivationState["Right"] && VoxelNeighbourActivationState["Front"] && VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",15);

            } if(!VoxelNeighbourActivationState["Left"] && !VoxelNeighbourActivationState["Right"] && !VoxelNeighbourActivationState["Front"] && !VoxelNeighbourActivationState["Back"]) {
                Voxel.SetMeta("VoxelIDType",0);
            }
        }

        // update The Fencing
        // Ik this is a wierd place to put it, but micro optimization... Yay!!!!
        PlacingPlotBorder.Update();
    }

    // Gets The Bounds Of The Grids
    public static (float,float,float,float) GetVoxelGridBounds() {
        float VoxelIncrement = (float)Math.Round(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE,2);
        float Max_X = 0f;           float Min_X = 0f;
        float Max_Y = 0f;           float Min_Y = 0f;

        foreach((string VoxelCords,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
            if (((Vector3)Voxel.GetMeta("VoxelPosition")).X > Max_X) {
                Max_X = ((Vector3)Voxel.GetMeta("VoxelPosition")).X;
            }
            if (((Vector3)Voxel.GetMeta("VoxelPosition")).X < Min_X) {
                Min_X = ((Vector3)Voxel.GetMeta("VoxelPosition")).X;
            }

            if (((Vector3)Voxel.GetMeta("VoxelPosition")).Z > Max_Y) {
                Max_Y = ((Vector3)Voxel.GetMeta("VoxelPosition")).Z;
            }
            if (((Vector3)Voxel.GetMeta("VoxelPosition")).Z < Min_Y) {
                Min_Y = ((Vector3)Voxel.GetMeta("VoxelPosition")).Z;
            }
        }

        Max_X += VoxelIncrement / 2;            Min_X += VoxelIncrement / 2;
        Max_Y += VoxelIncrement / 2;            Min_Y += VoxelIncrement / 2;
        return (Max_X,Min_X,Max_Y,Min_Y);
    }

    // Gets The Voxel Where The Object is in
    public static Node3D GetVoxelFromPosition(Vector3 _InstancePosition) {
        float VoxelIncrement = (float)Math.Round(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE,2);

        Vector3 VoxelLizedVector = GridSystem3D.RasterizeObjectPosition(_InstancePosition, new Vector3(0,0,0), 0, VoxelIncrement);
        VoxelLizedVector = new Vector3(VoxelLizedVector.X,PlacingGridHandler.CELL_HEIGHT,VoxelLizedVector.Z);

        if(PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.ContainsKey(VoxelLizedVector.ToString())) {
            Node3D VoxelizedVectorNode = PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY[VoxelLizedVector.ToString()];
            return VoxelizedVectorNode;
        }

        return null;
    }

    // Returns The Boundaries of the voxels
    public static Dictionary<string,float> GetVoxelExtends(Node3D _Voxel) {
        float VoxelIncrement = (float)Math.Round(PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE,2);

        Dictionary<string,float> VoxelExtends = new Dictionary<string, float>{
			["Max_X"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")).X + VoxelIncrement / 2,       ["Max_Y"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")).Z + VoxelIncrement / 2,
            ["Min_X"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")).X - VoxelIncrement / 2,       ["Min_Y"] = ((Vector3)_Voxel.GetMeta("VoxelPosition")).Z - VoxelIncrement / 2,
		};

        return VoxelExtends;
    }

    /*
    // Converts it to a format which the voxel system can read
    public static Vector3 ConvertVoxelPositionToLocalSpace(Vector3 VoxelPosition) {
        (float Max_X, float Min_X, float Max_Y, float Min_Y) = GetVoxelGridBounds();
        Vector3 VoxelGridCenter = new Vector3((Max_X + Min_X) / 2,0,(Max_Y + Min_Y) / 2);

        return new Vector3(VoxelPosition.X - VoxelGridCenter.X,0,VoxelPosition.Z - VoxelGridCenter.Z) + GridSystem3D.GridOffsetWorldSpace;
    }
    */
    /*
    public static void MergeGridToNode3D() {
        Node3D PlacingStorageNode = new Node3D();

        (float Max_X, float Min_X, float Max_Y, float Min_Y) = GetVoxelGridBounds();
        Vector3 VoxelGridCenter = new Vector3((Max_X + Min_X) / 2,0,(Max_Y + Min_Y) / 2);

        PlacingStorageNode.Position = VoxelGridCenter;

        foreach(var Object in PlacingVoxelGridHandler.PlacingSystemStorage.GetChildren()) {
            PlacingVoxelGridHandler.PlacingSystemStorage.RemoveChild(Object);
            PlacingStorageNode.AddChild(Object);
        }
        PlacingVoxelGridHandler.PlacingSystemStorage.QueueFree();

        PlacingVoxelGridHandler.ResourcesNode.AddChild(PlacingStorageNode);
        PlacingStorageNode.Position = GridSystem3D.GridOffsetWorlSpace;
        PlacingStorageNode.Name = "epic";
    }*/

   // Creates A New Voxel Grid
    public static void CreateNewVoxelGrid() {

        /*
        if(VoxelGridRefrenceLink == null) { 
            return;
        }

        // Check if shitty size is correct compared to the grid
        if(VoxelGridRefrenceLink.Scale.X % GridSystem3D.GridSystemGridSize != 0) {
            GD.Print("Supplied Refrence is not rounded to the grid size, Left: " + VoxelGridRefrenceLink.Scale.X % GridSystem3D.GridSystemGridSize);
            return;
        }
        if(VoxelGridRefrenceLink.Scale.Z % GridSystem3D.GridSystemGridSize != 0) {
            GD.Print("Supplied Refrence is not rounded to the grid size, Left: " + VoxelGridRefrenceLink.Scale.X % GridSystem3D.GridSystemGridSize);
            return;
        }*/

        // Creates A Random Name For Each voxel acording to the lowest data possible
        // should be used for loading data
        string AssignVoxelID() {
            // Generates A Random Voxel id
            string GenerateRandomVoxelID() {
                float VoxelAmountCubed = (float)Math.Pow(PlacingGridHandler.VOXEL_CLUSTER_AMOUNT,2);
                float NumericalVoxelDigits = VoxelAmountCubed.ToString().Length;

                RandomNumberGenerator VoxelRandomNumberGenerator = new RandomNumberGenerator();
                string CombinedVoxelIdGenerated = "";

                for(int VoxelDigitIncerement = 0; VoxelDigitIncerement < NumericalVoxelDigits; VoxelDigitIncerement++) {
                    CombinedVoxelIdGenerated += VoxelRandomNumberGenerator.RandiRange(0,9).ToString();
                }

                return CombinedVoxelIdGenerated;
            }

            string VoxelSugestedId = GenerateRandomVoxelID();
            if(PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.ContainsKey(VoxelSugestedId) is false) {
                return VoxelSugestedId;
            } else {
                return AssignVoxelID();
            }
        }
        
        // Load from data first
        // Gets Data Shit
        Godot.Collections.Dictionary<int,Variant> VoxelData = (Godot.Collections.Dictionary<int,Variant>)DataHandler.Get(
	        new System.Collections.Generic.Dictionary<int, string>{
                [0] = "P",
            },
			"V",
            DataHandler.DATA_FILE
        );

        foreach((int _, Variant Data) in VoxelData) {
            Godot.Collections.Dictionary<string,Variant> DataCorrected = (Godot.Collections.Dictionary<string,Variant>)Data;
            
            Node3D Voxel = new Node3D();
            PlacingVoxelGridHandler.NODE_VOXEL_STORAGE.AddChild(Voxel);

            Voxel.Position = new Vector3(
                (float)((Godot.Collections.Array<float>)DataCorrected["P"])[0],
                (float)((Godot.Collections.Array<float>)DataCorrected["P"])[1],
                (float)((Godot.Collections.Array<float>)DataCorrected["P"])[2]
            );
            
            Voxel.Name = Voxel.Position.ToString();

            Voxel.SetMeta("VoxelIsPurchased",(bool)DataCorrected["B"]);
            Voxel.SetMeta("VoxelIDType",(int)DataCorrected["I"]);
            Voxel.SetMeta("VoxelPosition",Voxel.Position);

            PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.Add(Voxel.Position.ToString(),Voxel);
            CreateVoxelGridInstance(Voxel.Position);
        }

        // Does the actual voxiliation
        for(float Collum = 0; Collum < PlacingGridHandler.VOXEL_CLUSTER_AMOUNT; Collum++) {
            for(float Rows = 0; Rows < PlacingGridHandler.VOXEL_CLUSTER_AMOUNT; Rows++) {
                Vector3 VoxelPosition = new Vector3(
                    (float)Math.Round((Collum + 1) * (PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE),2),
                    PlacingGridHandler.CELL_HEIGHT,
                    (float)Math.Round((Rows + 1) * (PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.CELL_SIZE),2)
                );

                if(!PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.ContainsKey(VoxelPosition.ToString())) {
                    Node3D Voxel = new Node3D();

                    PlacingVoxelGridHandler.NODE_VOXEL_STORAGE.AddChild(Voxel);
                    Voxel.Position = VoxelPosition;
                    Voxel.Name = Voxel.Position.ToString();
                    
                    Voxel.SetMeta("VoxelIsPurchased",false);
                    Voxel.SetMeta("VoxelIDType",0f);
                    Voxel.SetMeta("VoxelPosition",Voxel.Position);

                    PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY.Add(Voxel.Position.ToString(),Voxel);

                    CreateVoxelGridInstance(Voxel.Position);
                }
            }
        }

        /*
        // CENTERING GARBAGE

        (float Max_X, float Min_X, float Max_Y, float Min_Y) = GetVoxelGridBounds();
        Vector3 VoxelGridCenter = new Vector3((Max_X + Min_X) / 2,0,(Max_Y + Min_Y) / 2);
        float VoxelIncrement = (float)Math.Round(GridSystem3D.GridSystemClusterSize * GridSystem3D.GridSystemGridSize,2);

        // centers the grid
        foreach((string VoxelCords,Node3D Voxel) in VoxelGridLibrary) {
            Voxel.Position = new Vector3(Voxel.Position.X - VoxelGridCenter.X,0,Voxel.Position.Z - VoxelGridCenter.Z) + GridSystem3D.GridOffsetWorldSpace;
        }

        // CONTINUE HERE CUZ SHIT JUST DOESNT WORK
        // ESPECIALLY THIS LINE UNDER HERE
       
        (Max_X,Min_X,Max_Y,Min_Y) = GetVoxelGridBounds();

        // Positions the bounds correctly
        PlacingVoxelGridHandler.BoundaryNodes.GetChild<Node3D>(0).Position = new Vector3(Max_X,GridSystem3D.GridOffsetWorldSpace.Y,0);
        PlacingVoxelGridHandler.BoundaryNodes.GetChild<Node3D>(1).Position = new Vector3(Min_X,GridSystem3D.GridOffsetWorldSpace.Y,0);
        PlacingVoxelGridHandler.BoundaryNodes.GetChild<Node3D>(2).Position = new Vector3(0,GridSystem3D.GridOffsetWorldSpace.Y,Max_Y);
        PlacingVoxelGridHandler.BoundaryNodes.GetChild<Node3D>(3).Position = new Vector3(0,GridSystem3D.GridOffsetWorldSpace.Y,Min_Y);

        // Setup The RayCastingFloor
        PlacingVoxelGridHandler.VoxelFloorRayCastScanner.Scale = new Vector3(Max_X - Min_X,1,Max_Y - Min_Y);
        PlacingVoxelGridHandler.VoxelFloorRayCastScanner.Position = GridSystem3D.GridOffsetWorldSpace;
        */

        DefineVoxelNeighbours();
    }
}