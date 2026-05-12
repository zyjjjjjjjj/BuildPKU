using Godot;
using System;

public partial class CameraCtrl : Camera2D
{
	[Export] private float _moveSpeed = 600f;
	public float MoveSpeed => _moveSpeed / Zoom.X;
	[Export] private float _dragSpeed = 400f;
	public float DragSpeed => _dragSpeed / Zoom.X;
	// 边缘移动的触发范围，单位为屏幕宽高的百分比
	[Export] private float _edgeRatio = 0.1f;
	[Export] private float _zoomFactor = 1.1f;
	[Export] private float _minZoom = 0.5f;
	[Export] private float _maxZoom = 3.0f;

	private enum Mode
	{
		Edit,
		Normal,
	}
	private Mode _mode = Mode.Normal;
	private Vector2 _leftTop;
	private Vector2 _rightBottom;
	
	private bool _isMiddleDragging;
	private Vector2 _lastMousePos;

	[Signal]
	public delegate void CameraZoomEventHandler();

	public override void _Ready()
	{
		MakeCurrent();
		BuildingMgr.Instance.EnterEditorMode += () =>
		{
			_mode = Mode.Edit;
		};
		BuildingMgr.Instance.ExitEditorMode += () =>
		{
			_mode = Mode.Normal;
		};
		_leftTop = GridMgr.Instance.GlobalPosition;
		_rightBottom = _leftTop + GridMgr.Instance.GridSize*GridMgr.Instance.CellSize;
	}

	public override void _Process(double delta)
	{
		switch (_mode)
		{
			case Mode.Edit:
				HandleMove_Edit(delta);
				HandleMove_Normal(delta);
				break;
			case Mode.Normal:
				HandleMove_Edit(delta);
				HandleMove_Normal(delta);
				break;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (!mouseEvent.Pressed)
			return;

		if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
		{
			ZoomByFactor(_zoomFactor);
		}
		else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
		{
			ZoomByFactor(1f / _zoomFactor);
		}
	}

	private void HandleMove_Edit(double delta)
	{
		if(!MouseInEdge())
			return;
		Vector2 mousePos = GetViewport().GetMousePosition();
		var screenCenter = GetViewportRect().Size / 2;
		Vector2 direction = (mousePos - screenCenter).Normalized();
		Position += direction * MoveSpeed * (float)delta;
		ClampCameraPosition();
	}

	private bool MouseInEdge()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 edgeSize = viewportSize * _edgeRatio;
		if (mousePos.X <= edgeSize.X || mousePos.X >= viewportSize.X - edgeSize.X)
		{
			return true;
		}
		if (mousePos.Y <= edgeSize.Y || mousePos.Y >= viewportSize.Y - edgeSize.Y)
		{
			return true;
		}
		return false;
	}
	

	private void HandleMove_Normal(double delta)
	{
		bool middlePressed = Input.IsMouseButtonPressed(MouseButton.Middle);
		if (!middlePressed)
		{
			_isMiddleDragging = false;
			return;
		}

		Vector2 currentMousePos = GetViewport().GetMousePosition();
		if (!_isMiddleDragging)
		{
			// 刚开始按下这一帧，只记录位置，不移动相机
			_isMiddleDragging = true;
			_lastMousePos = currentMousePos;
			return;
		}

		Vector2 mouseDelta = currentMousePos - _lastMousePos;

		Position -= mouseDelta * DragSpeed * (float)delta;

		_lastMousePos = currentMousePos;

		ClampCameraPosition();
	}

	private void ClampCameraPosition()
	{
		Position = new Vector2(
			Mathf.Clamp(Position.X, _leftTop.X, _rightBottom.X),
			Mathf.Clamp(Position.Y, _leftTop.Y, _rightBottom.Y)
		);
	}

	private void ZoomByFactor(float factor)
	{
		float newZoom = Mathf.Clamp(Zoom.X * factor, _minZoom, _maxZoom);
		Zoom = new Vector2(newZoom, newZoom);

		EmitSignal(SignalName.CameraZoom);
	}
}
