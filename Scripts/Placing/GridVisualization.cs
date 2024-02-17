using Godot;
using System;
using System.Collections.Generic;

public class GridVisualization
{
	public Node3D VISUAL_GRID_HOLDER;
	public Dictionary<String,MeshInstance3D> GRID_TILE_STORAGE = new Dictionary<string, MeshInstance3D>();
	// Method Garbage

	// Creates A Grid Tile
	private void CreateGridTile(Vector2 _TilePosition) {
		MeshInstance3D GridTile = new MeshInstance3D();
		PlaneMesh VisibleMesh = new PlaneMesh();
		OrmMaterial3D FillMaterial = new OrmMaterial3D();

		// material Junk
		FillMaterial.Transparency = Godot.BaseMaterial3D.TransparencyEnum.Alpha;
		FillMaterial.AlbedoTexture = GD.Load<Texture2D>(GridHandler.PATH_TO_CELL_FILL_TEXTURE);
		FillMaterial.AlbedoColor = GridHandler.NON_COLLISION_COLOR;
		FillMaterial.Uv1Triplanar = true;
		FillMaterial.Uv1WorldTriplanar = true;
		FillMaterial.Uv1Scale = new Vector3(1 / GridHandler.CELL_SIZE,1 / GridHandler.CELL_SIZE,1 / GridHandler.CELL_SIZE);
		FillMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;

		GridTile.Mesh = VisibleMesh;
		GridTile.MaterialOverride = FillMaterial;

		GridTile.Scale = new Vector3(GridHandler.CELL_SIZE / 2,0.1f,GridHandler.CELL_SIZE / 2);
		GridTile.Position = new Vector3(_TilePosition.X,GridHandler.CELL_HEIGHT,_TilePosition.Y);
		GridTile.Transparency = 1f;
		
		VISUAL_GRID_HOLDER.AddChild(GridTile);
		GridTile.Name = GridTile.Position.ToString();
		
		// Add it to the storage
		GRID_TILE_STORAGE.Add(GridTile.Position.ToString(), GridTile);
	}

	// Modifies The Item Material
	private void ModifyItemMaterial() {
		// We Do this cuz otehrwise its to transparent
		Color PersonalColorCompiler = GridHandler.NON_COLLISION_COLOR;
		PersonalColorCompiler.A8 = 210;

		foreach(object Object in GridSystem3D.CURRENT_ITEM_PREFAB.GetChildren()) {
			var ObjectType = Object.GetType();

			if(ObjectType.GetProperty("MaterialOverride") != null) {
				// collect this one with garbage plz future me
				StandardMaterial3D Duped_Material = (StandardMaterial3D)((StandardMaterial3D)((MeshInstance3D)Object).MaterialOverride).Duplicate();
				((MeshInstance3D)Object).MaterialOverride = Duped_Material;

				Duped_Material.AlbedoColor = PersonalColorCompiler;
				Duped_Material.Emission = PersonalColorCompiler;
				Duped_Material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				Duped_Material.EmissionEnabled = true;
				Duped_Material.EmissionEnergyMultiplier = 0.2f;
			}
		}
	}

	// Updates The Grids Position
	public void Update(Vector3 _ObjectPosition, Vector3 _ObjectSize) { // assuming the position is rasterized
		Vector3 NormalizedSize = GridSystem3D.NormalizeScale(_ObjectSize,GridHandler.CURRENT_ROTATION);

		if(VISUAL_GRID_HOLDER != null) {
			if(GridHandler.CURRENT_ROTATION % 180 == 90f) {
				// this solves the rotation problem apparantelly???
				VISUAL_GRID_HOLDER.Position = new Vector3(_ObjectPosition.X - (NormalizedSize.X / 2),0,_ObjectPosition.Z + (NormalizedSize.Z / 2));
			} else {
				VISUAL_GRID_HOLDER.Position = new Vector3(_ObjectPosition.X - (NormalizedSize.X / 2),0,_ObjectPosition.Z - (NormalizedSize.Z / 2));
			}
			
			VISUAL_GRID_HOLDER.RotationDegrees = new Vector3(0,GridHandler.CURRENT_ROTATION,0);
		} 	

		// We Do this cuz otehrwise its to transparent
		Color PersonalColorCompiler = (!GridHandler.IS_COLLIDING) ? GridHandler.NON_COLLISION_COLOR : GridHandler.COLLISION_COLOR;
		PersonalColorCompiler.A8 = 210;

		// makes the maetrial cahnge collor
		foreach(object Object in GridSystem3D.CURRENT_ITEM_PREFAB.GetChildren()) {
			var ObjectType = Object.GetType();

			if(ObjectType.GetProperty("MaterialOverride") != null) {
				((StandardMaterial3D)((MeshInstance3D)Object).MaterialOverride).AlbedoColor = PersonalColorCompiler;
				((StandardMaterial3D)((MeshInstance3D)Object).MaterialOverride).Emission = PersonalColorCompiler;
			}
		}
	}
 
	// Updates THe Tile Color Depensing on the collision State
	public void CheckCollisions() {
		foreach((string _, MeshInstance3D Tile) in GRID_TILE_STORAGE) { 
			bool CollidingGrid = GridHandler.PLACING_SYSTEM.CollideWithOtherEntities(Tile.GlobalPosition, new Vector3(GridHandler.CELL_SIZE,0.1f,GridHandler.CELL_SIZE),0f);

			if(CollidingGrid) {
				((OrmMaterial3D)Tile.MaterialOverride).AlbedoColor = GridHandler.COLLISION_COLOR;
			} else {
				((OrmMaterial3D)Tile.MaterialOverride).AlbedoColor = GridHandler.NON_COLLISION_COLOR;
			}
		}
	}

	// Destorys teh shit
	public void Remove() {
		if(VISUAL_GRID_HOLDER != null) {
			VISUAL_GRID_HOLDER.QueueFree();
			GRID_TILE_STORAGE = new Dictionary<string, MeshInstance3D>();
		}
	}

	// Sets The grid up
	public GridVisualization(string _InstanceName, Vector3 _ObjectSize) {
		VISUAL_GRID_HOLDER = new Node3D();
		VISUAL_GRID_HOLDER.Name = _InstanceName;
		GridHandler.NODE_VISUAL_STORAGE.AddChild(VISUAL_GRID_HOLDER);

		int TileAmountX = (int)Math.Round(_ObjectSize.X / GridHandler.CELL_SIZE,0);
		int TileAmountZ = (int)Math.Round(_ObjectSize.Z / GridHandler.CELL_SIZE,0);

		for(var XRows = 0; XRows < TileAmountX; XRows++) {
			for(var ZRows = 0; ZRows < TileAmountZ; ZRows++) {
				CreateGridTile(new Vector2(XRows * GridHandler.CELL_SIZE + (GridHandler.CELL_SIZE / 2),ZRows * GridHandler.CELL_SIZE + (GridHandler.CELL_SIZE / 2)));
			}
		}

		ModifyItemMaterial();
	}
}
