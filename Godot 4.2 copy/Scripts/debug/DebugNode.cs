using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

public partial class DebugNode : Node
{

    public override void _Ready()
    {
        PlacingGridHandler.PLACING_SYSTEM = new GridSystem3D("Lantern");
    }

    bool VoxelVisualization = false;
    bool VoxelBoundariesVisualiztaion = false;
    bool VoxeGridVisualiztaion = false;
    public override void _Process(double delta)
    {
        if(Input.IsActionJustPressed("Debug_Placing") && !PlacingGridHandler.IS_PLACING && GetViewport().GuiGetFocusOwner() == null) {
            PlacingGridHandler.PLACING_SYSTEM.Enable();    
        }
        if(Input.IsActionJustPressed("Debug_Plant")) {
            MeshInstance3D obj = DemolitionTool.RayCastPlacedObject(new Godot.Collections.Array<Rid>(),"ItemCollision");

        	Node3D SelectedNode = obj != null ? obj.GetParent<Node3D>() : null;
            if(obj == null) {
                return;
            }

            Node metanode = obj.GetParent<Node3D>().FindChild("ItemMeta",true,false);
            if ((int)metanode.GetMeta("Id",0) == 2) {
                ulong? ObjectID = CropController.BindCropToObject("Wheat",SelectedNode);
                CropController.StartCropCycle(ObjectID);
                /*
                CreateNewCropInstance Plant = new CreateNewCropInstance("Wheat",SelectedNode);
                //Plant.OverrideData(2,5);
                Plant.InstantiateCycle();*/
            }
        }
        if(Input.IsActionJustPressed("Placing_Place")) {
            MeshInstance3D obj = DemolitionTool.RayCastPlacedObject(new Godot.Collections.Array<Rid>(),"ItemCollision");

        	Node3D SelectedNode = obj != null ? obj.GetParent<Node3D>() : null;
            if(obj == null) {
                return;
            }

            Node metanode = obj.GetParent<Node3D>().FindChild("ItemMeta",true,false);
            if ((int)metanode.GetMeta("Id",0) == 2) {
                if((bool)CropController.HasBind(SelectedNode.GetInstanceId())) {
                    CropController.HarvestCrop(SelectedNode.GetInstanceId());
                }
            }
        }

        /*
        if(Input.IsActionJustPressed("Debug_Grid_01")) {
            VoxelVisualization = !VoxelVisualization ? true: false;
            VoxelBoundariesVisualiztaion = false;
            VoxelGrid.VisualizeAffectedVoxelGrid(false);
		    VoxelGrid.VisualizeVoxelGrid(VoxelVisualization);
        } else if(Input.IsActionJustPressed("Debug_Grid_02")) {
            VoxelBoundariesVisualiztaion = !VoxelBoundariesVisualiztaion ? true: false;
            VoxelVisualization = false;
            VoxelGrid.VisualizeVoxelGrid(false);
		    VoxelGrid.VisualizeAffectedVoxelGrid(VoxelBoundariesVisualiztaion);
        } else if(Input.IsActionJustPressed("Debug_Grid_03")) {
            VoxeGridVisualiztaion = !VoxeGridVisualiztaion ? true : false;
            VoxelGrid.VisualizeNormalGrid(VoxeGridVisualiztaion);
        }*/

        // Togles the purchase state
        if(Input.IsActionJustPressed("Debug_Voxel_Tag")) {
            Vector3 Pos = PlacingGridHandler.RayCastFromCursor(new Godot.Collections.Array<Godot.Rid>{},"FloorColision");

            float magclose = 10000000;
            Node3D vox = null;
            foreach((string VoxelCords,Node3D Voxel) in PlacingVoxelGridHandler.VOXEL_GRID_LIBRARY) {
                Vector3 diff = Pos - Voxel.Position;

                float mag = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
                if(mag < magclose) {
                    magclose = mag;
                    vox = Voxel;
                }
            }

            if(vox != null) {
                vox.SetMeta("VoxelIsPurchased",(bool)vox.GetMeta("VoxelIsPurchased",false) ? false : true);
                GD.Print((bool)vox.GetMeta("VoxelIsPurchased",false));

                VoxelGrid.DefineVoxelNeighbours();
                if(VoxelBoundariesVisualiztaion) {
                    VoxelGrid.VisualizeAffectedVoxelGrid(true);
                }
            }
        }
   
    }
}
