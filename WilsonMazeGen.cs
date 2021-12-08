using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

class WilsonMazeGen : IMazeGenerator
{
    private Random _rng = new Random();
    private Maze _maze;
    private List<Cell> _currentPath;
    
    public Cell CurrentCell { get; set; }

    public bool Generating { get; set; } = false;

    public Maze Generate(int rows, int cols)
    {
        StartStepwiseGeneration(rows, cols);

        while (_maze.VisitedCount < rows * cols) {
            SingleStep();
        }

        Generating = false;

        return _maze;
    }

    public void StartStepwiseGeneration(int rows, int cols)
    {
        Generating = true;
        _maze = new Maze(rows, cols);

        // Mark one cell as visited; this will be the first target of random walk.
        _maze[0, 0].Visited = true;

        // Choose a random cell to start random walk from.
        ChooseRandomUnvisitedCurrentCell();

        // Initialize current path.
        _currentPath = new List<Cell>();
        _currentPath.Add(CurrentCell);
    }

    public (bool finished, Maze maze) SingleStep()
    {
        if (_maze.VisitedCount == _maze.Rows * _maze.Cols) {
            Generating = false;
            return (true, _maze);
        }

        var next = ChooseNextCell();
        var index = _currentPath.IndexOf(next);
        if (index >= 0) {
            // Remove loop.
            var remaining = _currentPath.Count - index;
            _currentPath.RemoveRange(index, remaining);
        }

        _currentPath.Add(next);
        if (next.Visited) {
            ConnectPath();

            ChooseRandomUnvisitedCurrentCell();

            if (CurrentCell == null) {
                // This is the last cell.
                Generating = false;
                return (true, _maze);
            }
            
            _currentPath = new List<Cell>();
            _currentPath.Add(CurrentCell);

            return (false, _maze);
        }

        CurrentCell = next;
        return (false, _maze);
    }

    protected Cell ChooseNextCell()
    {
        var dirs = new List<Direction> {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };
        Cell last = _currentPath[_currentPath.Count - 1];
        if (_currentPath.Count > 1) {
            Cell secondLast = _currentPath[_currentPath.Count - 2];
            Direction backDir = last.GetDirectionOf(secondLast);
            dirs.Remove(backDir);
        }
        if (last.Row == 0)
            dirs.Remove(Direction.Up);
        if (last.Row == _maze.Rows - 1)
            dirs.Remove(Direction.Down);
        if (last.Col == 0)
            dirs.Remove(Direction.Left);
        if (last.Col == _maze.Cols - 1)
            dirs.Remove(Direction.Right);
        Direction dir = dirs[_rng.Next(0, dirs.Count)];
        if (_maze.GetNeighbor(last, dir) == null) {
            GD.Print("bad direction; from ", last, " to ", dir);
        }
        return _maze.GetNeighbor(last, dir);
    }

    protected void ConnectPath()
    {
        Console.WriteLine("== Connecting path =============");
        if (_currentPath.Count == 1) {
            // Connect it to a random neighbor.
            var neighbors = _maze.GetNeighbors(_currentPath[0]);
            var neighbor = neighbors[_rng.Next(neighbors.Count)];
            Console.WriteLine($"Randomly connecting {_currentPath[0]} to {neighbor}.");
            _currentPath[0].Connect(neighbor);
            return;
        }

        if (_currentPath.Count == 0) {
            Console.WriteLine("foooo");
        }

        Cell prevCell = _currentPath[0];
        prevCell.Visited = true;
        for (int i = 1; i < _currentPath.Count; ++i) {
            var cell = _currentPath[i];
            Console.WriteLine($"Connecting {prevCell} to {cell}.");
            prevCell.Connect(cell);
            cell.Visited = true;
            prevCell = cell;
        }
    }

    protected void ChooseRandomUnvisitedCurrentCell()
    {
        var unvisited = new List<Cell>();
        for (int row = 0; row < _maze.Rows; ++row) {
            for (int col = 0; col < _maze.Cols; ++col) {
                if (!_maze[row, col].Visited) {
                    unvisited.Add(_maze[row, col]);
                }
            }
        }

        if (unvisited.Count == 0) {
            CurrentCell = null;
            return;
        }

        CurrentCell = unvisited[_rng.Next(unvisited.Count)];
    }
}