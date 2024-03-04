using Godot;
using System;

public partial class Fps : RichTextLabel
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Text = "Fps " + Engine.GetFramesPerSecond();

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
			GetNode<RichTextLabel>("../VoxelDisplay").Text = "Current Voxel: " + vox.GetMeta("VoxelPosition");
			GetNode<RichTextLabel>("../VoxelType").Text = "Voxel Type: " + vox.GetMeta("VoxelIDType");
		} else {
			GetNode<RichTextLabel>("../VoxelDisplay").Text = "Current Voxel: ";
			GetNode<RichTextLabel>("../VoxelType").Text = "Voxel Type: ";
		}
	}
}
