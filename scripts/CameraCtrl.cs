using Godot;
using System;

public partial class CameraCtrl : Camera2D
{
	[Export] public float MoveSpeed = 600f;

	// 边缘移动的触发范围，单位为屏幕宽高的百分比
	[Export] public float EdgeRatio = 0.1f;

	[Export] public float ZoomSpeed = 0.1f;
	[Export] public float MinZoom = 0.5f;
	[Export] public float MaxZoom = 3.0f;
	[Signal]
	public delegate void CameraZoomEventHandler();

	public override void _Ready()
	{
		MakeCurrent();
	}

	public override void _Process(double delta)
	{
		HandleEdgeMove(delta);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (!mouseEvent.Pressed)
			return;

		if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
		{
			ZoomIn();
		}
		else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
		{
			ZoomOut();
		}
	}

	private void HandleEdgeMove(double delta)
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 edgeSize = viewportSize * EdgeRatio;
		Vector2 direction = Vector2.Zero;

		if (mousePos.X <= edgeSize.X)
		{
			direction.X -= 1;
		}
		else if (mousePos.X >= viewportSize.X - edgeSize.X)
		{
			direction.X += 1;
		}

		if (mousePos.Y <= edgeSize.Y)
		{
			direction.Y -= 1;
		}
		else if (mousePos.Y >= viewportSize.Y - edgeSize.Y)
		{
			direction.Y += 1;
		}

		if (direction == Vector2.Zero)
			return;

		direction = direction.Normalized();

		// Zoom.X 越大，画面越远，所以移动速度也乘 Zoom.X，体感更稳定
		Position += direction * MoveSpeed * 1000 * (float)delta / Zoom.X;
	}

	private void ZoomOut()
	{
		float newZoom = Mathf.Max(MinZoom, Zoom.X - ZoomSpeed);
		Zoom = new Vector2(newZoom, newZoom);
	}

	private void ZoomIn()
	{
		float newZoom = Mathf.Min(MaxZoom, Zoom.X + ZoomSpeed);
		Zoom = new Vector2(newZoom, newZoom);
	}
}
