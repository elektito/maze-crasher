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

class Maze
{
    private Cell[,] _cells;

    public Maze(int rows, int cols)
    {
        _cells = new Cell[rows, cols];
        for (int row = 0; row < rows; ++row) {
            for (int col = 0; col < cols; ++col) {
                _cells[row, col] = new Cell(row, col);
            }
        }
    }

    public int Rows
    {
        get => _cells.GetLength(0);
    }

    public int Cols
    {
        get => _cells.GetLength(1);
    }

    public Cell this[int row, int col]
    {
        get {
            if (row < 0 || row >= _cells.GetLength(0) || col < 0 || col >= _cells.GetLength(1))
                return null;
            return _cells[row, col];
        }
        set => _cells[row, col] = value;
    }

    public List<Cell> GetUnvisitedNeighbors(Cell cell)
    {
        var neighbors = new List<Cell>();
        Cell neighbor;
        
        neighbor = this[cell.Row + 1, cell.Col];
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        neighbor = this[cell.Row - 1, cell.Col];
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        neighbor = this[cell.Row, cell.Col + 1];
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        neighbor = this[cell.Row, cell.Col - 1];
        if (neighbor != null && !neighbor.Visited)
            neighbors.Add(neighbor);

        return neighbors;
    }
}