using System;
using Godot;

public struct DataStruct {
    public DataStruct() {
        return;
    }

    // List of Notes to read before whining that the saving doesnt work

    /*
        HEY LITTLE GUY,

        please note that this thing is very, VERY sensitive to
        Comma's. So for the love of fucking god, place them there
        Cuz they will fuck everything over
    */

    /*
        ALSOOOOOO

        do note that setting empty dicts will result in it being used as a value and not a path
        and then proceding to ask "why won't my values remove".

        god i hate myself
        spend like 2+ hours figuring this one out,
    */

    // General save data
    public Godot.Collections.Dictionary<string,Variant> Data = new Godot.Collections.Dictionary<string, Variant>{
        ["P"] = new Godot.Collections.Dictionary<string,Variant> { // Placement
            ["V"] = new Godot.Collections.Dictionary<string,Variant> {}, // Voxels
            ["P"] = new Godot.Collections.Dictionary<string,Variant> {}, // Placed objects
        },

        ["I"] = new Godot.Collections.Dictionary<string,Variant> { // Inventory
            // inventory mats
            ["1"] = 0, // Wheat
            ["2"] = 0, // Stone
            ["3"] = 0, // Wood
            ["4"] = 0, // epic
        },

        // We keep item age as and empty cuz im to lazy to write mutiple inventories, cuz it WILL get messy
        ["A"] = new Godot.Collections.Dictionary<string,int> {}, // inventory history
    };

    // main Data
    public Godot.Collections.Dictionary<string,Variant> Settings = new Godot.Collections.Dictionary<string, Variant>{
    };

}