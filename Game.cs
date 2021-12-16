using Godot;
using System;

public class Game : Node2D
{
    private bool _debugMode = false;
    private Vector2 _cameraOriginalZoom;
    private Direction _wallWalkDirection = Direction.None;
    private Vector2 _lastPlayerPos;
    private MazeNode _maze;
    private Player _player;
    private Camera2D _camera;

    [Export] public bool DebugMode
    {
        get { return _debugMode; }
        set {
            if (!OS.IsDebugBuild())
                return;

            _debugMode = value;
            if (!IsInsideTree())
                return;

            if (_debugMode) {
                _cameraOriginalZoom = _camera.Zoom;
            } else {
                _camera.Zoom = _cameraOriginalZoom;
            }

            _player.LightsEnabled = !_debugMode;
            _maze.DebugMode = _debugMode;
        }
    }

    public override void _Ready()
    {
        _maze = GetNode<MazeNode>("maze");
        _player = GetNode<Player>("maze/player");
        _camera = GetNode<Camera2D>("maze/player/camera");

        _maze.RebuildMaze();
        _player.Position = GetCellPosition(0, 0);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (OS.IsDebugBuild() && Input.IsActionJustPressed("debug")) {
            DebugMode = !DebugMode;
        }

        var zoomStep = new Vector2(0.1f, 0.1f);
        if (DebugMode &&
            inputEvent is InputEventMouseButton &&
            ((inputEvent as InputEventMouseButton).ButtonIndex == (int) ButtonList.WheelUp) &&
            inputEvent.IsPressed())
        {
            if (_camera.Zoom > zoomStep) {
                _camera.Zoom -= zoomStep;
            }
        }

        if (DebugMode &&
            inputEvent is InputEventMouseButton &&
            ((inputEvent as InputEventMouseButton).ButtonIndex == (int) ButtonList.WheelDown) &&
            inputEvent.IsPressed())
        {
            _camera.Zoom += zoomStep;
        }

        if (Input.IsActionJustPressed("rebuild")) {
            _player.TargetCell = null;
            _maze.RebuildMaze();
        }

        if (Input.IsActionJustPressed("wall_walk_step")) {
            if (_lastPlayerPos != _player.Position) {
                // Reset direction if the player has been manually moved since last "wall-walk-step".
                _wallWalkDirection = Direction.None;
            }

            var curCell = GetCurrentCell();

            if (_wallWalkDirection == Direction.None) {
                // Choose a direction to walk, so that there is a wall to the right of the player.
                if (!curCell.IsConnected(Direction.Left)) {
                    _wallWalkDirection = Direction.Down;
                } else if (!curCell.IsConnected(Direction.Right)) {
                    _wallWalkDirection = Direction.Up;
                } else if (!curCell.IsConnected(Direction.Up)) {
                    _wallWalkDirection = Direction.Left;
                } else if (!curCell.IsConnected(Direction.Down)) {
                    _wallWalkDirection = Direction.Right;
                } else {
                    // Current cell has no walls, so we can't "wall walk".
                    curCell = null;
                }
            }

            if (curCell != null) {
                (Cell newCell, Direction newDir) = _maze.WallWalkStep(curCell, _wallWalkDirection);
                _wallWalkDirection = newDir;

                if (newCell != null) {
                    _player.Position = GetCellPosition(newCell.Row, newCell.Col);
                    _lastPlayerPos = _player.Position;
                }
            }
        }
    }

    public Cell GetCurrentCell()
    {
        Vector2 pos = _player.Position;
        int row = Mathf.FloorToInt(pos.y / _maze.CellSize.y);
        int col = Mathf.FloorToInt(pos.x / _maze.CellSize.x);
        return _maze.GetCell(row, col);
    }

    public Vector2 GetCellPosition(int row, int col)
    {
        return new Vector2(col * _maze.CellSize.x + _maze.CellSize.x / 2,
                           row * _maze.CellSize.y + _maze.CellSize.y / 2);
    }

    public void _OnMazeGenerated()
    {
        _camera.LimitRight = _maze.Cols * (int) _maze.CellSize.x;
        _camera.LimitBottom = _maze.Rows * (int) _maze.CellSize.y;
        _player.TargetCell = _maze.FurthestCell;
    }
}
