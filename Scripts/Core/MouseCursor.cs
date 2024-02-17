using Godot;
using System;

// Does Mouse Cursor Stuff
public partial class MouseCursor : Control
{
	private Vector2 LastMousePosition = Vector2.Zero;

	// Input Shit
    public override void _Input(InputEvent _InputEvent) {
        if(_InputEvent is InputEventMouseMotion MouseMotionEvent && Input.MouseMode != Input.MouseModeEnum.Captured) {
			// Stops it from jittering to the center for the singel ass frame
			if(MouseMotionEvent.Position == GetViewport().GetVisibleRect().Size / 2) {
				return;
			}

			this.Position = MouseMotionEvent.Position - this.Size / 2 + new Vector2(0,this.Size.Y / 2);
			LastMousePosition = this.Position;
		}
    }
}
