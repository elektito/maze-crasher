using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class MazeNode : Node2D
{
    private int _rows;
    private int _cols;
    private Maze _maze;
    private bool _debugMode = false;
    private Vector2 _cameraOriginalZoom;
    private Dijkstra _dijkstra;
    private Dictionary<Cell, Cell> _dijkstraPrev;
    private Dictionary<Cell, double> _dijkstraDistance;
    private IMazeGenerator _mazeGen = new WilsonMazeGen();
    private Direction _wallWalkDirection = Direction.None;

    private Camera2D _camera;
    private Player _player;
    private CanvasModulate _canvasModulate;
    private TileMap _mazeMap;
    private ColorRect _bg;
    private Timer _slowGenTimer;
    private ColorRect _cellHighlight;
    private Line2D _line;

    public override void _Ready()
    {
        _player = GetNode<Player>("player");
        _camera = GetNode<Camera2D>("player/camera");
        _canvasModulate = GetNode<CanvasModulate>("canvas_modulate");
        _mazeMap = GetNode<TileMap>("maze_map");
        _bg = GetNode<ColorRect>("bg");
        _slowGenTimer = GetNode<Timer>("slow_gen_timer");
        _cellHighlight = GetNode<ColorRect>("cell_highlight");

        _maze = new Maze(Rows, Cols);

        RebuildMaze();
    }

    [Export] public bool SlowGenMode { get; set; } = false;

    public override void _Input(InputEvent inputEvent)
    {
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

        if (Input.IsActionJustPressed("line")) {
            if (_line != null)
                _line.Visible = !_line.Visible;
        }

        if (Input.IsActionJustPressed("rebuild") && !_mazeGen.Generating) {
            RebuildMaze();
        }
    }
    
    [Export] public int Rows
    {
        get {
            return _rows;
        }
        set {
            _rows = value;
            if (!IsInsideTree()) {
                GD.Print("Not inside tree; not rebuilding.");
                return;
            }
            RebuildMaze();
        }
    }

    [Export] public int Cols
    {
        get {
            return _cols;
        }
        set {
            _cols = value;
            if (!IsInsideTree()) {
                GD.Print("Not inside tree; not rebuilding.");
                return;
            }
            RebuildMaze();
        }
    }

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

            _canvasModulate.Visible = !_debugMode;
            _player.LightsEnabled = !_debugMode;
        }
    }

    public void RebuildMaze()
    {
        GD.Print("Building maze with ", Rows, " rows and ", Cols, " columns.");
        _player.TargetCell = null;

        if (SlowGenMode) {
            _cellHighlight.Visible = true;
            _slowGenTimer.Start();
            _mazeGen.StartStepwiseGeneration(Rows, Cols);
        } else {
            _maze = _mazeGen.Generate(Rows, Cols);
            GenerateMapFromMaze();
            CreateLine();
        }

        if (IsInsideTree()) {
            GenerateMapFromMaze();
            _camera.LimitRight = Cols * (int) _mazeMap.CellSize.x;
            _camera.LimitBottom = Rows * (int) _mazeMap.CellSize.y;
            _bg.RectSize = _mazeMap.CellSize * new Vector2(Cols, Rows);
        }
    }

    private void MazeStep(bool rebuildMap = true)
    {
        (bool finished, Maze maze) = _mazeGen.SingleStep();
        _maze = maze;
        if (finished) {
            GenerateMapFromMaze();
            CreateLine();
            _slowGenTimer.Stop();
            return;
        }
        
        var cell = _mazeGen.CurrentCell;
        _cellHighlight.RectPosition = new Vector2(cell.Col * _mazeMap.CellSize.x, cell.Row * _mazeMap.CellSize.y);
        _cellHighlight.RectSize = _mazeMap.CellSize;
        
        if (rebuildMap)
            GenerateMapFromMaze();
    }

    public void GenerateMapFromMaze()
    {
        _mazeMap.Clear();
        for (int row = 0; row < Rows; ++row) {
            for (int col = 0; col < Cols; ++col) {
                var cell = _maze[row, col];
                var tile = _mazeMap.TileSet.FindTileByName(cell.TileName);
                _mazeMap.SetCell(col, row, tile);
            }
        }
    }

    public void CreateLine()
    {
        _dijkstra = new Dijkstra(_maze);
        (_dijkstraPrev, _dijkstraDistance) = _dijkstra.perform(_maze[0, 0]);

        var maxKeyValue = _dijkstraDistance.OrderByDescending(pair => pair.Value).First();
        var maxCell = maxKeyValue.Key;
        var maxDistance = maxKeyValue.Value;

        var cur = maxCell;
        var path = new List<Cell>();
        while (cur != null && _dijkstraPrev.ContainsKey(cur)) {
            path.Add(cur);
            cur = _dijkstraPrev[cur];
        }
        path.Reverse();

        if (_line != null)
            _line.QueueFree();
        _line = new Line2D();
        AddChild(_line);
        _line.Modulate = Colors.Red;
        _line.Width = 2;
        var cellSize = _mazeMap.CellSize.x;
        foreach (Cell cell in path) {
            var cellPos = new Vector2(cell.Col * _mazeMap.CellSize.x + _mazeMap.CellSize.x / 2,
                                        cell.Row * _mazeMap.CellSize.y + _mazeMap.CellSize.y / 2);
            _line.AddPoint(cellPos);
        }

        GD.Print("Target cell: ", maxCell);
        _player.TargetCell = maxCell;
    }

    public void WallWalkStep()
    {
        Cell curCell = GetCurrentCell();
        Cell newCell = null;

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
                return;
            }
        }

        while (newCell == null) {
            Direction rightSide = _wallWalkDirection switch {
                Direction.Down  => Direction.Left,
                Direction.Left  => Direction.Up,
                Direction.Up    => Direction.Right,
                Direction.Right => Direction.Down,
                _               => Direction.None,
            };
            Direction leftSide = _wallWalkDirection switch {
                Direction.Left  => Direction.Down,
                Direction.Up    => Direction.Left,
                Direction.Right => Direction.Up,
                Direction.Down  => Direction.Right,
                _               => Direction.None,
            };

            if (!curCell.IsConnected(rightSide)) {
                // There's a wall to the right; attempt going forward.
                var neighbor = _maze.GetNeighbor(curCell, _wallWalkDirection);
                if (curCell.IsConnected(_wallWalkDirection)) {
                    // The way forward is clear; go ahead.
                    newCell = _maze.GetNeighbor(curCell, _wallWalkDirection);
                } else {
                    // Can't go forward; turn left.
                    _wallWalkDirection = leftSide;
                }
            } else {
                // No wall to the right; move to the right side direction.
                _wallWalkDirection = rightSide;
                newCell = _maze.GetNeighbor(curCell, _wallWalkDirection);
            }
        }

        Console.WriteLine($"Moving from {curCell} to {newCell}.");
        _player.Position = new Vector2(newCell.Col * _mazeMap.CellSize.x + _mazeMap.CellSize.x / 2,
                                       newCell.Row * _mazeMap.CellSize.y + _mazeMap.CellSize.y / 2);
    }

    public Cell GetCurrentCell()
    {
        Vector2 pos = _player.Position;
        int row = Mathf.FloorToInt(pos.y / _mazeMap.CellSize.y);
        int col = Mathf.FloorToInt(pos.x / _mazeMap.CellSize.x);
        return _maze[row, col];
    }

    void _onSlowGenTimerTimeout()
    {
        MazeStep();
    }
}
