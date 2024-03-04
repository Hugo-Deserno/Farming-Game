using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;

/*
    DONT FORGET

    please fucking apply the meta tags before whining that shit doesnt work
*/

/* 
    Hey me again

    if you want to use a class statically
    look in the class cache
    all the classes are stored in there

    thank you very much future me
*/

public class Old {
    // pubs
    public int AnchorStage {get; set;}
    public int AnchorDuration {get; set;}

    // look at me finally using my big brain
    // Stores the classes so the can be accesed by static methods
    public static Dictionary<Node3D,Old> ClassCache = new Dictionary<Node3D, Old>();
    public static Dictionary<Node3D,string> DataCache = new Dictionary<Node3D, string>();

    public int CurrentStage = 1; // cuzz we start from 1
    public int CurrentTime = 0;
    
    public string CropName {get; set;}
    public bool BlockAcess = false;

    public Node3D ParentObject {get; set;}
    public Node MetaNode {get; set;} // this exists cuz its prob bad habit to call findchild a million times
    public Node3D CropStage {get; set;}

    // Construter
    public Old(string _CropName, Node3D _ParentObject) {
         // Check if there already a crop
        if(DataCache.ContainsKey(_ParentObject) || _CropName == "") {
            BlockAcess = true;
            return;
        }

        CropName = _CropName;
        ParentObject = _ParentObject;
        MetaNode = _ParentObject.FindChild("ItemMeta",true,false);
    
        (int? A, int? B) = FetchCropData(); //  A : Stages, B : Duration
        if(A == null || B == null) {
            BlockAcess = true;
            return;
        }

        AnchorStage = (int)A;

        AnchorDuration = (int)B;

        MetaNode.SetMeta("PlantType",_CropName);
        MetaNode.SetMeta("PlantStage",1);
        MetaNode.SetMeta("TimeRemaining",0);
    
        // Write to the caches cuz im incopetent to make good oop class
        ClassCache.Add(_ParentObject,this); // write the class to the cache so it can be called statically
        DataCache.Add(_ParentObject,_CropName);
    }

    // Does the main loop
    public async void InstantiateCycle() {
        // Denies acess
        if(BlockAcess) {
            return;
        }

        // Loads The Model of the desired stage
        bool ApplyStagePrefab(int __StageIndex) {
            PackedScene StageModel = LoadStagePrefab(__StageIndex);

            if(StageModel == null) {
                return true;
            } else {
                // Removes the old Stage
                if(CropStage != null) {
                    CropStage.QueueFree();
                }
            
                CropStage = StageModel.Instantiate<Node3D>();

                ParentObject.AddChild(CropStage);
                CropStage.Name = "CropInstance";
                CropStage.Position = new Vector3(0,0,0);

                return false;
            }
        }

        GD.Print(CurrentStage);
        if(ApplyStagePrefab(CurrentStage)) {
            return;
        }

        float StageSlice = AnchorDuration / (AnchorStage - 1);
        int NonConnected = (int)Math.Round(CurrentTime % StageSlice); // helper for overwriting shit
        
        // Main Cycle
        while (CurrentStage < AnchorStage) {
            await Task.Delay(1000);
            CurrentTime++;
            NonConnected++;
            
            MetaNode.SetMeta("TimeRemaining",CurrentTime);
            // If a cycle passes then increment
            if(NonConnected / StageSlice >= 1.0) {
                NonConnected = 0;
                CurrentStage++;

                MetaNode.SetMeta("PlantStage",CurrentStage);
                // check if the stage exist and apply it
                if(ApplyStagePrefab(CurrentStage)) {
                    CurrentStage = AnchorStage;
                    return;
                }
            }
        }
    }

    // Harvests The Dedicated crop
    public static void HarvestCrop(Node3D _ParentObject) {
        if(!ClassCache.ContainsKey(_ParentObject)) {
            GD.PushWarning("Error: Object Doesnt have a crop");
        }

        Old CachedClass = ClassCache[_ParentObject];

        if((int)CachedClass.MetaNode.GetMeta("PlantStage",0) == CachedClass.AnchorStage) {
            CachedClass.CropStage.QueueFree();
            DataCache.Remove(_ParentObject);

            // Gets inventory item
            (int? ItemAmount, int? ItemHistory) = InventoryData.GetItemInformationFromDatabase((string)CachedClass.MetaNode.GetMeta("PlantType",""));
            
            if(ItemAmount != null && ItemHistory != null) {
                // Adds to inventory
                InventoryData.AddItemToDataBase((string)CachedClass.MetaNode.GetMeta("PlantType",""),((int)ItemAmount) + 1,null,true,false);     
            }

            // Reset meta
            CachedClass.MetaNode.SetMeta("PlantStage",0);
            CachedClass.MetaNode.SetMeta("PlantType","");
            CachedClass.MetaNode.SetMeta("TimeRemaining",0);
        }
        ClassCache.Remove(_ParentObject);
    }

    // Overrides data which leads to skipping parts of the cycle
    public void OverrideData(int _CurrentStage, int _TimeRemaining) {
        // Denies acess
        if(BlockAcess) {
            return;
        }

        CurrentTime = _TimeRemaining;
        CurrentStage = _CurrentStage;

        MetaNode.SetMeta("PlantStage",_CurrentStage);
        MetaNode.SetMeta("TimeRemaining",_TimeRemaining);
    }

    // gather the required data for each crop
    private (int?,int?) FetchCropData() {
        if(!((Godot.Collections.Dictionary<string,Variant>)DataStruct.GeneralStruct["P"]).ContainsKey(CropName)) {
            GD.PushWarning("Error crop cannot be found : " + CropName);
            return (null,null);
        }
        Godot.Collections.Dictionary<string,Variant> CropDataInstance = (Godot.Collections.Dictionary<string,Variant>)((Godot.Collections.Dictionary<string,Variant>)DataStruct.GeneralStruct["P"])[CropName];

        // Check if the crop exist in the registry
        if(CropDataInstance == null) {
            GD.PushWarning("Error crop cannot be found : " + CropName);
            return (null,null);
        } else {
            int Stages = (int)Int32.Parse(CropDataInstance["S"].ToString());
            int Duration = (int)Int32.Parse(CropDataInstance["D"].ToString());

            return (Stages,Duration);
        }
    }

    // loads a certain stage model
    private PackedScene LoadStagePrefab(int _StageIndex) {
        // check if that shit is alive
		if(!File.Exists(ProjectSettings.GlobalizePath("res://") + "Packages/Crops/" + CropName + "/Stage_0" + _StageIndex + ".tscn")) {
            GD.PushWarning("Plant Model Doesnt eixst :" + CropName + "+" + _StageIndex);
            return null;
        } else {
            PackedScene StagePrefab = GD.Load<PackedScene>("res://Packages/Crops/" + CropName + "/Stage_0" + _StageIndex + ".tscn");
            return StagePrefab;
        }
    }
}

