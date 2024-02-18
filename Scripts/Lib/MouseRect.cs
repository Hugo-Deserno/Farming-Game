using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using Godot;

// So quick expo
// Godot mouse enter and leave function didnt work how i wanted it to
// So i wrote my own one

public class MouseRect {
    // Garbage
    public Vector2 O_POSITION {get; set;}
    public Vector2 O_SIZE {get; set;}
    //public Vector4 O_ANCHOR {get; set;} // IK vec4's arent ment for this but i was to lazy to use a array

    // Privates
    private bool State = false;
    private Control Object {get; set;} // Object link

    // Event garbage
    private delegate void CompoundFunction();
    CompoundFunction CompoundConnections;

    // Updates This Garbage
    private void Set() {
        // Updates the properties
        void UpdateRect() {
            O_POSITION = Object.GlobalPosition;
            O_SIZE = Object.Size;
        }
        CompoundConnections += UpdateRect;
        Object.ItemRectChanged += () => CompoundConnections(); // fires when the position or size changes
    }

    /*
    // update that stuff
    // also can pass null in for it to not update
    public void Set(Vector2? _Position, Vector2? _Size, Vector4? _Anchor) {
        O_POSITION = _Position != null ? (Vector2)_Position : O_POSITION;
        O_SIZE = _Size != null ? (Vector2)_Size : O_SIZE;
        O_ANCHOR = _Anchor != null ? (Vector4)_Anchor : O_ANCHOR;
    }*/

    // Constructor
    public MouseRect(Variant _UiObject) {
        // Creates Everything
        object UiObject = _UiObject.Obj;

        if(UiObject.GetType().GetProperty("GlobalPosition") != null) {
            Object = (Control)_UiObject;

            O_POSITION = Object.GlobalPosition;
            O_SIZE = Object.Size;

            Set();
        } else {
            Type ObjectType = UiObject.GetType();
            GD.PushError("Cannot Create mouserect for object type" + ObjectType);
        }
    }

    // Disconnects this shit
    public void Disconnect() {
        CompoundConnections = null;
    }

    // Gets the state
    public bool Get() {
        return State;
    }

    // Does cool stuff
    // Very descriptive IK
    public void Step(Vector2 _Position) { // mouse position
        // Boundaries
        float B_L;
        float B_R;
        float B_T;
        float B_B;

        B_L = O_POSITION.X;
        B_T = O_POSITION.Y;

        // first check
        // Only doing half of it because its more preformant
        // Ik i just needed it a excuse cuz it looked bad for it to be just 1 if statement
        // yay me with bad practises
        if(_Position.X < B_L 
        || _Position.Y < B_T) {
            State = false;
        } else {
            B_R = B_L + O_SIZE.X;
            B_B = B_T + O_SIZE.Y;

            // Do the second check
            if(_Position.X > B_R
            || _Position.Y > B_B) {
                State = false;
            } else {
                State = true;
            }
        }
    }
}