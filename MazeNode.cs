using Godot;
using System;
using System.Collections.Generic;

public class MazeNode : Node2D
{
    private int _rows;
    private int _cols;
    private Maze _maze;
    private bool _debugMode = false;
    private Vector2 _cameraOriginalZoom;

    private Random _rng = new Random();
    private Stack<Cell> _stack = new Stack<Cell>();

    private Camera2D _camera;
    private Player _player;
    private CanvasModulate _canvasModulate;
    private TileMap _mazeMap;
    private ColorRect _bg;
    private Timer _slowGenTimer;
    private ColorRect _cellHighlight;

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
        _maze = new Maze(Rows, Cols);

        var startCell = _maze[0, 0];
        startCell.Visited = true;
        _stack.Push(startCell);

        if (SlowGenMode) {
            _cellHighlight.Visible = true;
            _slowGenTimer.Start();
        } else {
            while (_stack.Count > 0) {
                MazeStep(false);
            }

            GenerateMapFromMaze();
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
        if (_stack.Count == 0) {
            _slowGenTimer.Stop();
            return;
        }

        var cell = _stack.Pop();
        if (cell == null)
            return;
        
        
        _cellHighlight.RectPosition = new Vector2(cell.Col * _mazeMap.CellSize.x, cell.Row * _mazeMap.CellSize.y);
        _cellHighlight.RectSize = _mazeMap.CellSize;
        
        cell.Visited = true;
        var unvisitedNeighbors = _maze.GetUnvisitedNeighbors(cell);
        if (unvisitedNeighbors.Count > 0) {
            _stack.Push(cell);
            var next = unvisitedNeighbors[_rng.Next(unvisitedNeighbors.Count)];
            cell.Connect(next);
            next.Visited = true;
            _stack.Push(next);
        }

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

    void _onSlowGenTimerTimeout()
    {
        MazeStep();
    }
}
