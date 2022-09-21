using SL.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SL.Shared
{
    public class Game
    {

        public Game(Board board)
        {
            Board = board;
        }

        public Board Board { get; }

        public Stack<Move> History { get; } = new();

        public bool MarkCellEdge(int row, int column, int direction, bool? hasLine, bool preventOverride = true)
        {
            var cell = Board[row, column];

            var edge = cell.Edges[direction];
            var oldValue = edge.HasLine;

            if (oldValue == hasLine)
            {
                return false;
            }

            if (preventOverride)
            {
                if (oldValue.HasValue)
                {
                    throw new InvalidOperationException($"Overwriting a '{oldValue}' at r:{row} c:{column} d:{direction}");
                }

                Debug.Assert((oldValue, hasLine) switch
                {
                    (null, null) => true,
                    (null, true) => true,
                    (null, false) => true,
                    (true, null) => false,
                    (true, true) => true,
                    (true, false) => false,
                    (false, null) => false,
                    (false, true) => false,
                    (false, false) => true,
                });
            }

            edge.HasLine = hasLine;

            History.Push(new Move()
            {
                Row = row,
                Column = column,
                Direction = direction,
                OldValue = oldValue,
                NewValue = hasLine,
            });

            return true;
        }

        public bool MarkJunctionEdge(int row, int column, int direction, bool? hasLine)
        {
            // Translate back into cell coordinates first.
            switch (direction)
            {
                case Direction.North:
                    if (column < Board.Columns)
                    {
                        return MarkCellEdge(row - 1, column, Direction.West, hasLine);
                    }
                    return MarkCellEdge(row - 1, column - 1, Direction.East, hasLine);
                case Direction.South:
                    if (column < Board.Columns)
                    {
                        return MarkCellEdge(row, column, Direction.West, hasLine);
                    }
                    return MarkCellEdge(row, column - 1, Direction.East, hasLine);
                case Direction.East:
                    if (row < Board.Rows)
                    {
                        return MarkCellEdge(row, column, Direction.North, hasLine);
                    }
                    return MarkCellEdge(row - 1, column, Direction.South, hasLine);
                case Direction.West:
                    if (row < Board.Rows)
                    {
                        return MarkCellEdge(row, column - 1, Direction.North, hasLine);
                    }
                    return MarkCellEdge(row - 1, column - 1, Direction.South, hasLine);
                default:
                    throw new NotImplementedException(direction.ToString());
            }
        }
    }
}
