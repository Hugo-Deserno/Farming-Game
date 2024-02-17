using System;
using Godot;

// So quick expo
// Godot mouse enter and leave function didnt work how iu wanted it to
// So i wrote my own one

public class MouseRect {
    // Garbage
    public Vector2 O_POSITION {get; set;}
    public Vector2 O_SIZE {get; set;}
    public Vector4 O_ANCHOR {get; set;} // IK vec4's arent ment for this but i was to lazy to use a array

    // Constructor
    public MouseRect(Vector2 _Position, Vector2 _Size, Vector4 _Anchor) {
        O_POSITION = _Position;
        O_SIZE = _Size;
        O_ANCHOR = _Anchor;
    }

    // update that stuff
    // also can pass null in for it to not update
    public void Set(Vector2? _Position, Vector2? _Size, Vector4? _Anchor) {
        O_POSITION = _Position != null ? (Vector2)_Position : O_POSITION;
        O_SIZE = _Size != null ? (Vector2)_Size : O_SIZE;
        O_ANCHOR = _Anchor != null ? (Vector4)_Anchor : O_ANCHOR;
    }

    // Gets the state
    public bool Get() {
        return true;
    }

    // Does cool stuff
    // Very descriptive IK
    public void Step(Vector2 _Position) {
        // Calculate the actual position by subtracting the anchor position from the actual position

        // use that plus the size and mouse pos to get a answer
    }
}