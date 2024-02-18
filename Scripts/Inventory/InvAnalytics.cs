using System;
using System.Collections.Generic;
using Godot;

// Do a bunch of data tracking and crap
public partial class InvAnalytics : DataRepository {
    // Public shit
    public static Dictionary<string,int> PLAYER_DATA = new Dictionary<string, int>();
    public static Dictionary<string,int> PLAYER_HISTORY_DATA = new Dictionary<string, int>(); // just for me, 0 means none so DONT assign items a 0 history value when it has more then 0 amount

    /// Decyrpt a number into data type
    // do i love data ............... :]
    public static string DecryptDataEntry(Variant _DataEntry) {
        string StringifiedDataEntry = (string)_DataEntry;
        
        int FormatedInteger = Int32.Parse(StringifiedDataEntry);
        FormatedInteger = (int)Convert.ToInt32(FormatedInteger); //  dk if this is needed, but im to lazy to remove it or try it out

        // Checks if it is
        if(!INVENTORY_ENCRYPTION_KEY.ContainsKey(FormatedInteger)) {
            GD.PushWarning("Decryption Error: Inventory Data Type " + _DataEntry + " Doesn't exist");
            return "";
        } else {
            return INVENTORY_ENCRYPTION_KEY[FormatedInteger];
        }
    }

    // Returns the number datatfied
    public static int? EncryptDataEntry(Variant _DataEntry) {
        string StringifiedDataEntry = (string)_DataEntry;
            
        // Loop through all thye shit
        foreach((int EntryCount, string ItemName) in INVENTORY_ENCRYPTION_KEY) {
            if(ItemName == StringifiedDataEntry) {
                return EntryCount;
            }
        }

        GD.PushError("Encryption Error: Inventory Data Type " + _DataEntry + " Doesn't exist");
        return null; // return null cuz writing random data could be dangerous
    }

    // Writes a updated data value to the json
    // with the dumb ass get function which i cant wrap my head around
    public static void WriteBufferData(string _TypeName, int _Amount, int _History) {
        int? Int32DataEntry = EncryptDataEntry(_TypeName);

        if(Int32DataEntry == null) {
            return;
        } else {
            int ConvertNullifiedInt = (int)Int32DataEntry;
            DataHandler.Set(
                new System.Collections.Generic.Dictionary<int, string>{
                [0] = "I", 
                }, // emptyt dict cuz we fecthing a non nested member
                ConvertNullifiedInt.ToString(),
                _Amount,
                DataHandler.DATA_FILE
            );

            // Set history
            DataHandler.Set(
                new System.Collections.Generic.Dictionary<int, string>{
                [0] = "A", 
                }, // emptyt dict cuz we fecthing a non nested member
                ConvertNullifiedInt.ToString(),
                _History,
                DataHandler.DATA_FILE
            );

            PLAYER_DATA[_TypeName] = _Amount;
            PLAYER_HISTORY_DATA[_TypeName] = _History;
        }
    }

    // loads all the garbage
    public static void BufferInventoryData() {
        // Get Inv Data
        // Still cant wrap my head around this dumpster fire, but it works iggggg :|
        Godot.Collections.Dictionary<string,Variant> InmportedJsonData  = (Godot.Collections.Dictionary<string,Variant>)DataHandler.Get(
	        new System.Collections.Generic.Dictionary<int, string>{}, // emptyt dict cuz we fecthing a non nested member
			"I",
            DataHandler.DATA_FILE
        );

        Godot.Collections.Dictionary<string,int> ImportedItemHistory  = (Godot.Collections.Dictionary<string,int>)DataHandler.Get(
	        new System.Collections.Generic.Dictionary<int, string>{}, // emptyt dict cuz we fecthing a non nested member
			"A",
            DataHandler.DATA_FILE
        );

        // We Port that shit over
        foreach((string TypeName, Variant IntValue) in InmportedJsonData) {
            string DecryptedData = DecryptDataEntry(TypeName);
            PLAYER_DATA.Add(DecryptedData,(int)IntValue);

            // Item history
            if(ImportedItemHistory.ContainsKey(TypeName)) {
                PLAYER_HISTORY_DATA.Add(DecryptedData,ImportedItemHistory[TypeName]);
            } else {
                PLAYER_HISTORY_DATA.Add(DecryptedData,0); // 0 is nothing, simple ass that
                // So for the love of god DO not fucking assign a item history value of zero
            }
        }

        // Check if item still exists
        // otherwise delete data
        foreach((string TypeName, Variant _) in ImportedItemHistory) {
            string DecryptedData = DecryptDataEntry(TypeName);
            
            if(DecryptedData == "") {
                GD.PushWarning("Solving lost memory");
                // Ik its not the most safe thing
                // if shit hits the fan, look here
                ((Godot.Collections.Dictionary<string,int>)DataHandler.DATA_FILE["A"]).Remove(TypeName);
            }
        }
    }
}