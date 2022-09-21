using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SL.Shared.Structures
{
    public class Board
    {
        // Height, Width
        // Row, column
        private readonly Cell[,] _cells;
        private readonly Junction[,] _junctions;

        public Board(int rows, int columns)
        {
            Debug.Assert(rows > 0 && columns > 0);
            Rows = rows;
            Columns = columns;

            _cells = new Cell[rows, columns];
            _junctions = new Junction[rows + 1, columns + 1];

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    _cells[r, c] = new Cell();
                }
            }

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var cell = _cells[r, c];
                    cell.Edges[Direction.East] = new () { Vertical = true };
                    cell.Edges[Direction.South] = new () { Vertical = false };
                    cell.Edges[Direction.North] = r > 0 ? _cells[r - 1, c].Edges[Direction.South] : new() { Vertical = false };
                    cell.Edges[Direction.West] = c > 0 ? _cells[r, c - 1].Edges[Direction.East] : new() { Vertical = true };
                }
            }

            for (var r = 0; r <= rows; r++)
            {
                for (var c = 0; c <= columns; c++)
                {
                    var junction = new Junction();
                    _junctions[r, c] = junction;

                    if (c > 0)
                    {
                        junction.Edges[Direction.West] = r < rows ? _cells[r, c - 1].Edges[Direction.North] : _cells[r - 1, c - 1].Edges[Direction.South];
                        junction.Edges[Direction.West].Junctions[Direction.East % 2] = junction;
                    }
                    if (r > 0)
                    {
                        junction.Edges[Direction.North] = c < columns ? _cells[r - 1, c].Edges[Direction.West] : _cells[r - 1, c - 1].Edges[Direction.East];
                        junction.Edges[Direction.North].Junctions[Direction.South % 2] = junction;
                    }
                    if (c < columns)
                    {
                        junction.Edges[Direction.East] = r < rows ? _cells[r, c].Edges[Direction.North] : _cells[r - 1, c].Edges[Direction.South];
                        junction.Edges[Direction.East].Junctions[Direction.West % 2] = junction;
                    }
                    if (r < rows)
                    {
                        junction.Edges[Direction.South] = c < columns ? _cells[r, c].Edges[Direction.West] : _cells[r, c - 1].Edges[Direction.East];
                        junction.Edges[Direction.South].Junctions[Direction.North % 2] = junction;
                    }
                }
            }
        }

        public int Rows { get; }
        public int Columns { get; }

        public Cell this[int row, int column] { get => _cells[row, column]; }

        public Junction GetJunction(int row, int column) => _junctions[row, column];
    }
}
