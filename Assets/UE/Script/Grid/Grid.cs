using System;
using System.Collections.Generic;
using UnityEngine;

namespace UE.Script.Grid
{
    [Serializable]
    public class Grid<T>
    {
        public CellData[] Cells { get; }
        public int Width { get; }
        public int Height { get; }

        private Vector2 _size = new(1.0f, 1.0f);

        public Vector2 Size
        {
            get => _size;
            set => _size = value;
        }

        public Vector2 Gap
        {
            get => _gap;
            set => _gap = value;
        }

        private Vector2 _gap = new(0.5f, 0.5f);

        public struct CellData
        {
            public int CellIndex;
            public T CellObject;
        }

        public Grid(int width, int height)
        {
            Cells = new CellData[width * height];
            Width = width;
            Height = height;
        }

        public Vector2 CoordsToWorld(int x, int y)
        {
            return new Vector2((x * _size.x) + (x * _gap.x), (y * _size.y) + (y * _gap.y));
        }

        public Vector2Int WorldToCoords(Vector2 localPos)
        {
            int x = (int)((localPos.x + (_size.x * 0.5f)) / (_size.x + _gap.x));
            int y = (int)((localPos.y + (_size.y * 0.5f)) / (_size.y + _gap.y));
            return new Vector2Int(x, y);
        }

        public int CoordsToIndex(int x, int y)
        {
            return y * Width + x;
        }

        public int CoordsToIndex(Vector2Int coords)
        {
            return CoordsToIndex(coords.x, coords.y);
        }

        public Vector2Int IndexToCoords(int index)
        {
            return new Vector2Int(index % Width, index / Width);
        }

        public void Set(int x, int y, CellData value)
        {
            Cells[CoordsToIndex(x, y)] = value;
        }

        public void Set(Vector2Int coords, CellData value)
        {
            Cells[CoordsToIndex(coords.x, coords.y)] = value;
        }

        public void Set(int index, CellData value)
        {
            Cells[index] = value;
        }
    
        public CellData Get(int x, int y)
        {
            return Cells[CoordsToIndex(x, y)];
        }

        public CellData Get(Vector2Int coords)
        {
            return Cells[CoordsToIndex(coords.x, coords.y)];
        }

        public CellData Get(int index)
        {
            return Cells[index];
        }

        public bool AreCoordsValid(int x, int y, bool safeWalls = false)
        {
            return safeWalls ? 
                (x > 0 && x < Width - 1 && y > 0 && y < Height - 1) :
                (x >= 0 && x < Width && y >= 0 && y < Height);
        }

        public bool AreCoordsValid(Vector2Int coords, bool safeWalls = false)
        {
            return AreCoordsValid(coords.x, coords.y, safeWalls);
        }

        public Vector2Int GetCoords(T value)
        {
            var i = Array.IndexOf(Cells, value);
            if (i == -1)
            {
                throw new ArgumentException();
            }

            return IndexToCoords(i);
        }

        public List<CellData> GetNeighbours(Vector2Int coords, bool safeWalls = false)
        {
            var directions = (Direction[])Enum.GetValues(typeof(Direction));
            var neighbours = new List<CellData>();
            foreach (var direction in directions)
            {
                var neighbourCoords = coords + direction.ToCoords();
                if (AreCoordsValid(neighbourCoords, safeWalls))
                {
                    neighbours.Add(Get(coords));
                }
            }

            return neighbours;
        }
    }
}

