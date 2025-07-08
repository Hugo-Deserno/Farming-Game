using System;
using System.Runtime.Intrinsics.X86;
using Godot;

struct CharConfig {
    public float CharSpeedNormal;
    public float CharSpeedSprint;
    public float CharHeight;
    public float CameraSensitivity;
}

public partial class Camera : Node3D
{
    // Metaaaaaaa ahhh
    private static float CameraSpeed { get; set; }
    private static float CameraHeight { get; set; }
    private static float CameraPitch { get; set; }
    private static float CameraSensitivity { get; set; }

    // Props or whatever the fuck
    private static bool IsRunning { get; set; }

    private static Vector3 CharDir { get; set; }
    private static Vector3 RotateDir { get; set; }

    private static Vector2 PreDragMousePos { get; set; }
    private static bool PrevIsRotateDragPressed { get; set; }
    private static bool IsRotateDragPressed { get; set; }

    private static CriticallyDampedSpring PosSpring { get; set; }

    // Objects
    private static Node3D Char { get; set; }
    private static Node3D RotateAxel { get; set; }
    private static Node3D Head { get; set; }

    // Get Input direction for Character
    private static void GetCharInputDir() {
        Vector3 Dir = Vector3.Zero;
        if (Input.IsActionPressed("game_char_forward"))
            Dir += -RotateAxel.Basis.Z;
        if (Input.IsActionPressed("game_char_backward"))
            Dir += RotateAxel.Basis.Z;
        if (Input.IsActionPressed("game_char_left"))
            Dir += -RotateAxel.Basis.X;
        if (Input.IsActionPressed("game_char_right"))
            Dir += RotateAxel.Basis.X;

        if (Dir.Length() > 0)
            Dir = Dir.Normalized();
        CharDir = Dir * (IsRunning ? CameraSpeed * 3 : CameraSpeed);
    }

    private static void UpdateCharPos(double Delta) {
        if (CharDir == Vector3.Zero)
            return;

        PosSpring.SetGoal(Char.Position + CharDir * 100);
        Vector3 CharPos = PosSpring.Step(Delta);
        CharPos.Y = CameraHeight;

        Char.Position = CharPos;
    }

    private static void UpdateCharRot(double Delta) {
        if (RotateDir == Vector3.Zero)
            return;

        Vector3 Vel = Char.Rotation.Lerp(Char.Rotation + RotateDir * 100, (float)Delta);
        RotateAxel.RotateY(Vel.Y * CameraSensitivity * -1);
        Head.Rotation = new Vector3(Util.BasicUtilities.Rad(CameraPitch), 0, 0);
        RotateDir = Vector3.Zero;
    }

    private static void SetCharPos(Vector3 Pos, Vector3? Rot) {
        Char.Position = Pos;
        if (Rot != null) {
            Vector3 _Rot = (Vector3)Rot;
            Char.Rotation = new Vector3(-60, _Rot.Y, _Rot.Z);
            Head.Rotation = new Vector3(Util.BasicUtilities.Rad(CameraPitch), 0, 0);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // trigger for detect wqhen drag starts
        // we save mouse pos on start to load
        // when drag ends
        if (@event is InputEventMouseButton MouseButtonClick) {
            if (MouseButtonClick.ButtonIndex == MouseButton.Right) {
                if (!IsRotateDragPressed && MouseButtonClick.Pressed)
                    PreDragMousePos = MouseButtonClick.Position;
                IsRotateDragPressed = MouseButtonClick.Pressed;
            }
        }

        Vector2 MouseDelta = Vector2.Zero;
        if (@event is InputEventMouseMotion MouseMotion)
            MouseDelta = MouseMotion.Relative;

        Input.MouseMode = IsRotateDragPressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        if (PrevIsRotateDragPressed  && !IsRotateDragPressed)
            GetViewport().WarpMouse(PreDragMousePos);
        PrevIsRotateDragPressed = IsRotateDragPressed;

        if (IsRotateDragPressed)
            RotateDir = new Vector3(0, MouseDelta.X, 0);
        else
            RotateDir = Vector3.Zero;
        base._Input(@event);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        CameraSpeed = (float)GetMeta("CameraSpeed");
        CameraHeight = (float)GetMeta("CameraHeight");
        CameraPitch = (float)GetMeta("CameraPitch");
        CameraSensitivity = (float)GetMeta("Sensitivity");

        GetCharInputDir();
        UpdateCharPos(delta);
        UpdateCharRot(delta);
        IsRunning = Input.IsActionPressed("game_char_sprint");
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Char = this;
        Head = GetNode<Node3D>("./RotateAxel/Head");
        RotateAxel = GetNode<Node3D>("./RotateAxel");

        CameraSpeed = (float)GetMeta("CameraSpeed");
        CameraHeight = (float)GetMeta("CameraHeight");
        CameraPitch = (float)GetMeta("CameraPitch");
        CameraSensitivity = (float)GetMeta("Sensitivity");

        SetCharPos(new Vector3(1, CameraHeight, 1), null);

        IsRunning = false;
        PrevIsRotateDragPressed = false;
        CharDir = Vector3.Zero;
        RotateDir = Vector3.Zero;
        PreDragMousePos = Vector2.Zero;
        PosSpring = new CriticallyDampedSpring(0.6f, Char.Position);
    }

}
