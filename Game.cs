using Godot;
using System;

public class Game : Node2D
{
    private MazeNode _maze;

    public override void _Ready()
    {
        _maze = GetNode<MazeNode>("maze");
    }

    public override void _Input(InputEvent @event)
    {
        //base._Input(@event);
        if (OS.IsDebugBuild() && Input.IsActionJustPressed("debug")) {
            _maze.DebugMode = !_maze.DebugMode;
        }

        if (Input.IsActionJustPressed("rebuild")) {
            _maze.RebuildMaze();
        }

        if (Input.IsActionJustPressed("wall_walk_step")) {
            _maze.WallWalkStep();
        }
    }
}
