using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class CropController : Node {
    private static Node NodeCropControl {get; set;}

    // Holes i still need to get behind
    // > how tf are we linking the placveable with the plant
    // > im dumb help

    // update -> we link it vwith the object ID
    // anyways bind the crop to the plot of land    
    public static ulong? BindCropToObject(string  _CropName, Node3D _ParentObject) {
        // generate the link ID
        ulong ObjectID = _ParentObject.GetInstanceId();
        (int? Stage, int? Time) = FetchCropData(_CropName);

        // Block this shit, die ,die ,die
        if(Stage == null || Time == null) {
            return null;
        } else {
            // generate the collection here
            Node PlantCollection = new Node();
            Node MetaNode = _ParentObject.FindChild("ItemMeta",true,false);
      
            NodeCropControl.AddChild(PlantCollection);
            PlantCollection.SetMeta("ObjectID",ObjectID);
            
            MetaNode.SetMeta("PlantType",_CropName);
            MetaNode.SetMeta("PlantStage",1);
            MetaNode.SetMeta("TimeRemaining",0);
        }

         return ObjectID; // the ID is ussed like a acess key to the crop
    }

    // Fetches the node from the objectID
    public static Node GetBind(ulong? _ObjectID) {
        if (_ObjectID == null) {return null;}
        Node MatchResult = null;

        // Loops through the shit
        foreach(Node CropNodeExtry in NodeCropControl.GetChildren()) {
            if ((ulong)CropNodeExtry.GetMeta("ObjectID",0) == _ObjectID) {
                MatchResult = CropNodeExtry; 
                break;
            }
        }

        if(MatchResult == null) {
            GD.PushWarning("Error: Crop ID Link Dopesnt exist");
        }
        return MatchResult;
    }

    // checks if a bind exists
    public static bool? HasBind(ulong? _ObjectID) {
        if (_ObjectID == null) {return null;}

        // Loops through the shit
        foreach(Node CropNodeExtry in NodeCropControl.GetChildren()) {
            if ((ulong)CropNodeExtry.GetMeta("ObjectID",0) == _ObjectID) {
                return true;
            }
        }
        return false;
    }

    // Handles Idle Time
    // basically
    // compiles the time the app has been inactive and applies it to the time
    public static void StaticIdleController(ulong? _ObjectID) {
        Node3D ObjectNode = GetObjectFromLink(_ObjectID);
        Node MetaBindNode = ObjectNode.FindChild("ItemMeta",true,false);

        // Get the unix epoch and other crap
        int Tick = (int)MetaBindNode.GetMeta("Date",0);
        int CropTime = (int)MetaBindNode.GetMeta("TimeRemaining",0);
        string CropName = (string)MetaBindNode.GetMeta("PlantType","");

        // Breaks so it doesnt fuck the data over
        if(CropTime == 0) {return;}

        // Concurrent Epoch
        TimeSpan TickEpoch = DateTime.UtcNow - new DateTime(2024,1,1); // Get 2024 cuz its less bytes
        int NowTick = (int)TickEpoch.TotalSeconds;

        // Get The Difference
        int TickDifference = NowTick - Tick;
        int CompiledTime = CropTime + TickDifference;

        (int? Stage, int? Time) = FetchCropData(CropName);
        int StageConst = (int)Stage;
        int TimeConst = (int)Time;

        // calculate the stage and cap the time
        CompiledTime = CompiledTime > TimeConst ? TimeConst : CompiledTime;
        float StageSlice = TimeConst / StageConst;
        int RoundedSlice = (int)Math.Round(CompiledTime / StageSlice);

        // Apply the shit
        MetaBindNode.SetMeta("TimeRemaining",CompiledTime);
        MetaBindNode.SetMeta("PlantStage",RoundedSlice);
    }

    // Starts the growing
    public async static void StartCropCycle(ulong? _ObjectID) {
        if (_ObjectID == null) {return;}

        // load the afk data
        StaticIdleController(_ObjectID);
        
        Node BindNode = GetBind(_ObjectID);
        Node3D ObjectNode = GetObjectFromLink(_ObjectID);
        Node MetaBindNode = ObjectNode.FindChild("ItemMeta",true,false);

        string CropName = (string)MetaBindNode.GetMeta("PlantType","");
        int CropStage = (int)MetaBindNode.GetMeta("PlantStage",0);
        int CropTime = (int)MetaBindNode.GetMeta("TimeRemaining",0);

        (int? Stage, int? Time) = FetchCropData(CropName);
        int StageConst = (int)Stage;
        int TimeConst = (int)Time;

        // Updates the crop model on the plot land
        bool ApplyStagePrefab(int __StageIndex) {
            PackedScene StageModel = LoadStagePrefab(__StageIndex,CropName);

            if(StageModel == null) {
                GD.PushWarning("Crop Error, Model index " + __StageIndex + " Doesn't exist");
                return true;
            } else {
                // Remove the old stage
                if(BindNode.GetChildren().Count > 0) {
                    foreach(Node ObjectEntry in BindNode.GetChildren()) {
                        ObjectEntry.QueueFree();
                    }
                }
            
                Node3D CropStage = StageModel.Instantiate<Node3D>();

                BindNode.AddChild(CropStage);
                CropStage.Name = "CropInstance";
                CropStage.Position = ObjectNode.GlobalPosition;

                return false;
            }
        }

        if(ApplyStagePrefab(CropStage)) {return;}

        float StageSlice = TimeConst / (StageConst - 1);
        int NonConnected = (int)Math.Round(CropTime % StageSlice); // helper for overwriting shit
        
        // Main Cycle
        while (CropStage < StageConst) {
            await Task.Delay(1000);
            CropTime++;
            NonConnected++;
            
            // Gets the Tick also update this every second or we gettin wonky results
            TimeSpan TickEpoch = DateTime.UtcNow - new DateTime(2024,1,1); // Get 2024 cuz its less bytes
            int Tick = (int)TickEpoch.TotalSeconds;
            MetaBindNode.SetMeta("Date",Tick);

            MetaBindNode.SetMeta("TimeRemaining",CropTime);
            // If a cycle passes then increment
            if(NonConnected / StageSlice >= 1.0) {
                NonConnected = 0;
                CropStage++;

                MetaBindNode.SetMeta("PlantStage",CropStage);

                // check if the stage exist and apply it
                // if its true (so it doesnt exist) we break out of the loop
                if(ApplyStagePrefab(CropStage)) {
                    CropStage = StageConst;
                    return;
                }
            }
        }
    }

    // Pre loads data for crops
    public static void OverrideCropData(int _CurrentStage, int _TimeRemaining, ulong? _ObjectID) {
        if (_ObjectID == null) {return;}

        Node3D ObjectNode = GetObjectFromLink(_ObjectID);
        Node MetaBindNode = ObjectNode.FindChild("ItemMeta",true,false);

        MetaBindNode.SetMeta("PlantStage",_CurrentStage);
        MetaBindNode.SetMeta("TimeRemaining",_TimeRemaining);
    }

    public static void HarvestCrop(ulong? _ObjectID) {
        if (_ObjectID == null) {return;}

        Node BindNode = GetBind(_ObjectID);
        Node3D ObjectNode = GetObjectFromLink(_ObjectID);
        Node MetaBindNode = ObjectNode.FindChild("ItemMeta",true,false);

        string CropName = (string)MetaBindNode.GetMeta("PlantType","");
        int CropStage = (int)MetaBindNode.GetMeta("PlantStage",0);
        int CropTime = (int)MetaBindNode.GetMeta("TimeRemaining",0);

        (int? Stage, int? Time) = FetchCropData(CropName);

        if(Stage != null && CropStage == (int)Stage) {
            BindNode.QueueFree();

             // Gets inventory item
            (int? ItemAmount, int? ItemHistory) = InventoryData.GetItemInformationFromDatabase(CropName);
            if(ItemAmount != null && ItemHistory != null) {
                // Adds to inventory
                InventoryData.AddItemToDataBase(CropName,((int)ItemAmount) + 1,null,true,false);     
            }

            MetaBindNode.SetMeta("PlantType","");
            MetaBindNode.SetMeta("PlantStage",0);
            MetaBindNode.SetMeta("TimeRemaining",0);
        } else {
            GD.PushWarning("Error, crop hasnt grown yet");
            return;
        }
    }

    // Fecth crap bewlo here
    
    // gather the required data for each crop
    private static (int?,int?) FetchCropData(string _CropName) {
        if(!((Godot.Collections.Dictionary<string,Variant>)DataStruct.GeneralStruct["P"]).ContainsKey(_CropName)) {
            GD.PushWarning("Error crop cannot be found : " + _CropName);
            return (null,null);
        }
        Godot.Collections.Dictionary<string,Variant> CropDataInstance = (Godot.Collections.Dictionary<string,Variant>)((Godot.Collections.Dictionary<string,Variant>)DataStruct.GeneralStruct["P"])[_CropName];

        // Check if the crop exist in the registry
        if(CropDataInstance == null) {
            GD.PushWarning("Error crop cannot be found : " + _CropName);
            return (null,null);
        } else {
            int Stages = (int)Int32.Parse(CropDataInstance["S"].ToString());
            int Duration = (int)Int32.Parse(CropDataInstance["D"].ToString());

            return (Stages,Duration);
        }
    }

        // loads a certain stage model
    private static PackedScene LoadStagePrefab(int _StageIndex, string _CropName) {
        // check if that shit is alive
		if(!File.Exists(ProjectSettings.GlobalizePath("res://") + "Packages/Crops/" + _CropName + "/Stage_0" + _StageIndex + ".tscn")) {
            GD.PushWarning("Plant Model Doesnt eixst :" + _CropName + "+" + _StageIndex);
            return null;
        } else {
            PackedScene StagePrefab = GD.Load<PackedScene>("res://Packages/Crops/" + _CropName + "/Stage_0" + _StageIndex + ".tscn");
            return StagePrefab;
        }
    }

    // Gets the object from a ID
    private static Node3D GetObjectFromLink(ulong? _ObjectID) {
        if (_ObjectID == null) {return null;}

        GodotObject ObjectLinkObject = InstanceFromId((ulong)_ObjectID);
        
        // Do a quick check if it reall        
        if(ObjectLinkObject == null) {
            GD.PushWarning("Warning Object ID " + _ObjectID + " Doesnt exist");
            return null;
        } else {
            return (Node3D)ObjectLinkObject;
        }
    }

    // Main calls

    public override void _Ready() {
        NodeCropControl = this;
    }
}