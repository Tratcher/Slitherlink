using SL.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
        public Stack<Move> RedoHistory { get; } = new();

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

            // Validations:

            // - An adjacent cell would have too many lines
            if (hasLine == true && cell.Hint == cell.Lines)
            {
                throw new InvalidOperationException($"Cell at r:{row} c:{column} h:{cell.Hint} d:{direction} already has enough lines.");
            }
            int adjacentRow = row, adjacentColumn = column;
            switch (direction)
            {
                case Direction.North:
                    adjacentRow--;
                    break;
                case Direction.South:
                    adjacentRow++;
                    break;
                case Direction.East:
                    adjacentColumn++;
                    break;
                case Direction.West:
                    adjacentColumn--;
                    break;
            }
            if (hasLine == true
                && 0 <= adjacentRow && adjacentRow < Board.Rows
                && 0 <= adjacentColumn && adjacentColumn < Board.Columns)
            {
                var adjacentCell = Board[adjacentRow, adjacentColumn];
                if (adjacentCell.Hint == adjacentCell.Lines)
                {
                    throw new InvalidOperationException($"Cell at r:{adjacentRow} c:{adjacentColumn} h:{adjacentCell.Hint} d:{Direction.Opposite(direction)} already has enough lines.");
                }
            }

            // - The junction already has two lines
            int junction1Row = row, junction1Column = column, junction2Row = row, junction2Column = column;
            switch (direction)
            {
                case Direction.North: // East
                    junction2Column++;
                    break;
                case Direction.South: // East from the south west junction
                    junction1Row++;
                    junction2Row++;
                    junction2Column++;
                    break;
                case Direction.East: // South from the north east junction
                    junction1Column++;
                    junction2Row++;
                    junction2Column++;
                    break;
                case Direction.West: // South from the north west junction
                    junction2Row++;
                    break;
            }
            var junction1 = Board.GetJunction(junction1Row, junction1Column);
            if (hasLine == true && junction1.LineCount >= 2)
            {
                throw new InvalidOperationException($"Junction at r:{junction1Row} c:{junction1Column} already has enough lines.");
            }
            var junction2 = Board.GetJunction(junction2Row, junction2Column);
            if (hasLine == true && junction1.LineCount >= 2)
            {
                throw new InvalidOperationException($"Junction at r:{junction2Row} c:{junction2Column} already has enough lines.");
            }


            // Make the change

            edge.HasLine = hasLine;

            History.Push(new Move()
            {
                Row = row,
                Column = column,
                Direction = direction,
                OldValue = oldValue,
                NewValue = hasLine,
            });

            RedoHistory.Clear();

            return true;
        }

        public bool MarkJunctionEdge(int row, int column, int direction, bool? hasLine, bool preventOverride = true)
        {
            // Translate back into cell coordinates first.
            switch (direction)
            {
                case Direction.North:
                    if (column < Board.Columns)
                    {
                        return MarkCellEdge(row - 1, column, Direction.West, hasLine, preventOverride);
                    }
                    return MarkCellEdge(row - 1, column - 1, Direction.East, hasLine, preventOverride);
                case Direction.South:
                    if (column < Board.Columns)
                    {
                        return MarkCellEdge(row, column, Direction.West, hasLine, preventOverride);
                    }
                    return MarkCellEdge(row, column - 1, Direction.East, hasLine, preventOverride);
                case Direction.East:
                    if (row < Board.Rows)
                    {
                        return MarkCellEdge(row, column, Direction.North, hasLine, preventOverride);
                    }
                    return MarkCellEdge(row - 1, column, Direction.South, hasLine, preventOverride);
                case Direction.West:
                    if (row < Board.Rows)
                    {
                        return MarkCellEdge(row, column - 1, Direction.North, hasLine, preventOverride);
                    }
                    return MarkCellEdge(row - 1, column - 1, Direction.South, hasLine, preventOverride);
                default:
                    throw new NotImplementedException(direction.ToString());
            }
        }

        // TODO: Undo doesn't revert inferences made by the solver
        // Maybe we should clear inferences after running the solver (unless we want to display them?)
        // Otherwise we should add them to the History
        public void Undo()
        {
            if (!History.TryPop(out var move)) return;

            var cell = Board[move.Row, move.Column];
            var edge = cell.Edges[move.Direction];
            Debug.Assert(edge.HasLine == move.NewValue);
            edge.HasLine = move.OldValue;

            RedoHistory.Push(move);
        }

        public void Redo()
        {
            if (!RedoHistory.TryPop(out var move)) return;

            var cell = Board[move.Row, move.Column];
            var edge = cell.Edges[move.Direction];
            Debug.Assert(edge.HasLine == move.OldValue);
            edge.HasLine = move.NewValue;

            History.Push(move);
        }
    }
}
