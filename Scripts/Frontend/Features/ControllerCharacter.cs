using Godot;
using System;
using System.Collections.Generic;

public partial class ControllerCharacter : CharacterBody3D
{
	// Store important trash in here
	static Dictionary<string,object> ControllerInterpolationStorage = new Dictionary<string, object>{
		["PreviousMouseDragDeltaFrame"] = 0f,
		["PreviousCameraControllerDeltaFrame"] = 0f,
		["PreviousCameraZoomFieldOfView"] = 0f,
		["CharacterAccelerationFactor"] = 0f,

		["PreviousMovementControllerDeltaFrame"] = Vector3.Zero,
		["PreviousCharacterMovementFocusDeltaFrame"] = Vector3.Zero,
		["PreciousCharacterMovementMainDeltaFrame"] = Vector3.Zero,
	};
	// interp delta junk
	static Dictionary<string,float> ControllerDeltaTimeInterpolation = new Dictionary<string, float>{
		["CameraControllerDeltaTick"] = 20f,
		["CameraZoomControllerDeltaTick"] = 10f,
		["MovementControllerDeltaTick"] = 10f,
		["MovementControllerFocusDeltaTick"] = 10f,
		["MovementControllerAccelerationDeltaTick"] = 10f,
	};

	//Other stuff
	float RawFieldOfViewScroll = 0f;
	// Mouse Drag Shit
	bool IsMouseButtonDrag = false;
    bool IsMouseButtonDragTimer = false;
    Vector2 PreviousMousePosition = Vector2.Zero;
    // Focus jargle
    static bool CharacterCameraIsEnabled = true;
    static Vector3 CharacterVector3FocusCords;
	static Vector3 ConstructedMainVector3Movement = Vector3.Zero;
	static Vector3 lastPosition = Vector3.Zero;

	// Lerps A Number
	private static float Lerp(float A, float B, float T) {
		return A + (B - A) * T;
	}
	// Converts Degrees To Radians
	private static float Rad(float A) {
		return A * (float)Math.PI / 180;
	}

	// CameraFocus Junk

	// Updates The Tick Time
	private static void UpdateCharacterTickFocusTime(Vector3 _CameraFocusCords) {
		Vector3 DistanceFromPoint = ((Vector3)ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"]) - _CameraFocusCords;
		float DistanceFromFocusPointMagnitude = (float)Math.Sqrt(
			(float)Math.Pow(DistanceFromPoint.X,2) +
			(float)Math.Pow(DistanceFromPoint.Y,2) +
			(float)Math.Pow(DistanceFromPoint.Z,2)
			);

		ControllerDeltaTimeInterpolation["MovementControllerFocusDeltaTick"] = DistanceFromFocusPointMagnitude / 10;
	}
	// Focuses The Camera On One Point
    public static void CharacterCameraFocus(Vector3 _CameraFocusCords) {
        CharacterCameraIsEnabled = false;
        CharacterVector3FocusCords = _CameraFocusCords;
		UpdateCharacterTickFocusTime(_CameraFocusCords);
    }
    // Updates The Camera Focus
    public static void CharacterCameraFocusUpdate(Vector3 _CameraFocusCords) {
        if(!CharacterCameraIsEnabled) {
            CharacterVector3FocusCords = _CameraFocusCords;
			UpdateCharacterTickFocusTime(_CameraFocusCords);
        }
    }
    // Disconnects The Focus
    public static void CharacterCameraFocusDisconnect() {
        CharacterCameraIsEnabled = true;
        CharacterVector3FocusCords = Vector3.Zero;
    }

	// Sets the camera to a focused Position
	public static void SetCharacterCameraToPoint(Vector3 CameraFocusCord) {
		ConstructedMainVector3Movement = CameraFocusCord;
		ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"] = CameraFocusCord;
		lastPosition = CameraFocusCord;
	}

	// main controller dump

	// Updates The Character Controller
	private void UpdateCharacterCameraController(double _DeltaTime) {
		// Gather our shit
 		float CharacterCameraSensitivity = (float)GetMeta("CameraSensitivity",3f);
        float SetCameraXAxisAngle = (float)GetMeta("CameraXAngle",-45f);
        Node3D CameraRootNode = GetNode<Node3D>("CameraRoot");

		/*
		Vector3 ConstructorThetaDrag = new Vector3(
			Rad(Input.GetVector("Movement_Backwards","Movement_Forwards","Movement_Right","Movement_Left").X) *- 1,
			0,
			Rad(Input.GetVector("Movement_Backwards","Movement_Forwards","Movement_Right","Movement_Left").Y)
		)  * 10;
		Vector3 ConstructedRotationVector = new Vector3(
			Rad(SetCameraXAxisAngle),
			(float)ControllerInterpolationStorage["PreviousMouseDragDeltaFrame"],
			0f
		);*/

		ControllerInterpolationStorage["PreviousCameraControllerDeltaFrame"] = Lerp(
			(float)ControllerInterpolationStorage["PreviousCameraControllerDeltaFrame"],
			(float)ControllerInterpolationStorage["PreviousMouseDragDeltaFrame"],
			(float)_DeltaTime * ControllerDeltaTimeInterpolation["CameraControllerDeltaTick"]);
		ControllerInterpolationStorage["PreviousMouseDragDeltaFrame"] = 0f;

		// Gather Nescesary stuff and compile it into one big vector and then apply it to different parts
		RotateY((float)ControllerInterpolationStorage["PreviousCameraControllerDeltaFrame"] * CharacterCameraSensitivity *- 1);
        CameraRootNode.Rotation = new Vector3(Rad(SetCameraXAxisAngle),CameraRootNode.Rotation.Y,CameraRootNode.Rotation.Z);
	}

	private void UpdateCharacterMovementController(double _DeltaTime) {
		// Clamps The Camera In Pre Created Bounds So It Cant Fly fo to nowhere
        Vector3 ClampCharacterCameraToBounds(Vector3 __WorldSpaceVectorCordinated) {
            Godot.Collections.Array<Node> CameraLockBounds = GetNode<Node>("/root/Workspace/Game/ImportantResources/CameraBounds").GetChildren(false);
            Dictionary<string,float> CameraBoundPositions = new Dictionary<string,float>{
                ["ClampBoundMaxX"] = 0f, ["ClampBoundMinX"] = 1000000000f,
                ["ClampBoundMaxY"] = 0f, ["ClampBoundMinY"] = 1000000000f,
            };

            foreach (Node3D CamerBoundNode in CameraLockBounds) {
                CameraBoundPositions["ClampBoundMaxX"] = (CamerBoundNode.Position.X > CameraBoundPositions["ClampBoundMaxX"]) ? CamerBoundNode.Position.X : CameraBoundPositions["ClampBoundMaxX"];
                CameraBoundPositions["ClampBoundMinX"] = (CamerBoundNode.Position.X < CameraBoundPositions["ClampBoundMinX"]) ? CamerBoundNode.Position.X : CameraBoundPositions["ClampBoundMinX"];
                CameraBoundPositions["ClampBoundMaxY"] = (CamerBoundNode.Position.Z > CameraBoundPositions["ClampBoundMaxY"]) ? CamerBoundNode.Position.Z : CameraBoundPositions["ClampBoundMaxY"];
                CameraBoundPositions["ClampBoundMinY"] = (CamerBoundNode.Position.Z < CameraBoundPositions["ClampBoundMinY"]) ? CamerBoundNode.Position.Z : CameraBoundPositions["ClampBoundMinY"];
            }

            __WorldSpaceVectorCordinated = new Vector3(
                Math.Clamp(__WorldSpaceVectorCordinated.X,CameraBoundPositions["ClampBoundMinX"],CameraBoundPositions["ClampBoundMaxX"]),
                __WorldSpaceVectorCordinated.Y,
                Math.Clamp(__WorldSpaceVectorCordinated.Z,CameraBoundPositions["ClampBoundMinY"],CameraBoundPositions["ClampBoundMaxY"])
            );
            return __WorldSpaceVectorCordinated;
        }

		// Makes The "Sprinting" work
        void CharacterAccelerationHandler() {
            float DesiredInterpolation = Input.IsActionPressed("Movement_Accelerate") ? 3f : 1f;
            ControllerInterpolationStorage["CharacterAccelerationFactor"] = Lerp(
				(float)ControllerInterpolationStorage["CharacterAccelerationFactor"],
				DesiredInterpolation,
				(float)_DeltaTime * ControllerDeltaTimeInterpolation["MovementControllerAccelerationDeltaTick"]
			);
        }

		// Get Required Stuff
		Vector2 PlayerCaptureCardResult = Input.GetVector("Movement_Left","Movement_Right","Movement_Forwards","Movement_Backwards");
        Vector3 NormalizedPlayerDirection = (Basis * new Vector3(PlayerCaptureCardResult.X,0,PlayerCaptureCardResult.Y)).Normalized();
        float CharacterCameraSpeedAmplifier = (float)GetMeta("CameraSpeed",4f);
		CharacterAccelerationHandler();

		// when player si typing in textbox
		if(GetViewport().GuiGetFocusOwner() != null) {
			NormalizedPlayerDirection = Vector3.Zero;
		}

		Vector3 ConstructedCameraVelocityVector = new Vector3(
            NormalizedPlayerDirection.X * CharacterCameraSpeedAmplifier * (float)ControllerInterpolationStorage["CharacterAccelerationFactor"],
            0,
            NormalizedPlayerDirection.Z * CharacterCameraSpeedAmplifier * (float)ControllerInterpolationStorage["CharacterAccelerationFactor"]
        );

		ControllerInterpolationStorage["PreviousMovementControllerDeltaFrame"] = new Vector3(
			Lerp(((Vector3)ControllerInterpolationStorage["PreviousMovementControllerDeltaFrame"]).X, ConstructedCameraVelocityVector.X, (float)_DeltaTime * ControllerDeltaTimeInterpolation["MovementControllerDeltaTick"]),
			Lerp(((Vector3)ControllerInterpolationStorage["PreviousMovementControllerDeltaFrame"]).Y, ConstructedCameraVelocityVector.Y, (float)_DeltaTime * ControllerDeltaTimeInterpolation["MovementControllerDeltaTick"]),
			Lerp(((Vector3)ControllerInterpolationStorage["PreviousMovementControllerDeltaFrame"]).Z, ConstructedCameraVelocityVector.Z, (float)_DeltaTime * ControllerDeltaTimeInterpolation["MovementControllerDeltaTick"])
		);

		ConstructedMainVector3Movement = ClampCharacterCameraToBounds(lastPosition + (Vector3)ControllerInterpolationStorage["PreviousMovementControllerDeltaFrame"] / 7.5f);
		ControllerInterpolationStorage["PreviousCharacterMovementFocusDeltaFrame"] = lastPosition;
	}

	// Makes The Camera Focus
	private void UpdateControllerMovementFocus(double DeltaTime) {
        ControllerInterpolationStorage["PreviousCharacterMovementFocusDeltaFrame"] = new Vector3(
            Lerp(((Vector3)ControllerInterpolationStorage["PreviousCharacterMovementFocusDeltaFrame"]).X,CharacterVector3FocusCords.X,(float)DeltaTime * (float)ControllerDeltaTimeInterpolation["MovementControllerFocusDeltaTick"]),
            Lerp(((Vector3)ControllerInterpolationStorage["PreviousCharacterMovementFocusDeltaFrame"]).Y,CharacterVector3FocusCords.Y,(float)DeltaTime * (float)ControllerDeltaTimeInterpolation["MovementControllerFocusDeltaTick"]),
            Lerp(((Vector3)ControllerInterpolationStorage["PreviousCharacterMovementFocusDeltaFrame"]).Z,CharacterVector3FocusCords.Z,(float)DeltaTime * (float)ControllerDeltaTimeInterpolation["MovementControllerFocusDeltaTick"])
        );

		ControllerInterpolationStorage["PreviousMovementControllerDeltaFrame"] = Vector3.Zero;
		ConstructedMainVector3Movement = (Vector3)ControllerInterpolationStorage["PreviousCharacterMovementFocusDeltaFrame"];
	}

	// Actually Applies all the of the fucking garbage
	private void UpdateControllerMovementMain(double DeltaTime) {
				//GD.Print((Vector3)ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"],ConstructedMainVector3Movement);

		// Tbh this kinda has no use but still
		ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"] = new Vector3(
			Lerp(((Vector3)ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"]).X, ConstructedMainVector3Movement.X, (float)DeltaTime * ControllerDeltaTimeInterpolation["MovementControllerDeltaTick"]),
			5f,
			Lerp(((Vector3)ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"]).Z, ConstructedMainVector3Movement.Z, (float)DeltaTime * ControllerDeltaTimeInterpolation["MovementControllerDeltaTick"])
		);

		Position = (Vector3)ControllerInterpolationStorage["PreciousCharacterMovementMainDeltaFrame"];
		lastPosition = Position;
	}

	// Scroll Stuff
	    // handle The Camera Zooming
    private void CharacterCameraZoom(double _DeltaTime) {
        /*
        void DecentralizedScroll() {
            float NormalizedXOffset = RawUnfilterdMousePosition.X / (GetViewport().GetVisibleRect().Size.X / 2) - 1;
            float NormalizedYOffset = RawUnfilterdMousePosition.Y / (GetViewport().GetVisibleRect().Size.Y / 2) - 1;

            float ScrollingNormalized = 1 + Math.Abs(RawFieldOfViewScroll / 45);
            Vector3 Diff = test - GetNode<Camera3D>("CameraRoot/Camera").ProjectPosition(RawUnfilterdMousePosition,1f);

            GD.Print(Diff);
            //DeCentralizedCameraOffset = 
        }
		*/

		// For Scrolling
		if(Inventory.IS_ON_MAIN_UI_ELEMENT) {
			return;
		}

		// Input Related stuff
        if(Input.IsActionJustReleased("Movement_Zoom_Down")) {
            RawFieldOfViewScroll = Math.Clamp(RawFieldOfViewScroll + 3,-45,0);
        }
        else if(Input.IsActionJustReleased("Movement_Zoom_Up")) {
            RawFieldOfViewScroll = Math.Clamp(RawFieldOfViewScroll - 3,-45,0);
        }
        float SetCameraFieldOfView = (float)GetMeta("CameraFielOfView",70f);

        ControllerInterpolationStorage["PreviousCameraZoomFieldOfView"] = Lerp(
			(float)ControllerInterpolationStorage["PreviousCameraZoomFieldOfView"],
			SetCameraFieldOfView + RawFieldOfViewScroll,
			(float)_DeltaTime * ControllerDeltaTimeInterpolation["CameraZoomControllerDeltaTick"]
		);

        GetNode<Camera3D>("CameraRoot/Camera").Fov = (float)ControllerInterpolationStorage["PreviousCameraZoomFieldOfView"];
    }
	
	// Godot provided funcs

 	// Handles The Camera Dragging
    public override void _Input(InputEvent _event) {
        Vector2 RawCameraDragDelta = Vector2.Zero;

        // Mouse Click Detectiuon
        if(_event is InputEventMouseButton MouseButtonClick) {
            if(MouseButtonClick.ButtonIndex == MouseButton.Right) {
                if(!IsMouseButtonDrag && MouseButtonClick.Pressed) {
                    PreviousMousePosition = MouseButtonClick.Position;
                }
                
                IsMouseButtonDrag = MouseButtonClick.Pressed ? true : false;
            }
        }
        // Mouse Drag Detectiom
        if(_event is InputEventMouseMotion InputMouseMotion) {
            RawCameraDragDelta = InputMouseMotion.Relative;
        }

        // and a bunch of useless logic stuff
        Input.MouseMode = IsMouseButtonDrag ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        if(IsMouseButtonDragTimer && !IsMouseButtonDrag) {
            GetViewport().WarpMouse(PreviousMousePosition);
        }
        IsMouseButtonDragTimer = IsMouseButtonDrag;

        if(IsMouseButtonDrag) {
            ControllerInterpolationStorage["PreviousMouseDragDeltaFrame"] = RawCameraDragDelta.X / 1000;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
		UpdateCharacterCameraController(delta);
		CharacterCameraZoom(delta);

		if(CharacterCameraIsEnabled) {
			UpdateCharacterMovementController(delta);
		} else {
			UpdateControllerMovementFocus(delta);
		}
		UpdateControllerMovementMain(delta);
	}

	// Startup Junk
    public override void _Ready() {
        // Set the camera position to the center
		SetCharacterCameraToPoint(new Vector3(
			(PlacingGridHandler.CELL_SIZE * PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.VOXEL_CLUSTER_AMOUNT - (PlacingGridHandler.CELL_SIZE * PlacingGridHandler.VOXEL_CLUSTER_SIZE)) / 2,
			0,
			(PlacingGridHandler.CELL_SIZE * PlacingGridHandler.VOXEL_CLUSTER_SIZE * PlacingGridHandler.VOXEL_CLUSTER_AMOUNT + (PlacingGridHandler.CELL_SIZE * PlacingGridHandler.VOXEL_CLUSTER_SIZE)) / 2
		));
    }
}