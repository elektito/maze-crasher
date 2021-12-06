using System;
using System.Collections.Generic;

class RandomDfsMazeGen : IMazeGenerator
{
    private Random _rng = new Random();
    private Stack<Cell> _stack = new Stack<Cell>();
    private Maze _maze;

    public Cell CurrentCell { get; set; }

    public Maze Generate(int rows, int cols)
    {
        StartStepwiseGeneration(rows, cols);

        while (_stack.Count > 0) {
            SingleStep();
        }

        return _maze;
    }

    public void StartStepwiseGeneration(int rows, int cols)
    {
        _maze = new Maze(rows, cols);

        CurrentCell = _maze[0, 0];
        CurrentCell.Visited = true;
        _stack.Push(CurrentCell);
    }
    public (bool finished, Maze maze) SingleStep()
    {
        if (_stack.Count == 0) {
            return (true, _maze);
        }

        var cell = _stack.Pop();
        if (cell == null)
            return (false, _maze);
        
        CurrentCell = cell;
        cell.Visited = true;
        var unvisitedNeighbors = _maze.GetUnvisitedNeighbors(cell);
        if (unvisitedNeighbors.Count > 0) {
            _stack.Push(cell);
            var next = unvisitedNeighbors[_rng.Next(unvisitedNeighbors.Count)];
            cell.Connect(next);
            next.Visited = true;
            _stack.Push(next);
        }

        return (false, _maze);
    }
}