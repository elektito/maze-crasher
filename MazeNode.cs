using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class MazeNode : Node2D
{
    [Signal] public delegate void Generated();

    private int _rows;
    private int _cols;
    private Maze _maze;
    private bool _debugMode = false;
    private Dijkstra _dijkstra;
    private Dictionary<Cell, Cell> _dijkstraPrev;
    private Dictionary<Cell, double> _dijkstraDistance;
    private IMazeGenerator _mazeGen = new WilsonMazeGen();
    private Direction _wallWalkDirection = Direction.None;
    private Cell _furthestCell;

    private CanvasModulate _canvasModulate;
    private TileMap _mazeMap;
    private ColorRect _bg;
    private Timer _slowGenTimer;
    private ColorRect _cellHighlight;
    private Line2D _line;

    public override void _Ready()
    {
        _canvasModulate = GetNode<CanvasModulate>("canvas_modulate");
        _mazeMap = GetNode<TileMap>("maze_map");
        _bg = GetNode<ColorRect>("bg");
        _slowGenTimer = GetNode<Timer>("slow_gen_timer");
        _cellHighlight = GetNode<ColorRect>("cell_highlight");

        _maze = new Maze(Rows, Cols);
    }

    [Export] public bool SlowGenMode { get; set; } = false;

    public override void _Input(InputEvent inputEvent)
    {
        if (Input.IsActionJustPressed("line")) {
            if (_line != null)
                _line.Visible = !_line.Visible;
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
        get => _debugMode;
        set {
            _debugMode = value;
            _canvasModulate.Visible = !_debugMode;
        }
    }

    [Export] public Vector2 CellSize
    {
        get {
            if (_mazeMap == null)
                return Vector2.Zero;
            return _mazeMap.CellSize;
        }
        set {
            if (_mazeMap == null)
                return;
            _mazeMap.CellSize = value;
        }
    }

    public Cell FurthestCell
    {
        get => _furthestCell;
    }

    public void RebuildMaze()
    {
        if (_mazeGen.Generating) {
            Console.WriteLine($"Already generating a maze; Rebuild request ignored.");
            return;
        }

        GD.Print("Building maze with ", Rows, " rows and ", Cols, " columns.");

        _bg.RectSize = _mazeMap.CellSize * new Vector2(Cols, Rows);

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
            EmitSignal(nameof(Generated));
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
            EmitSignal(nameof(Generated));
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
        _furthestCell = maxCell;
    }

    void _onSlowGenTimerTimeout()
    {
        MazeStep();
    }

    public (Cell newCell, Direction newDirection) WallWalkStep(Cell curCell, Direction direction)
    {
        return _maze.WallWalkStep(curCell, direction);
    }

    public Cell GetCell(int row, int col)
    {
        return _maze[row, col];
    }
}
