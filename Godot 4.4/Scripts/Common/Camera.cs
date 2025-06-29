using System;
using Godot;

struct CharConfig {
    public float CharSpeedNormal;
    public float CharSpeedSprint;
    public float CharHeight;
}

public partial class Camera : Node3D
{
    private static CharConfig Config = new CharConfig
    {
        CharSpeedNormal = 0.02f,
        CharSpeedSprint = 0.05f,
        CharHeight = 0,
    };

    // Props or whatever the fuck
    private static Vector3 CharDir { get; set; }
    private static CriticallyDampedSpring CritSpring { get; set; }

    // Objects
    private static Node3D Char { get; set; }
    private static Node3D RotateAxel { get; set; }

    // Get Input direction for Character
    private static void GetCharInputDir() {
        Vector3 Dir = Vector3.Zero;
        if (Input.IsActionPressed("game_char_forward"))
            Dir += -Char.Basis.Z;
        if (Input.IsActionPressed("game_char_backward"))
            Dir += Char.Basis.Z;
        if (Input.IsActionPressed("game_char_left"))
            Dir += -Char.Basis.X;
        if (Input.IsActionPressed("game_char_right"))
            Dir += Char.Basis.X;

        if (Dir.Length() > 0)
            Dir = Dir.Normalized();
        CharDir = Dir * Config.CharSpeedNormal;
    }

    private static void UpdateCharPos(double Delta) {
        if (CharDir == Vector3.Zero)
            return;

        CritSpring.SetGoal(Char.Position + CharDir / ((float)Delta));
        Vector3 CharPos = CritSpring.Step(Delta);

        Char.Position = CharPos;
    }

    private static void SetCharPos(Vector3 Pos) {
        Char.Position = Pos;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Char = this;
        RotateAxel = GetNode<Node3D>("./RotateAxel");

        SetCharPos(new Vector3(1, 1, 1));

        CharDir = Vector3.Zero;
        CritSpring = new CriticallyDampedSpring(0.6f, Char.Position);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        GetCharInputDir();
        UpdateCharPos(delta);
    }
}
