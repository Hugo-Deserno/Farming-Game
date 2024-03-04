
using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public class DataHandler {
    // Path and such
    public static string SAVE_FILE_PATH {get;set;}
    public static string SAVE_DIRECTORY_PATH {get;set;}
    // Settingsd path
    public static string SETTINGS_DIRECTORY_PATH = Path.Join(ProjectSettings.GlobalizePath("res://"),"Database");
    public static string SETTINGS_FILE_PATH = Path.Join(ProjectSettings.GlobalizePath("res://"),"Database/Settings.json");
    // Shit
    public static bool SAVE_CATCHER = true;
    public static string SAVE_CATCH_REASON {get;set;}
    // File lock in system
    public static int? CURRENT_FILE_KEY {get;set;}
    public static Godot.Collections.Dictionary<string,Variant> DATA_FILE {get;set;} //  All the Stored Data
    public static Godot.Collections.Dictionary<string,Variant> DEFAULT_DATA {get;set;} //  All the Stored Data

    // saves all teh data to teh json
    public static void UploadData() {
        if(SAVE_CATCHER) { // if nothing went wrong with initliazing
            string ConvertedJson = StaticDataHandler.ConvFileToJson(DATA_FILE);

            if(ConvertedJson == "" || ConvertedJson == null) {
                GD.PushWarning("Warning, Couldn't save file, got " + ConvertedJson);
            } else {
                if(SAVE_FILE_PATH != null
                && StaticDataHandler.DirectoryExists(SAVE_DIRECTORY_PATH)
                && File.Exists(SAVE_FILE_PATH)) {

                    File.WriteAllText(SAVE_FILE_PATH,ConvertedJson);
                    GD.Print("Saved Data Succesfully");
                }
            }
        } else {
            GD.PushError(SAVE_CATCH_REASON);
            return;
        }
    }

    // Loads the data onto the DATA_FILE variable
    public static void DownloadData() {
        if(SAVE_CATCHER) {
            if(SAVE_FILE_PATH != null
                && StaticDataHandler.DirectoryExists(SAVE_DIRECTORY_PATH)
                && File.Exists(SAVE_FILE_PATH)) {
                    string JsonText = File.ReadAllText(SAVE_FILE_PATH);

                    if(JsonText == "" || JsonText == null) { // Checks if the file isnt empty
                        SAVE_CATCHER = false;
                        SAVE_CATCH_REASON = "READABLE JSON IS EMPTY";
                        return;
                    } else {
                        Json JsonInstance =  new Json();
                        Error ParsedResult = JsonInstance.Parse(JsonText);

                        if(ParsedResult != Error.Ok) {
                            SAVE_CATCHER = false;
                            SAVE_CATCH_REASON = "PARSE FAILED";
                            GD.PushError("Couldnt parse Json, " + JsonInstance.GetErrorMessage());
                        } else {
                            DATA_FILE = (Godot.Collections.Dictionary<string,Variant>)JsonInstance.Data;
                            GD.Print("Loaded Data Succesfully");
                        }
                    }
                } else {
                    GD.PushError("Error: save File doesn't exist");
                    SAVE_CATCHER = false;
                    SAVE_CATCH_REASON = "File doesn't exist";
                    return;
                }
        } else {
            GD.PushError(SAVE_CATCH_REASON);
            return;
        }
    }

    // P A I N N N N N N N N
    public static void Set(Dictionary<int,string> _DataTreePath, string _ChosenKey, Variant _ChosenValue, Godot.Collections.Dictionary<string,Variant> _DataTree) {
        Godot.Collections.Dictionary<string,Variant> CurrentPath = (Godot.Collections.Dictionary<string,Variant>)_DataTree;

        if(_DataTree == null) {
            GD.PushError("Data hasnt been downloaded yet");
            return;
        }

        for(int Nest = 0; Nest < _DataTreePath.Count; Nest++) {
            if(_DataTreePath.ContainsKey(Nest)) {
                CurrentPath = (Godot.Collections.Dictionary<string,Variant>)((Godot.Collections.Dictionary<string,Variant>)CurrentPath)[_DataTreePath[Nest]];
            } else {
                GD.PushError("Error Data Set, Data Value Doesnt Exist");
            }
        }

        CurrentPath[_ChosenKey] = _ChosenValue;
    }
    // MORE SUFFERING
    // READ THIS DUMBASS... ORDER STARTS FROM 0 NOT 1
    // READ THIS DUMBASS... ORDER STARTS FROM 0 NOT 1
    // READ THIS DUMBASS... ORDER STARTS FROM 0 NOT 1
    // READ THIS DUMBASS... ORDER STARTS FROM 0 NOT 1

    public static Variant? Get(Dictionary<int,string> _DataTreePath, string _ChosenKey, Godot.Collections.Dictionary<string,Variant> _DataTree) {
        Godot.Collections.Dictionary<string,Variant> CurrentPath = (Godot.Collections.Dictionary<string,Variant>)_DataTree;
        
        // Pass in null for the chosenKey to get the dictonary nest
        if(_DataTree == null) {
            GD.PushError("Data hasnt been downloaded yet");
            return null;
        }

        for(int Nest = 0; Nest < _DataTreePath.Count; Nest++) {
            if(_DataTreePath.ContainsKey(Nest) && ((Godot.Collections.Dictionary<string,Variant>)CurrentPath).ContainsKey(_DataTreePath[Nest])) {
                CurrentPath = (Godot.Collections.Dictionary<string,Variant>)((Godot.Collections.Dictionary<string,Variant>)CurrentPath)[_DataTreePath[Nest]];
            } else  {
                CurrentPath = null;
                break;
            }
        }

        if(CurrentPath != null && _ChosenKey != null) {
            if(CurrentPath.ContainsKey(_ChosenKey)) {
                return CurrentPath[_ChosenKey];
            } else {
                return null;
            }
        } else {
            if(_ChosenKey == null) {
                return CurrentPath;
            }
            return null;
        }
    }
}

// Stuff for now
public partial class DataController : DataHandler {
    // Updates the file when new stuff gets added
    public static void PatchJsonFile(int _StructType) {
         // Get The struct patch here
        // change changes made to the struct patch
        string FilePath = (_StructType == 1) ? SAVE_FILE_PATH : SETTINGS_FILE_PATH;

        string JsonText = File.ReadAllText(FilePath);
        
        if(JsonText == "") { //ks i Checf the file isnt empty
            SAVE_CATCHER = false;
            SAVE_CATCH_REASON = "READABLE JSON IS EMPTY";
            GD.PushError("File Is Empty");
            return;
        } else {
            Json JsonInstance =  new Json();
            Error ParsedResult = JsonInstance.Parse(JsonText);

            if(ParsedResult  != Error.Ok) { // check if the parse hasnt failed and if will push a error
                SAVE_CATCHER = false;
                SAVE_CATCH_REASON = "PARSE FAILED";
                GD.PushError("Couldnt parse Json, " + JsonInstance.GetErrorMessage());
                return;
            } else {
                Godot.Collections.Dictionary<string,Variant> ParsedJsonDictonary = (Godot.Collections.Dictionary<string,Variant>)JsonInstance.Data;
                Godot.Collections.Dictionary<string,Variant> NewDataStructPatch = GetCurrentStruct(_StructType);

                Godot.Collections.Dictionary<string,Variant> UpdatedDataPatch = StaticDataHandler.DataPatcher(ParsedJsonDictonary,NewDataStructPatch);
                // Write That Shit Corretly
                string ConvertedJson = StaticDataHandler.ConvFileToJson(UpdatedDataPatch);

                // Pushes The New Entry
                if(ConvertedJson == "") { // checks if the json string is valid
                    SAVE_CATCHER = false;
                    SAVE_CATCH_REASON = "CONVERTED JSON IS EMPTY";
                    GD.PushError("The Data provided to Convert to string is incorrect, got:" + ConvertedJson);
                    return;
                } else {
                    File.WriteAllText(FilePath,ConvertedJson);
                }
            }
        }
    }

    // Creates texte file if it doesnt exist
    public static void GenerateJsonFile(int _StructType) { 
        string DirectoryPath = (_StructType == 1) ? SAVE_DIRECTORY_PATH : SETTINGS_DIRECTORY_PATH;
        string FilePath = (_StructType == 1) ? SAVE_FILE_PATH : SETTINGS_FILE_PATH;

        if(FilePath == null) {
            GD.PushError("DATA_ARCHIVES: Cannot load unkown Json File, Directory: " + SAVE_FILE_PATH);
            return;
        } else {
            // Creates new shit if missing
            if(!StaticDataHandler.DirectoryExists(DirectoryPath)) {
                Directory.CreateDirectory(DirectoryPath);
            }

            // Check if file already exists
            if(!File.Exists(FilePath)) {
                // Creates the data tree
                Godot.Collections.Dictionary<string,Variant> SelectedStruct = GetCurrentStruct(_StructType);

                if(SelectedStruct == null) {
                    return;
                }
                if(_StructType == 1) {
                    // Sets the Defualt data such as voxels and placing
                    if(DEFAULT_DATA != null) {
                        SelectedStruct = DEFAULT_DATA;
                    }
                }
                
                string ConvertedJson = StaticDataHandler.ConvFileToJson(SelectedStruct);

                // Pushes The New Entry
                if(ConvertedJson == "") { // checks if the json string is valid
                    SAVE_CATCHER = false;
                    SAVE_CATCH_REASON = "CONVERTED JSON IS EMPTY";
                    GD.PushError("The Data provided to Convert to string is incorrect, got:" + ConvertedJson);
                    return;
                } else {
                    // Creates THe File
                    FileStream CurrentFile =  File.Create(FilePath);
                    CurrentFile.Close();

                    File.WriteAllText(FilePath,ConvertedJson);
                    return;
                }
            } else {
               PatchJsonFile(_StructType);
            }
            return;
        }
    }

    // Generates The Desired Struct
    public static Godot.Collections.Dictionary<string,Variant> GetCurrentStruct(int _StructType) {
        Godot.Collections.Dictionary<string,Variant> SelectedStruct;  
        DataStruct CompiledDataStruct = new DataStruct();

        switch(_StructType) {
            case 1:
                SelectedStruct = CompiledDataStruct.Data;
                break;
            case 2:
                SelectedStruct = CompiledDataStruct.Settings;
                break;
            default :
                SelectedStruct = null;
                break;
        }

        if(SelectedStruct == null) {
            GD.PushError("StructID is invalid, got " + _StructType);
            return null;
        } else {
            return SelectedStruct;
        }
    }

    // Creates The Key
    public static void SetKey(int _SaveKeyID) {
        CURRENT_FILE_KEY = 1;
        SAVE_FILE_PATH = Path.Join(ProjectSettings.GlobalizePath("res://"),"Database/Archives/" + _SaveKeyID + "_Data_Archive.json");
        SAVE_DIRECTORY_PATH = Path.Join(ProjectSettings.GlobalizePath("res://"),"Database/Archives");
    }

    // Sets the default data
    public static void SetDefaultKey(Godot.Collections.Dictionary<string,Variant> _DefaultData) {
        DEFAULT_DATA = _DefaultData;
    }
}

// For Blank Data
public partial class StaticDataHandler : DataHandler {
    // Checks if the directory exists
    public static bool DirectoryExists(string _CurrentDirectory) {
        if(Directory.Exists(_CurrentDirectory)) {
            return true;
        } else {
            return false;
        }
    }

    // Converts file to json
    public static string ConvFileToJson(Godot.Collections.Dictionary<string,Variant> _DataStructure) {
        /*
            HEY IDIOT ME HERE AGAIN,

            DO NOT, AND I REPEAT, DO NOT USE INDENT
            IT DOUBLES FILE SIZE
            MAY LOOK PRETTY, BUT EATS DATA
        */
        return Json.Stringify(_DataStructure);
    }

    // Adds missing content
    // And removes old content
    public static Godot.Collections.Dictionary<string,Variant> DataPatcher(
        Godot.Collections.Dictionary<string,Variant> _OldPatch,
        Godot.Collections.Dictionary<string,Variant> _NewPatch) {
        Godot.Collections.Dictionary<string,Variant> RemappedPatch = new Godot.Collections.Dictionary<string,Variant>(_NewPatch);

        // Nested Shit
        void NestDataPatch(Variant __Patch, Dictionary<int,string> __DataTree) {
            foreach((string PatchName, Variant PatchChild) in (Godot.Collections.Dictionary<string,Variant>)__Patch) {
                Dictionary<int,string> CurrentDataTreeCopy = new Dictionary<int, string>(__DataTree);

                Variant? CurrentOldPatchedVar = Get(CurrentDataTreeCopy,PatchName,_OldPatch);

                if(CurrentOldPatchedVar != null ) {
                    if(PatchChild.VariantType == Variant.Type.Dictionary && ((Godot.Collections.Dictionary<string,Variant>)PatchChild).Count == 0 || PatchChild.VariantType != Variant.Type.Dictionary) {
                        Set(CurrentDataTreeCopy,PatchName,(Variant)CurrentOldPatchedVar,RemappedPatch);
                    }
                } else {
                    Set(CurrentDataTreeCopy,PatchName,PatchChild,RemappedPatch);
                }

                if(PatchChild.VariantType == Variant.Type.Dictionary) {
                    CurrentDataTreeCopy.Add(CurrentDataTreeCopy.Count,PatchName);

                    NestDataPatch(PatchChild,CurrentDataTreeCopy);
                }
            }
        }
        NestDataPatch(_NewPatch, new Dictionary<int, string>());

        return RemappedPatch;
    }

    /*
    // does a reverse patch check
    // so it should check if data still exists in the new patch
    public static Godot.Collections.Dictionary<string,Variant> InversePatch(        
        Godot.Collections.Dictionary<string,Variant> _OldPatch,
        Godot.Collections.Dictionary<string,Variant> _NewPatch) {

        // so thsi old patch has all the content because it first runs data patcher
        Godot.Collections.Dictionary<string,Variant> RemappedPatch = new Godot.Collections.Dictionary<string,Variant>(_OldPatch);

        // Nested Shit
        void NestDataPatch(Variant __Patch, Dictionary<int,string> __DataTree) {
            foreach((string PatchName, Variant PatchChild) in (Godot.Collections.Dictionary<string,Variant>)__Patch) {
                Dictionary<int,string> CurrentDataTreeCopy = new Dictionary<int, string>(__DataTree);

                Variant? NewPatchdifference = Get(CurrentDataTreeCopy,PatchName,_NewPatch);

                // If patch doesnt exist in old new patch, remove it
                GD.Print(PatchName);
                if(NewPatchdifference == null) {
                    ((Godot.Collections.Dictionary<string,Variant>)Get(CurrentDataTreeCopy,null,_OldPatch)).Remove(PatchName);
                } else {
                    Set(CurrentDataTreeCopy,PatchName,PatchChild,RemappedPatch);

                    // We check and if its exist then nest
                    // ALSooooooo prevents dict loss like voxels and such
                    // so you can use dicts as a data type in the struct
                    // :D
                    if(((Variant)NewPatchdifference).VariantType == Variant.Type.Dictionary && ((Godot.Collections.Dictionary<string,Variant>)NewPatchdifference).Count > 0) {
                        CurrentDataTreeCopy.Add(CurrentDataTreeCopy.Count,PatchName);
                        NestDataPatch(PatchChild,CurrentDataTreeCopy);
                    }
                }
            }
        }
        NestDataPatch(_OldPatch, new Dictionary<int, string>());

        return RemappedPatch;
    }*/
}