using System.Collections.Generic;

class Dijkstra {
    protected Maze _maze;

    public Dijkstra(Maze maze) {
        _maze = maze;
    }

    public (Dictionary<Cell, Cell> prev, Dictionary<Cell, double> distance) perform(Cell source) {
        var prev = new Dictionary<Cell, Cell>();
        var distance = new Dictionary<Cell, double>();
        var q = new List<Cell>();

        for (int row = 0; row < _maze.Rows; ++row) {
            for (int col = 0; col < _maze.Cols; ++col) {
                distance[_maze[row, col]] = double.PositiveInfinity;
                prev[_maze[row, col]] = null;
                q.Add(_maze[row, col]);
            }
        }
        distance[source] = 0.0;

        while (q.Count > 0) {
            Cell u = null;
            foreach (var v in q) {
                if (u == null || distance[v] < distance[u]) {
                    u = v;
                }
            }
            q.Remove(u);

            foreach (var v in _maze.GetConnectedNeighbors(u)) {
               if (!q.Contains(v))
                    continue;

                var alt = distance[u] + 1;
                if (alt < distance[v]) {
                    distance[v] = alt;
                    prev[v] = u;
                }
            }
        }

        return (prev, distance);
    }
}