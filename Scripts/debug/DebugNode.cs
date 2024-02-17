using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DebugNode : Node
{

    public override void _Ready()
    {
        GridHandler.PLACING_SYSTEM = new GridSystem3D("Lantern");
    }

    bool VoxelVisualization = false;
    bool VoxelBoundariesVisualiztaion = false;
    bool VoxeGridVisualiztaion = false;
    public override void _Process(double delta)
    {
        if(Input.IsActionJustPressed("Debug_Placing") && !GridHandler.IS_PLACING && GetViewport().GuiGetFocusOwner() == null) {
            GridHandler.PLACING_SYSTEM.Enable();    
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
            Vector3 Pos = GridHandler.RayCastFromCursor(new Godot.Collections.Array<Godot.Rid>{},"FloorColision");

            float magclose = 10000000;
            Node3D vox = null;
            foreach((string VoxelCords,Node3D Voxel) in VoxelGridHandler.VOXEL_GRID_LIBRARY) {
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
