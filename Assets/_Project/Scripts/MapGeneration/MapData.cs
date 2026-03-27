using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    [System.Serializable]
    public class MapData
    {
        public int width;
        public int height;
        public MapCell[,] cells;
        public List<Room> rooms = new();
        public List<Corridor> corridors = new();
        public Vector2Int spawnCell = new(-1, -1);
        public Vector2Int exitCell = new(-1, -1);

        public MapData(int width, int height)
        {
            this.width = width;
            this.height = height;
            cells = new MapCell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = new MapCell(x, y);
        }

        public bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
        public bool InBounds(Vector2Int p) => InBounds(p.x, p.y);

        public MapCell GetCell(int x, int y) => InBounds(x, y) ? cells[x, y] : null;
        public MapCell GetCell(Vector2Int p) => GetCell(p.x, p.y);

        public void SetCellType(int x, int y, CellType type)
        {
            if (InBounds(x, y)) cells[x, y].type = type;
        }

        public Room GetRoom(int roomId) => rooms.Find(r => r.id == roomId);

        // BFS flood fill pour vérifier la connectivité
        public HashSet<Vector2Int> FloodFillWalkable(Vector2Int start)
        {
            var visited = new HashSet<Vector2Int>();
            if (!InBounds(start) || !cells[start.x, start.y].IsWalkable) return visited;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited.Add(start);

            var dirs = new[] {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var dir in dirs)
                {
                    var next = current + dir;
                    if (InBounds(next) && !visited.Contains(next) && cells[next.x, next.y].IsWalkable)
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            return visited;
        }

        // BFS pathfinding pour trouver le plus court chemin
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (!InBounds(start) || !InBounds(end)) return null;

            var visited = new HashSet<Vector2Int>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            var dirs = new[] {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == end)
                {
                    var path = new List<Vector2Int>();
                    var step = end;
                    while (step != start)
                    {
                        path.Add(step);
                        step = parent[step];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                foreach (var dir in dirs)
                {
                    var next = current + dir;
                    if (InBounds(next) && !visited.Contains(next) && cells[next.x, next.y].IsWalkable)
                    {
                        visited.Add(next);
                        parent[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return null;
        }

        public int CountCells(CellType type)
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (cells[x, y].type == type) count++;
            return count;
        }

        public List<Vector2Int> GetAllWalkableCells()
        {
            var result = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (cells[x, y].IsWalkable)
                        result.Add(new Vector2Int(x, y));
            return result;
        }

        // Distance Manhattan entre deux cellules
        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
