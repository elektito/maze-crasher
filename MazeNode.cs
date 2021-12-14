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

        if (Input.IsActionJustPressed("line")) {
            var lines = GetTree().GetNodesInGroup("line");
            var line = lines.Count > 0 ? (Line2D) lines[0] : null;
            if (line != null)
                line.Visible = !line.Visible;
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

        foreach (Line2D existingLine in GetTree().GetNodesInGroup("line")) {
            existingLine.QueueFree();
        }
        var line = new Line2D();
        line.AddToGroup("line");
        AddChild(line);
        line.Modulate = Colors.Red;
        line.Width = 2;
        var cellSize = _mazeMap.CellSize.x;
        foreach (Cell cell in path) {
            var cellPos = new Vector2(cell.Col * _mazeMap.CellSize.x + _mazeMap.CellSize.x / 2,
                                        cell.Row * _mazeMap.CellSize.y + _mazeMap.CellSize.y / 2);
            line.AddPoint(cellPos);
        }
    }

    void _onSlowGenTimerTimeout()
    {
        MazeStep();
    }
}
