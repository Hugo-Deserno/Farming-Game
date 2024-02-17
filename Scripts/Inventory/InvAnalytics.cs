using System;
using System.Collections.Generic;
using Godot;

// Do a bunch of data tracking and crap
public partial class InvAnalytics : DataRepository {
    // Public shit
    public static Dictionary<string,int> PLAYER_DATA = new Dictionary<string, int>();

    /// Decyrpt a number into data type
    // do i love data ............... :]
    public static string DecryptDataEntry(Variant _DataEntry) {
        string StringifiedDataEntry = (string)_DataEntry;
        
        int FormatedInteger = Int32.Parse(StringifiedDataEntry);
        FormatedInteger = (int)Convert.ToInt32(FormatedInteger); //  dk if this is needed, but im to lazy to remove it or try it out

        // Checks if it is
        if(!INVENTORY_ENCRYPTION_KEY.ContainsKey(FormatedInteger)) {
            GD.PushError("Decryption Error: Inventory Data Type " + _DataEntry + " Doesn't exist");
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
    public static void WriteBufferData(string TypeName, int Amount) {
        int? Int32DataEntry = EncryptDataEntry(TypeName);

        if(Int32DataEntry == null) {
            return;
        } else {
            int ConvertNullifiedInt = (int)Int32DataEntry;
            DataHandler.Set(
                new System.Collections.Generic.Dictionary<int, string>{
                [0] = "I", 
                }, // emptyt dict cuz we fecthing a non nested member
                ConvertNullifiedInt.ToString(),
                Amount,
                DataHandler.DATA_FILE
            );

            PLAYER_DATA[TypeName] = Amount;
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

        // We Port that shit over
        foreach((string TypeName, Variant IntValue) in InmportedJsonData) {
            PLAYER_DATA.Add(DecryptDataEntry(TypeName),(int)IntValue);
        }
    }
}