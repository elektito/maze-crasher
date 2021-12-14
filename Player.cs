using Godot;
using System;

public class Player : KinematicBody2D
{
    private Vector2 _velocity = Vector2.Zero;

    private Camera2D _camera;
    private Light2D _lightMain;
    private Light2D _lightAux;
    private Light2D _lightPlayer;
    private Label _posLabel;
    private Label _distLabel;
    private Line2D _dirIndicator;

    public Cell TargetCell { get; set; } = null;

    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("camera");
        _lightMain = GetNode<Light2D>("light_main");
        _lightAux = GetNode<Light2D>("light_aux");
        _lightPlayer = GetNode<Light2D>("light_player");
        _posLabel = GetNode<Label>("pos_label");
        _distLabel = GetNode<Label>("distance_label");
        _dirIndicator = GetNode<Line2D>("direction_indicator");
    }

    public override void _PhysicsProcess(float delta)
    {
        var dir = Vector2.Zero;
        if (Input.IsActionPressed("up"))
            dir += Vector2.Up;
        if (Input.IsActionPressed("left"))
            dir += Vector2.Left;
        if (Input.IsActionPressed("down"))
            dir += Vector2.Down;
        if (Input.IsActionPressed("right"))
            dir += Vector2.Right;
        _velocity = dir * 150.0f;

        _velocity = MoveAndSlide(_velocity);

        if (TargetCell == null) {
            _dirIndicator.Visible = false;
            _distLabel.Visible = false;
            _posLabel.Visible = false;
        } else {
            // TODO: fix hard-coded cell size (32)
            var curPos = new Vector2(Mathf.CeilToInt(Position.y / 32), Mathf.CeilToInt(Position.x / 32));
            var target = new Vector2(TargetCell.Row + 1, TargetCell.Col + 1);
            _posLabel.Text = $"{curPos.x},{curPos.y}";
            _distLabel.Text = $"{Mathf.FloorToInt((target - curPos).Length())}";
            
            // Calculate actual on-screen positions, then calculate the angle based on those.
            var t = new Vector2((TargetCell.Col) * 32 + 16, (TargetCell.Row) * 32 + 16);
            var s = new Vector2((curPos.y - 1) * 32 + 16, (curPos.x - 1) * 32 + 16);
            _dirIndicator.Rotation = (t - s).Angle();

            _dirIndicator.Visible = true;
            _distLabel.Visible = true;
            _posLabel.Visible = true;
        }
    }

    public bool LightsEnabled
    {
        get { return _lightMain.Visible; }
        set {
            _lightMain.Visible = value;
            _lightAux.Visible = value;
            _lightPlayer.Visible = value;
        }
    }
}
