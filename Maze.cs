using Godot;
using System;
using System.Collections.Generic;

public class Cell
{
    public int Row;
    public int Col;
    public bool Visited = false;

    private bool _topConnected = false;
    private bool _bottomConnected = false;
    private bool _leftConnected = false;
    private bool _rightConnected = false;

    public Cell(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public String TileName
    {
        get {
            String name = "";

            if (!_topConnected)
                name += "top-";
            if (!_rightConnected)
                name += "right-";
            if (!_bottomConnected)
                name += "bottom-";
            if (!_leftConnected)
                name += "left-";
            
            if (name == "")
                name = "empty";
            else
                name = name.Remove(name.Length - 1);

            return name;
        }
    }

    public override String ToString()
    {
        return $"<{Row},{Col}>";
    }

    public void Connect(Cell other)
    {
        if (Col - other.Col == 1) {
            _leftConnected = true;
            other._rightConnected = true;
        } else if (Col - other.Col == -1) {
            other._leftConnected = true;
            _rightConnected = true;
        } else if (Row - other.Row == 1) {
            _topConnected = true;
            other._bottomConnected = true;
        } else if (Row - other.Row == -1) {
            other._topConnected = true;
            _bottomConnected = true;
        } else {
            Console.WriteLine($"Cells {this} and {other} are not neighbors. Can't connect.");
        }
    }
}

public class Maze : Node2D
{
    private List<Cell> _maze;
    private int _rows = 10;
    private int _cols = 10;
    private bool _debugMode;
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

        RebuildMaze();
    }

    [Export] public bool SlowGenMode { get; set; }

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
        get { return _rows; }
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
        get { return _cols; }
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
        _maze = new List<Cell>();
        for (int row = 0; row < Rows; ++row) {
            for (int col = 0; col < Cols; ++col) {
                _maze.Add(new Cell(row, col));
            }
        }

        var startCell = GetCellAt(0, 0);
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
        var unvisitedNeighbors = GetUnvisitedNeighbors(cell);
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
                var cell = GetCellAt(row, col);
                var tile = _mazeMap.TileSet.FindTileByName(cell.TileName);
                _mazeMap.SetCell(col, row, tile);
            }
        }
    }

    public Cell GetCellAt(int row, int col)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols)
            return null;
        return _maze[row * Cols + col];
    }

    public List<Cell> GetUnvisitedNeighbors(Cell cell)
    {
        var neighbors = new List<Cell>();
        Cell neighbor;
        
        neighbor = GetCellAt(cell.Row + 1, cell.Col);
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        neighbor = GetCellAt(cell.Row - 1, cell.Col);
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        neighbor = GetCellAt(cell.Row, cell.Col + 1);
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        neighbor = GetCellAt(cell.Row, cell.Col - 1);
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        return neighbors;
    }

    void _onSlowGenTimerTimeout()
    {
        MazeStep();
    }
}
