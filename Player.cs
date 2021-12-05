using Godot;
using System;

public class Player : KinematicBody2D
{
    private Vector2 _velocity = Vector2.Zero;

    private Camera2D _camera;
    private Light2D _lightMain;
    private Light2D _lightAux;
    private Light2D _lightPlayer;

    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("camera");
        _lightMain = GetNode<Light2D>("light_main");
        _lightAux = GetNode<Light2D>("light_aux");
        _lightPlayer = GetNode<Light2D>("light_player");
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
