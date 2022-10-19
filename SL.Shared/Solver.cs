using SL.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SL.Shared
{
    public class Solver
    {
        private readonly HashSet<KeyValuePair<int, int>> _finishedCells = new();
        private readonly HashSet<KeyValuePair<int, int>> _finishedJunctions = new();
        private readonly HashSet<KeyValuePair<int, int>> _cellsToTry = new();
        private readonly HashSet<KeyValuePair<int, int>> _junctionsToTry = new();
        private readonly Game _game;
        private readonly Board _board;

        private Solver(Game game)
        {
            _game = game;
            _board = game.Board;
        }

        public static bool Solve(Game game)
        {
            var solver = new Solver(game);
            // Reset any stale inferences, they may be derived from invalid user input.
            solver.ClearInferences();

            // MarkZeros(game); // Generalized by MarkLinesMatchingHints

            solver.MarkAdjacentThrees();

            // MarkDiagonalThrees(game); // Generalized by InferThrees

            do
            {
                if (solver.CheckRecentlyChangedCells()) continue;
                if (solver.CheckRecentlyChangedJunctions()) continue;
                if (solver.MarkLinesMatchingHints()) continue;
                if (solver.MarkDeadEnds()) continue;
                if (solver.ExtendLines()) continue;
                if (solver.MarkOnesInACorner()) continue;
                if (solver.MarkThreesInACorner()) continue;
                if (solver.MarkThreesWithIncomingLines()) continue;
                if (solver.MarkTwosInACorner()) continue;
                if (solver.InferOnes()) continue;
                if (solver.InferTwos()) continue;
                if (solver.InferThrees()) continue;
                if (solver.InferExit()) continue;
                if (solver.CheckInferences()) continue;
                if (solver.CheckSingleCellParity()) continue;
                if (solver.DetectLoops()) continue;

                // Ones next to threes on an edge

                // Check parity, Isolated reagons (LH37)
                // - Can't close off a section from the rest of the board
                // - If a cell only has one possible external exit, and there's an odd number of loose ends, the line that can exit must.
                break;
            } while (true);

            return game.IsSolved();
        }

        public static bool SolveWithLookAhead(Game game)
        {
            var itterations = 1;
            if (Solve(game)) return true;
            var board = game.Board;
            int progress;
            // Start by only trying to extend lines
            do
            {
                itterations++;
                progress = game.History.Count;

                // Try to extend lines
                for (var r = 0; r <= board.Rows; r++)
                {
                    for (var c = 0; c <= board.Columns; c++)
                    {
                        var junction = board.GetJunction(r, c);
                        if (junction.LineCount == 1)
                        {
                            // if (TestEdge(game, r, c, Direction.North)) continue;
                            // if (TestEdge(game, r, c, Direction.West)) continue;
                            if (TestEdge(game, r, c, Direction.South)) continue;
                            if (TestEdge(game, r, c, Direction.East)) continue;
                        }
                    }
                }
                if (game.IsSolved()) return true;
                if (game.History.Count > progress) continue;
                itterations++;

                // Then test every remaining edge
                // Pick un unassigned edge.
                // If we make any progress, go back to only looking at lines.
                for (var r = 0; r <= board.Rows; r++)
                {
                    for (var c = 0; c <= board.Columns; c++)
                    {
                        // Since we're checking every junction we only need to look at two edges each.
                        if (TestEdge(game, r, c, Direction.East)) continue;
                        if (TestEdge(game, r, c, Direction.South)) continue;
                    }
                }

            } while (game.History.Count > progress);

            return game.IsSolved();

            static bool TestEdge(Game game, int row, int column, int direction)
            {
                var junction = game.Board.GetJunction(row, column);
                var edge = junction.Edges[direction];
                if (edge == null || edge.HasLine.HasValue) return false;

                var checkpoint = game.History.Count;

                // Set it to true. Try solving.
                game.MarkJunctionEdge(row, column, direction, true);
                try
                {
                    if (Solve(game)) return true;
                }
                catch (InvalidOperationException ioe1)
                {
                    // If it fails, this edge must be false.
                    game.Reset(checkpoint);
                    game.MarkJunctionEdge(row, column, direction, false);
                    try
                    {
                        Solve(game);
                        return true;
                    }
                    catch (InvalidOperationException ioe2)
                    {
                        game.Reset(checkpoint);
                        throw new AggregateException($"r:{row}, c:{column}, d:{direction} breaks in both directions.", ioe1, ioe2);
                    }
                }

                //  else, set it to false and try solving
                game.Reset(checkpoint);
                game.MarkJunctionEdge(row, column, direction, false);
                try
                {
                    if (Solve(game)) return true;
                }
                catch (InvalidOperationException ioe1)
                {
                    // If that fails, set it to true and re-solve
                    game.Reset(checkpoint);
                    game.MarkJunctionEdge(row, column, direction, true);
                    try
                    {
                        Solve(game);
                        return true;
                    }
                    catch (InvalidOperationException ioe2)
                    {
                        game.Reset(checkpoint);
                        throw new AggregateException($"r:{row}, c:{column}, d:{direction} breaks in both directions.", ioe1, ioe2);
                    }
                }

                //  else reset and move-on.
                game.Reset(checkpoint);
                return false;
            }
        }

        private void ClearInferences()
        {
            for (var r = 0; r <= _board.Rows; r++)
            {
                for (var c = 0; c <= _board.Columns; c++)
                {
                    var junction = _board.GetJunction(r, c);
                    junction.Inferences.Clear();
                }
            }
        }

        private static readonly KeyValuePair<int, int>[] _cellNeighboorOffsets = new KeyValuePair<int, int>[]
        {
            new KeyValuePair<int, int>(-1, -1), // NW
            new KeyValuePair<int, int>(-1, 0), // N
            new KeyValuePair<int, int>(-1, 1), // NE
            new KeyValuePair<int, int>(0, -1), // W
            new KeyValuePair<int, int>(0, 0), // Self
            new KeyValuePair<int, int>(0, 1), // E
            new KeyValuePair<int, int>(1, -1), // SW
            new KeyValuePair<int, int>(1, 0), // S
            new KeyValuePair<int, int>(1, 1), // SE
        };

        public bool MarkCellEdge(int row, int column, int direction, bool? hasLine)
        {
            if (!_game.MarkCellEdge(row, column, direction, hasLine))
            {
                return false;
            }

            // State changed, mark this and adjacent cells for re-exhamination.
            for (var i = 0; i < _cellNeighboorOffsets.Length; i++)
            {
                var offsets = _cellNeighboorOffsets[i];
                var nRow = offsets.Key + row;
                var nCol = offsets.Value + column;
                var pair = new KeyValuePair<int, int>(nRow, nCol);
                if (0 <= nRow && nRow < _board.Rows
                    && 0 <= nCol && nCol < _board.Columns
                    && !_finishedCells.Contains(pair))
                {
                    // if (_board[nRow, nCol].Undetermined == 0)
                    // TODO: WTF: Why does this typo make 10x it faster?
                    // Worse, now it can't even solve HH105 without this typo
                    if (_board[nCol, nCol].Undetermined == 0)
                    {
                        _finishedCells.Add(pair);
                    }
                    else
                    {
                        _cellsToTry.Add(pair);
                    }
                }
            }

            // Also check affected junctions
            int junctionR1 = row, junctionC1 = column, junctionR2 = row, junctionC2 = column;
            switch (direction)
            {
                case Direction.North:
                    junctionC2++;
                    break;
                case Direction.East:
                    junctionC1++;
                    junctionC2++;
                    junctionR2++;
                    break;
                case Direction.South:
                    junctionR1++;
                    junctionC2++;
                    junctionR2++;
                    break;
                case Direction.West:
                    junctionR2++;
                    break;
            }

            CheckJunction(junctionR1, junctionC1);
            CheckJunction(junctionR2, junctionC2);
            return true;

            void CheckJunction(int row, int column)
            {
                var pair = new KeyValuePair<int, int>(row, column);
                if (!_finishedJunctions.Contains(pair))
                {
                    if (_board.GetJunction(row, column).UnknownCount == 0)
                    {
                        _finishedJunctions.Add(pair);
                    }
                    else
                    {
                        _junctionsToTry.Add(pair);
                    }
                }
            }
        }

        // Given a junction coordinate, identify the four cells around it.
        private static readonly KeyValuePair<int, int>[] _cellsOffsetsAroundJunction = new KeyValuePair<int, int>[4]
        {
            new KeyValuePair<int, int>(-1, -1), // NW
            new KeyValuePair<int, int>(-1, 0), // NE
            new KeyValuePair<int, int>(0, -1), // SW
            new KeyValuePair<int, int>(0, 0), // SE
        };

        public bool MarkJunctionEdge(int row, int column, int direction, bool? hasLine)
        {
            if (!_game.MarkJunctionEdge(row, column, direction, hasLine))
            {
                return false;
            }

            // Find the other junction
            int junctionR1 = row, junctionC1 = column, junctionR2 = row, junctionC2 = column;
            switch (direction)
            {
                case Direction.North:
                    junctionR2--;
                    break;
                case Direction.East:
                    junctionC2++;
                    break;
                case Direction.South:
                    junctionR2++;
                    break;
                case Direction.West:
                    junctionC2--;
                    break;
            }

            CheckJunction(junctionR1, junctionC1);
            CheckJunction(junctionR2, junctionC2);

            // Also check nearby cells
            for (var i = 0; i < _cellsOffsetsAroundJunction.Length; i++)
            {
                var offsets = _cellsOffsetsAroundJunction[i];
                var c1Row = offsets.Key + junctionR1;
                var c1Col = offsets.Value + junctionC1;
                var c2Row = offsets.Key + junctionR2;
                var c2Col = offsets.Value + junctionC2;
                CheckCell(c1Row, c1Col);
                CheckCell(c2Row, c2Col);
            }

            return true;

            void CheckJunction(int row, int column)
            {
                var pair = new KeyValuePair<int, int>(row, column);
                if (!_finishedJunctions.Contains(pair))
                {
                    if (_board.GetJunction(row, column).UnknownCount == 0)
                    {
                        _finishedJunctions.Add(pair);
                    }
                    else
                    {
                        _junctionsToTry.Add(pair);
                    }
                }
            }

            void CheckCell(int row, int column)
            {
                var pair = new KeyValuePair<int, int>(row, column);
                if (0 <= row && row < _board.Rows
                    && 0 <= column && column < _board.Columns
                    && !_finishedCells.Contains(pair))
                {
                    if (_board[row, column].Undetermined == 0)
                    {
                        _finishedCells.Add(pair);
                    }
                    else
                    {
                        _cellsToTry.Add(pair);
                    }
                }
            }
        }

        // Look at cells near recent changes to see if we can deduce anything
        public bool CheckRecentlyChangedCells()
        {
            bool progress = false;
            while (_cellsToTry.Count > 0)
            {
                var coordinates = _cellsToTry.First();
                _cellsToTry.Remove(coordinates);
                coordinates.Deconstruct(out var row, out var column);
                var cell = _board[row, column];
                if (cell.Undetermined == 0)
                {
                    _finishedCells.Add(coordinates);
                    continue;
                }

                progress |= MarkLinesMatchingHints(row, column);
                progress |= MarkOnesInACorner(row, column);
                progress |= MarkThreesInACorner(row, column);
                progress |= MarkTwosInACorner(row, column);
                progress |= InferOnes(row, column);
                progress |= InferTwos(row, column);
                progress |= InferThrees(row, column);
            }

            return progress;
        }

        // Look at junctions near recent changes to see if we can deduce anything
        public bool CheckRecentlyChangedJunctions()
        {
            bool progress = false;
            while (_junctionsToTry.Count > 0)
            {
                var coordinates = _junctionsToTry.First();
                _junctionsToTry.Remove(coordinates);
                coordinates.Deconstruct(out var row, out var column);
                var junction = _board.GetJunction(row, column);
                if (junction.UnknownCount == 0)
                {
                    _finishedJunctions.Add(coordinates);
                    continue;
                }

                progress |= MarkDeadEnds(row, column);
                progress |= ExtendLines(row, column);
                progress |= CheckInferences(row, column);
            }

            return progress;
        }

        private void MarkAdjacentThrees()
        {
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    var cell = _board[r, c];
                    if (cell.Hint != 3)
                    {
                        continue;
                    }
                    // Since we check every cell we only need to look east and south.

                    // East
                    if (c < _board.Columns - 1 && _board[r, c + 1].Hint == 3)
                    {
                        MarkCellEdge(r, c, Direction.West, true);
                        MarkCellEdge(r, c, Direction.East, true);
                        MarkCellEdge(r, c + 1, Direction.East, true);

                        // Also x the north and south east edged as innaccessible

                        if (r > 0)
                        {
                            MarkCellEdge(r - 1, c, Direction.East, false);
                        }

                        if (r < _board.Rows - 1)
                        {
                            MarkCellEdge(r + 1, c, Direction.East, false);
                        }
                    }

                    // South
                    if (r < _board.Rows - 1 && _board[r + 1, c].Hint == 3)
                    {
                        MarkCellEdge(r, c, Direction.North, true);
                        MarkCellEdge(r, c, Direction.South, true);
                        MarkCellEdge(r + 1, c, Direction.South, true);

                        // Also x the east and west south edged as innaccessible

                        if (c > 0)
                        {
                            MarkCellEdge(r, c - 1, Direction.South, false);
                        }

                        if (c < _board.Columns - 1)
                        {
                            MarkCellEdge(r, c + 1, Direction.South, false);
                        }
                    }
                }
            }
        }

        // Mark 3s with a line entering, opposite corner must have lines
        // - the other line possible exit line can be x'd. (covered by inference)
        private bool MarkThreesWithIncomingLines()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= MarkThreesWithIncomingLines(r, c);
                }
            }

            return progress;
        }

        // Mark 3s with a line entering, opposite corner must have lines
        // - the other line possible exit line can be x'd. (covered by inference)
        private bool MarkThreesWithIncomingLines(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (cell.Hint != 3)
            {
                return false;
            }

            // Check each junction. If it has an external line then mark the opposite edges as lines.

            // NW
            if (JunctionHasOneOutwardLine(_board.GetJunction(r, c), Direction.North, Direction.West, allowUnknown: true))
            {
                progress |= MarkCellEdge(r, c, Direction.East, true);
                progress |= MarkCellEdge(r, c, Direction.South, true);
            }
            // NE
            if (JunctionHasOneOutwardLine(_board.GetJunction(r, c + 1), Direction.North, Direction.East, allowUnknown: true))
            {
                progress |= MarkCellEdge(r, c, Direction.South, true);
                progress |= MarkCellEdge(r, c, Direction.West, true);
            }
            // SE
            if (JunctionHasOneOutwardLine(_board.GetJunction(r + 1, c + 1), Direction.South, Direction.East, allowUnknown: true))
            {
                progress |= MarkCellEdge(r, c, Direction.North, true);
                progress |= MarkCellEdge(r, c, Direction.West, true);
            }
            // SW
            if (JunctionHasOneOutwardLine(_board.GetJunction(r + 1, c), Direction.South, Direction.West, allowUnknown: true))
            {
                progress |= MarkCellEdge(r, c, Direction.East, true);
                progress |= MarkCellEdge(r, c, Direction.North, true);
            }

            return progress;
        }

        private bool MarkOnesInACorner()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= MarkOnesInACorner(r, c);
                }
            }
            return progress;
        }

        // X 1's edges in a corner
        // - other lines are xOr
        private bool MarkOnesInACorner(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (cell.Hint != 1)
            {
                return false;
            }

            if (IsCorner(cell, Direction.North, Direction.East))
            {
                progress |= MarkCellEdge(r, c, Direction.North, false);
                progress |= MarkCellEdge(r, c, Direction.East, false);
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East); // SW
                progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West); // SW
            }

            if (IsCorner(cell, Direction.South, Direction.East))
            {
                progress |= MarkCellEdge(r, c, Direction.South, false);
                progress |= MarkCellEdge(r, c, Direction.East, false);
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East); // NW
                progress |= InferJunctionXor(r, c, Direction.North, Direction.West); // NW
            }

            if (IsCorner(cell, Direction.South, Direction.West))
            {
                progress |= MarkCellEdge(r, c, Direction.South, false);
                progress |= MarkCellEdge(r, c, Direction.West, false);
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West); // NE
                progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East); // NE
            }

            if (IsCorner(cell, Direction.North, Direction.West))
            {
                progress |= MarkCellEdge(r, c, Direction.North, false);
                progress |= MarkCellEdge(r, c, Direction.West, false);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West); // SE
                progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East); // SE
            }
            return progress;
        }

        // Mark 3s in corners
        // - opposite corner is xor
        private bool MarkThreesInACorner()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= MarkThreesInACorner(r, c);
                }
            }
            return progress;
        }

        // Mark 3s in corners
        // - opposite corner is xor
        private bool MarkThreesInACorner(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (cell.Hint != 3 || cell.Undetermined == 0)
            {
                return false;
            }

            if (IsCorner(cell, Direction.North, Direction.East))
            {
                progress |= MarkCellEdge(r, c, Direction.North, true);
                progress |= MarkCellEdge(r, c, Direction.East, true);
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East); // SW
                progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West); // SW
            }

            if (IsCorner(cell, Direction.South, Direction.East))
            {
                progress |= MarkCellEdge(r, c, Direction.South, true);
                progress |= MarkCellEdge(r, c, Direction.East, true);
                progress |= InferJunctionXor(r, c, Direction.North, Direction.West); // NW
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East); // NW
            }

            if (IsCorner(cell, Direction.South, Direction.West))
            {
                progress |= MarkCellEdge(r, c, Direction.South, true);
                progress |= MarkCellEdge(r, c, Direction.West, true);
                progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East); // NE
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West); // NE
            }

            if (IsCorner(cell, Direction.North, Direction.West))
            {
                progress |= MarkCellEdge(r, c, Direction.North, true);
                progress |= MarkCellEdge(r, c, Direction.West, true);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West); // SE
                progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East); // SE
            }
            return progress;
        }

        private bool MarkTwosInACorner()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= MarkTwosInACorner(r, c);
                }
            }

            return progress;
        }

        // Mark 2s in corners
        // - Draw lines out the ends if only one option (LN7, LH37)
        // - Opposite corner is adjacent to a 3, must take the corner (LH37)
        // - Opposite corner has a line entering, must turn away from 2 (LH37)
        // - Opposite corner only has one possible line, can't exit that way.
        // TODO
        // - Opposite corner is adjacent to a 1, can't exit to either edge of the 1
        // - At least one of the opposite edges isn't available, must take the corner
        private bool MarkTwosInACorner(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (cell.Hint != 2)
            {
                return false;
            }

            if (IsCorner(cell, Direction.North, Direction.East))
            {
                progress |= InferJunctionXor(r, c, Direction.North, Direction.West); // NW c
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East);

                progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);  // SE c
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);

                progress |= MarkOutgoingSingleLine(r + 1, c, Direction.South, Direction.West, false);
                if (GetHint(r + 1, c - 1) == 3
                    || JunctionHasOneOutwardLine(_board.GetJunction(r + 1, c), Direction.South, Direction.West, allowUnknown: true)) // SW
                {
                    progress |= MarkCellEdge(r, c, Direction.North, true);
                    progress |= MarkCellEdge(r, c, Direction.East, true);
                }
            }

            if (IsCorner(cell, Direction.South, Direction.East))
            {
                progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East); // NE c
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);

                progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);  // SW c
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);

                progress |= MarkOutgoingSingleLine(r, c, Direction.North, Direction.West, false);
                if (GetHint(r - 1, c - 1) == 3
                    || JunctionHasOneOutwardLine(_board.GetJunction(r, c), Direction.North, Direction.West, allowUnknown: true)) // NW
                {
                    progress |= MarkCellEdge(r, c, Direction.South, true);
                    progress |= MarkCellEdge(r, c, Direction.East, true);
                }
            }

            if (IsCorner(cell, Direction.South, Direction.West))
            {
                progress |= InferJunctionXor(r, c, Direction.North, Direction.West); // NW c
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East);

                progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);  // SE c
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);

                progress |= MarkOutgoingSingleLine(r, c + 1, Direction.North, Direction.East, false);
                if (GetHint(r - 1, c + 1) == 3
                    || JunctionHasOneOutwardLine(_board.GetJunction(r, c + 1), Direction.North, Direction.East, allowUnknown: true)) // NE
                {
                    progress |= MarkCellEdge(r, c, Direction.South, true);
                    progress |= MarkCellEdge(r, c, Direction.West, true);
                }
            }

            if (IsCorner(cell, Direction.North, Direction.West))
            {
                progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East); // NE c
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);

                progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);  // SW c
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);

                progress |= MarkOutgoingSingleLine(r + 1, c + 1, Direction.South, Direction.East, false);
                if (GetHint(r + 1, c + 1) == 3
                    || JunctionHasOneOutwardLine(_board.GetJunction(r + 1, c + 1), Direction.South, Direction.East, allowUnknown: true)) // SE
                {
                    progress |= MarkCellEdge(r, c, Direction.North, true);
                    progress |= MarkCellEdge(r, c, Direction.West, true);
                }
            }
            return progress;
        }

        // X edges that dead end
        private bool MarkDeadEnds()
        {
            bool progress = false;
            for (var r = 0; r <= _board.Rows; r++)
            {
                for (var c = 0; c <= _board.Columns; c++)
                {
                    progress |= MarkDeadEnds(r, c);
                }
            }
            return progress;
        }

        // X edges that dead end
        private bool MarkDeadEnds(int r, int c)
        {
            bool progress = false;
            var junction = _board.GetJunction(r, c);
            var unknown = junction.UnknownCount;
            if (unknown == 0 || unknown > 2) return false;
            var lines = junction.LineCount;
            var totalAlive = lines + unknown;

            var north = junction.Edges[Direction.North];
            var south = junction.Edges[Direction.South];
            var east = junction.Edges[Direction.East];
            var west = junction.Edges[Direction.West];

            if (totalAlive == 1)
            {
                // No exit, x out the reaming edge
                if (north != null && !north.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.North, false);
                }
                else if (south != null && !south.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.South, false);
                }
                else if (east != null && !east.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.East, false);
                }
                else if (west != null && !west.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.West, false);
                }
            }
            else if (lines == 2)
            {
                // The line has already entered and exited this intercection. Remaining edges can be x'd.
                if (north != null && !north.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.North, false);
                }
                if (south != null && !south.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.South, false);
                }
                if (east != null && !east.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.East, false);
                }
                if (west != null && !west.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.West, false);
                }
            }
            return progress;
        }

        // When all marked or unknown edges total to match the hint
        // OR when all known lines match the hint
        private bool MarkLinesMatchingHints()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= MarkLinesMatchingHints(r, c);
                }
            }
            return progress;
        }

        private bool MarkLinesMatchingHints(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (!cell.Hint.HasValue)
            {
                return false;
            }
            var undetermined = cell.Undetermined;
            if (undetermined == 0) return false;
            var north = cell.Edges[Direction.North];
            var south = cell.Edges[Direction.South];
            var east = cell.Edges[Direction.East];
            var west = cell.Edges[Direction.West];
            var totalLines = cell.Lines;
            var availableEdges = totalLines + undetermined;

            // Possible edges matches hit, mark them all as lines
            if (availableEdges == cell.Hint)
            {
                if (!north.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.North, true);
                }
                if (!south.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.South, true);
                }
                if (!east.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.East, true);
                }
                if (!west.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.West, true);
                }
            }
            // There are already enough lines to satisfy the hint, x out the remainder
            else if (totalLines == cell.Hint)
            {
                if (!north.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.North, false);
                }
                if (!south.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.South, false);
                }
                if (!east.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.East, false);
                }
                if (!west.HasLine.HasValue)
                {
                    progress |= MarkCellEdge(r, c, Direction.West, false);
                }
            }
            return progress;
        }

        // Extend lines with only one option
        private bool ExtendLines()
        {
            bool progress = false;
            for (var r = 0; r <= _board.Rows; r++)
            {
                for (var c = 0; c <= _board.Columns; c++)
                {
                    progress |= ExtendLines(r, c);
                }
            }
            return progress;
        }

        // Extend lines with only one option
        private bool ExtendLines(int r, int c)
        {
            bool progress = false;
            var junction = _board.GetJunction(r, c);
            if (junction.LineCount != 1) return false;

            if (junction.UnknownCount == 1)
            {
                var north = junction.Edges[Direction.North];
                var south = junction.Edges[Direction.South];
                var east = junction.Edges[Direction.East];
                var west = junction.Edges[Direction.West];
                // Two possible lines and one is marked. Mark the other.
                if (north != null && !north.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.North, true);
                }
                else if (south != null && !south.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.South, true);
                }
                else if (east != null && !east.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.East, true);
                }
                else if (west != null && !west.HasLine.HasValue)
                {
                    progress |= MarkJunctionEdge(r, c, Direction.West, true);
                }
            }
            return progress;
        }

        // Check if making an edge a line would close a loop.
        // Mark X if this this is a sub loop.
        // TODO: Mark as a line if all clues have/would be satisfied and this ends the game.
        private bool DetectLoops()
        {
            bool progress = false;
            for (var r = 0; r <= _board.Rows; r++)
            {
                for (var c = 0; c <= _board.Columns; c++)
                {
                    var junction = _board.GetJunction(r, c);
                    var north = junction.Edges[Direction.North];
                    var south = junction.Edges[Direction.South];
                    var east = junction.Edges[Direction.East];
                    var west = junction.Edges[Direction.West];

                    var northHasLine = north?.HasLine == true;
                    var southHasLine = south?.HasLine == true;
                    var eastHasLine = east?.HasLine == true;
                    var westHasLine = west?.HasLine == true;
                    var totalLines = (northHasLine ? 1 : 0) + (southHasLine ? 1 : 0) + (eastHasLine ? 1 : 0) + (westHasLine ? 1 : 0);

                    // We're only evaluating if a line could extend from this juction. 0 or 2 would prevent that.
                    if (totalLines != 1)
                    {
                        continue;
                    }

                    // For this exercise we're really enumerating edges, so for a given junction only look at the east and south lines
                    // to avoid duplicate effort.

                    if (east != null && !east.HasLine.HasValue)
                    {
                        progress |= DetectLoop(east, r, c, Direction.East);
                    }

                    if (south != null && !south.HasLine.HasValue)
                    {
                        progress |= DetectLoop(south, r, c, Direction.South);
                    }
                }
            }
            return progress;
        }

        // Trace if marking this edge would create a loop.
        private bool DetectLoop(Edge edge, int row, int column, int direction)
        {
            var startJunction = edge.Junctions[0];
            var endJunction = edge.Junctions[1];

            if (startJunction.LineCount != 1 || endJunction.LineCount != 1)
            {
                // These two can't be connected right now.
                return false;
            }

            if (FollowLine(startJunction) == endJunction)
            {
                // Found a loop, x out this edge
                return MarkJunctionEdge(row, column, direction, false);
            }

            return false;

            Junction FollowLine(Junction junction)
            {
                Edge? priorEdge = null;
                bool progressed;
                do
                {
                    progressed = false;
                    foreach (var edge in junction.Edges)
                    {
                        if (edge != null && edge != priorEdge && edge.HasLine == true)
                        {
                            junction = GetNextJunction(edge, junction);
                            progressed = true;
                            priorEdge = edge;
                            break;
                        }
                    }
                }
                while (progressed);

                return junction;
            }

            // Get the oposite one
            Junction GetNextJunction(Edge edge, Junction junction)
            {
                return edge.Junctions[0] == junction ? edge.Junctions[1] : edge.Junctions[0];
            }
        }

        private bool InferOnes()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= InferOnes(r, c);
                }
            }
            return progress;
        }

        // - One line entering a 1, x the opposite edges (LH37)
        //  - Mark diagonal 1's, 2s, 3s. (covered by inference)
        // - Two connected edges available, must exit from that corner, single line or infered xor (SM43)
        // TODO:
        // - Two adjacent ones on the edge, can't go between them. (HH105, east edge)
        private bool InferOnes(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (cell.Hint != 1 || cell.Lines > 0)
            {
                return false;
            }

            if (JunctionHasOneOutwardLine(_board.GetJunction(r, c), Direction.North, Direction.West))
            {
                progress |= MarkCellEdge(r, c, Direction.East, false);
                progress |= MarkCellEdge(r, c, Direction.South, false);
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East);
            }
            if (JunctionHasOneOutwardLine(_board.GetJunction(r, c + 1), Direction.North, Direction.East))
            {
                progress |= MarkCellEdge(r, c, Direction.West, false);
                progress |= MarkCellEdge(r, c, Direction.South, false);
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);
            }
            if (JunctionHasOneOutwardLine(_board.GetJunction(r + 1, c), Direction.South, Direction.West))
            {
                progress |= MarkCellEdge(r, c, Direction.East, false);
                progress |= MarkCellEdge(r, c, Direction.North, false);
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);
            }
            if (JunctionHasOneOutwardLine(_board.GetJunction(r + 1, c + 1), Direction.South, Direction.East))
            {
                progress |= MarkCellEdge(r, c, Direction.North, false);
                progress |= MarkCellEdge(r, c, Direction.West, false);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);
            }

            // Two connected edges available, must exit from that corner, single line or infered xor (SM43)
            int junctionR = r, junctionC = c;
            if (HasTwoAdjacentAvailableEdges(cell, ref junctionR, ref junctionC, out int dir1, out int dir2))
            {
                progress |= InferJunctionXor(junctionR, junctionC, dir1, dir2);
                progress |= InferJunctionXor(junctionR, junctionC, Direction.Opposite(dir1), Direction.Opposite(dir2));
            }

            return progress;

            static bool HasTwoAdjacentAvailableEdges(Cell cell, ref int jr, ref int jc, out int dir1, out int dir2)
            {
                dir1 = dir2 = -1;

                var edgeN = cell.Edges[Direction.North];
                var edgeE = cell.Edges[Direction.East];
                var edgeS = cell.Edges[Direction.South];
                var edgeW = cell.Edges[Direction.West];

                var found = false;
                if (edgeN.HasLine.HasValue == false && edgeE.HasLine.HasValue == false)
                {
                    found = true;
                    jc++;
                    dir1 = Direction.North;
                    dir2 = Direction.East;
                }
                if (edgeS.HasLine.HasValue == false && edgeE.HasLine.HasValue == false)
                {
                    if (found) return false; // Can't have more than 1 set
                    found = true;
                    jr++;
                    jc++;
                    dir1 = Direction.South;
                    dir2 = Direction.East;
                }
                if (edgeS.HasLine.HasValue == false && edgeW.HasLine.HasValue == false)
                {
                    if (found) return false; // Can't have more than 1 set
                    found = true;
                    jr++;
                    dir1 = Direction.South;
                    dir2 = Direction.West;
                }
                if (edgeN.HasLine.HasValue == false && edgeW.HasLine.HasValue == false)
                {
                    if (found) return false; // Can't have more than 1 set
                    found = true;
                    dir1 = Direction.North;
                    dir2 = Direction.West;
                }

                return found;
            }
        }

        private bool InferTwos()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= InferTwos(r, c);
                }
            }
            return progress;
        }

        // Opposite corners
        // Two with a line touching opposite corners, both must enter, xor that corner (LH38)
        // - Line into a two's junction with three available edges. Exiting would eliminate two edges from the 2. If that does not leave enough remaining edges, exiting here isn't possible. (LH38 - 7,13)
        // TODO:
        // - one line, one x, two adjacent lines makes an inference
        // - Two diagonal to a three, with three available edges (possibly with intermediate twos). The two edges adjacent connected to the 3 are xor and the 3rd is a line, then xor the 3's corner (SH41)
        private bool InferTwos(int r, int c)
        {
            bool progress = false;
            var cell = _board[r, c];
            if (cell.Hint != 2 || cell.Lines > 1)
            {
                return false;
            }

            var nwJ = _board.GetJunction(r, c);
            var neJ = _board.GetJunction(r, c + 1);
            var swJ = _board.GetJunction(r + 1, c);
            var seJ = _board.GetJunction(r + 1, c + 1);

            // Opposite corners
            if (JunctionHasOneOutwardLine(nwJ, Direction.North, Direction.West))
            {
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);
            }
            if (JunctionHasOneOutwardLine(neJ, Direction.North, Direction.East))
            {
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);
                progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);
            }
            if (JunctionHasOneOutwardLine(swJ, Direction.South, Direction.West))
            {
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);
                progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East);
            }
            if (JunctionHasOneOutwardLine(seJ, Direction.South, Direction.East))
            {
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);
                progress |= InferJunctionXor(r, c, Direction.North, Direction.West);
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East);
            }

            // Two with a line touching opposite corners, both must enter, xor that corner
            if (OppositeCornersHaveLines(nwJ, seJ, Direction.North, Direction.West))
            {
                progress |= InferJunctionXor(r, c, Direction.North, Direction.West);
                progress |= InferJunctionXor(r, c, Direction.South, Direction.East);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);
                progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);
            }
            if (OppositeCornersHaveLines(neJ, swJ, Direction.North, Direction.East))
            {
                progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East);
                progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);
                progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);
                progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);
            }

            return progress;

            static bool OppositeCornersHaveLines(Junction j1, Junction j2, int dir1, int dir2)
            {
                var dir3 = Direction.Opposite(dir1);
                var dir4 = Direction.Opposite(dir2);
                return (j1.Edges[dir1]?.HasLine == true && j1.Edges[dir2] != null && j1.Edges[dir2].HasLine != true
                    || j1.Edges[dir2]?.HasLine == true && j1.Edges[dir1] != null && j1.Edges[dir1].HasLine != true)
                    && (j2.Edges[dir3]?.HasLine == true && j2.Edges[dir4] != null && j2.Edges[dir4].HasLine != true
                    || j2.Edges[dir4]?.HasLine == true && j2.Edges[dir3] != null && j2.Edges[dir3].HasLine != true);
            }
        }

        // - Two diagonal to a three, with three available edges (possibly with intermediate twos). The two edges adjacent connected to the 3 are xor and the 3rd is a line, then xor the 3's corner (MH29, SH41)
        // - (MH29, LH37) pick an available corner. If making that a corner would cut off too many edges from a diagonal cell, then the opposite corner must be lines.
        // TODO: This needs to check for cascades too (SH41).
        private bool InferThrees()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    progress |= InferThrees(r, c);
                }
            }
            return progress;
        }

        // - Two diagonal to a three, with three available edges (possibly with intermediate twos). The two edges adjacent connected to the 3 are xor and the 3rd is a line, then xor the 3's corner (MH29, SH41)
        // - (MH29, LH37) pick an available corner. If making that a corner would cut off too many edges from a diagonal cell, then the opposite corner must be lines.
        // TODO: This needs to check for cascades too (SH41).
        private bool InferThrees(int r, int c)
        {
            bool progress = false;

            var cell = _board[r, c];
            if (cell.Hint != 3 || cell.Lines > 2)
            {
                return false;
            }

            // For each available corner
            // Diagonally adjacent to a hint
            if (IsCornerAvailable(_board.GetJunction(r, c), Direction.South, Direction.East) && GetHint(r - 1, c - 1) is int hintNW) // NW
            {
                // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                var diagonalCell = _board[r - 1, c - 1];
                if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintNW)
                {
                    // If so, infer xor on that corner, and lines on the opposite corner
                    progress |= InferJunctionXor(r, c, Direction.North, Direction.West);
                    progress |= InferJunctionXor(r, c, Direction.South, Direction.East);
                    progress |= MarkCellEdge(r, c, Direction.South, true);
                    progress |= MarkCellEdge(r, c, Direction.East, true);
                }
            }
            if (IsCornerAvailable(_board.GetJunction(r, c + 1), Direction.South, Direction.West) && GetHint(r - 1, c + 1) is int hintNE) // NE
            {
                // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                var diagonalCell = _board[r - 1, c + 1];
                if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintNE)
                {
                    // If so, infer xor on that corner, and lines on the opposite corner
                    progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East);
                    progress |= InferJunctionXor(r, c + 1, Direction.South, Direction.West);
                    progress |= MarkCellEdge(r, c, Direction.South, true);
                    progress |= MarkCellEdge(r, c, Direction.West, true);
                }
            }
            if (IsCornerAvailable(_board.GetJunction(r + 1, c + 1), Direction.North, Direction.West) && GetHint(r + 1, c + 1) is int hintSE) // SE
            {
                // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                var diagonalCell = _board[r + 1, c + 1];
                if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintSE)
                {
                    // If so, infer xor on that corner, and lines on the opposite corner
                    progress |= InferJunctionXor(r + 1, c + 1, Direction.North, Direction.West);
                    progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);
                    progress |= MarkCellEdge(r, c, Direction.North, true);
                    progress |= MarkCellEdge(r, c, Direction.West, true);
                }
            }
            if (IsCornerAvailable(_board.GetJunction(r + 1, c), Direction.North, Direction.East) && GetHint(r + 1, c - 1) is int hintSW) // SW
            {
                // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                var diagonalCell = _board[r + 1, c - 1];
                if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintSW)
                {
                    // If so, infer xor on that corner, and lines on the opposite corner
                    progress |= InferJunctionXor(r + 1, c, Direction.North, Direction.East);
                    progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);
                    progress |= MarkCellEdge(r, c, Direction.North, true);
                    progress |= MarkCellEdge(r, c, Direction.East, true);
                }
            }

            return progress;

            // 0 or 1 lines and 1-2 undetermined. No x's. These are inward edges and shouldn't be null
            // The opposite corners must both be non-null and unassigned. MarkThreesInACorner covers that case.
            static bool IsCornerAvailable(Junction junction, int dir1, int dir2)
            {
                var edge1 = junction.Edges[dir1];
                var edge2 = junction.Edges[dir2];
                var edge3 = junction.Edges[Direction.Opposite(dir1)];
                var edge4 = junction.Edges[Direction.Opposite(dir2)];

                if (edge3 == null || edge4 == null
                    || edge3.HasLine.HasValue
                    || edge4.HasLine.HasValue)
                {
                    return false;
                }

                // At least one unassigned and the other as a line or unassigned, not an x.
                return !edge1.HasLine.HasValue && edge2.HasLine != false
                    || !edge2.HasLine.HasValue && edge1.HasLine != false;
            }
        }

        private int? GetHint(int r, int c)
        {
            if (0 <= r && r < _board.Rows
                && 0 <= c && c < _board.Columns)
            {
                return _board[r, c].Hint;
            }
            return null;
        }

        //  - Line into a hint's junction with three available edges. Exiting would eliminate two edges from the cell. If that does not leave enough remaining edges, exiting here isn't possible. (LH38 - 7,13)
        // - This should cascade. If E.g. HH105 6,22 SE junction can't turn right, otherwise the 2 would cut off the diagonaly 3. Also for HH105 24,18 N, and 18,9 N, and 13,22 SE, and 8,13 SW, 10,8 NW.
        // TODO cascading into 1's 
        private bool InferExit()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    var cell = _board[r, c];
                    if (!cell.Hint.HasValue || cell.Hint == 0)
                    {
                        continue;
                    }
                    // Check if exiting and marking the two cell edges as X would reduce the available cell edges below the hint count.
                    if (cell.Undetermined + cell.Lines - 2 < cell.Hint)
                    {
                        // For each junction
                        // if it has one external line in and three unassigned edges available
                        // If yes, x the exit.
                        if (CheckIfExitingEliminatesTooManyEdges(_board.GetJunction(r, c), Direction.North, Direction.West))
                        {
                            progress |= InferJunctionXor(r, c, Direction.North, Direction.West);
                        }
                        if (CheckIfExitingEliminatesTooManyEdges(_board.GetJunction(r, c + 1), Direction.North, Direction.East))
                        {
                            progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East);
                        }
                        if (CheckIfExitingEliminatesTooManyEdges(_board.GetJunction(r + 1, c), Direction.South, Direction.West))
                        {
                            progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);
                        }
                        if (CheckIfExitingEliminatesTooManyEdges(_board.GetJunction(r + 1, c + 1), Direction.South, Direction.East))
                        {
                            progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);
                        }
                    }
                    // What if it eliminates just the right number of edges, does this cause a cascade? This should only be possible with 2's.
                    if (cell.Hint == 2 && cell.Undetermined >= 2)
                    {
                        // For each junction
                        // if it has one external line in and three unassigned edges available
                        // If yes, x the exit.                        
                        if (CheckForTwoCascade(r, c, _board.GetJunction(r, c), Direction.North, Direction.West))
                        {
                            progress |= InferJunctionXor(r, c, Direction.North, Direction.West);
                        }
                        if (CheckForTwoCascade(r, c, _board.GetJunction(r, c + 1), Direction.North, Direction.East))
                        {
                            progress |= InferJunctionXor(r, c + 1, Direction.North, Direction.East);
                        }
                        if (CheckForTwoCascade(r, c, _board.GetJunction(r + 1, c), Direction.South, Direction.West))
                        {
                            progress |= InferJunctionXor(r + 1, c, Direction.South, Direction.West);
                        }
                        if (CheckForTwoCascade(r, c, _board.GetJunction(r + 1, c + 1), Direction.South, Direction.East))
                        {
                            progress |= InferJunctionXor(r + 1, c + 1, Direction.South, Direction.East);
                        }
                    }
                }
            }
            return progress;

            static bool CheckIfExitingEliminatesTooManyEdges(Junction junction, int dir1, int dir2)
            {
                return junction.EdgeCount == 4 && junction.LineCount == 1 && junction.UnknownCount == 3
                    && (junction.Edges[dir1]?.HasLine == true || junction.Edges[dir2]?.HasLine == true);
            }

            bool CheckForTwoCascade(int r, int c, Junction junction, int dir1, int dir2)
            {
                // Is there one incoming line that can exit?
                if (!(junction.EdgeCount == 4 && junction.LineCount == 1 && junction.UnknownCount >= 2
                    && (junction.Edges[dir1]?.HasLine == true || junction.Edges[dir2]?.HasLine == true)))
                {
                    return false;
                }
                var cell = _board[r, c];
                // Is the next diagonal cell hinted? Would making a corner there cause it to loose too many edges? If it's a two, recurse.
                var dir3 = Direction.Opposite(dir1);
                var dir4 = Direction.Opposite(dir2);
                switch (dir3)
                {
                    case Direction.North:
                        r--;
                        break;
                    case Direction.South:
                        r++;
                        break;
                    case Direction.East:
                        c++;
                        break;
                    case Direction.West:
                        c--;
                        break;
                }
                switch (dir4)
                {
                    case Direction.North:
                        r--;
                        break;
                    case Direction.South:
                        r++;
                        break;
                    case Direction.East:
                        c++;
                        break;
                    case Direction.West:
                        c--;
                        break;
                }

                var nextHint = GetHint(r, c);
                if (nextHint == null) return false; // No hint
                if (nextHint == 3) return true; // Can never take a corner out of a three
                if (nextHint == 2)
                {
                    // Recurse
                    return CheckForTwoCascadeRecursive(r, c, cell.GetJunction(dir3, dir4), dir3, dir4);
                }
                // TODO: 1

                return false;
            }

            bool CheckForTwoCascadeRecursive(int r, int c, Junction junction, int dir1, int dir2)
            {
                // We're inferring that this junction already exited, so make sure there aren't any inward lines from it
                // And that removing those edges wouldn't leave it with too few.
                if (junction.EdgeCount != 4) return false;
                // An inward line, that would conflict with an outward corner
                if (junction.Edges[dir1]?.HasLine == true || junction.Edges[dir2]?.HasLine == true)
                {
                    return true;
                }
                // HH105 18,16 NW. The diagonal 2 is already down an edge, it can't afford to lose two more.
                var cell = _board[r, c];
                /* // TODO: Breaks HH105 21,1
                if (cell.Hint == 2 && cell.Undetermined < 4)
                {
                    return true;
                }*/
                // Is the next diagonal cell hinted? Would making a corner there cause it to loose too many edges? If it's a two, recurse.
                switch (dir1)
                {
                    case Direction.North:
                        r--;
                        break;
                    case Direction.South:
                        r++;
                        break;
                    case Direction.East:
                        c++;
                        break;
                    case Direction.West:
                        c--;
                        break;
                }
                switch (dir2)
                {
                    case Direction.North:
                        r--;
                        break;
                    case Direction.South:
                        r++;
                        break;
                    case Direction.East:
                        c++;
                        break;
                    case Direction.West:
                        c--;
                        break;
                }

                var nextHint = GetHint(r, c);
                if (nextHint == null) return false; // No hint
                if (nextHint == 3) return true; // Can never take a corner out of a three
                if (nextHint == 2)
                {
                    // Recurse
                    return CheckForTwoCascadeRecursive(r, c, cell.GetJunction(dir1, dir2), dir1, dir2);
                }
                // TODO: 1

                return false;
            }
        }

        // One outward edge is a line and the other doesn't exist or is an X
        private static bool JunctionHasOneOutwardLine(Junction junction, int direction1, int direction2, bool allowUnknown = false)
        {
            var edge1 = junction.Edges[direction1];
            var edge2 = junction.Edges[direction2];
            if (edge1?.HasLine == true
                && (edge2 == null || edge2.HasLine == false
                || (allowUnknown && !edge2.HasLine.HasValue)))
            {
                return true;
            }
            else if (edge2?.HasLine == true 
                && (edge1 == null || edge1.HasLine == false
                || (allowUnknown && !edge1.HasLine.HasValue)))
            {
                return true;
            }

            foreach (var inference in junction.Inferences)
            {
                if (inference.Equals(direction1, direction2))
                {
                    return true;
                }
            }

            return false;
        }

        // A corner must have two edges in that direction unavailable (x or missing / off board)
        // and two edges in the oppositie direction available (line or unselected)
        private static bool IsCorner(Cell cell, int direction1, int direction2)
        {
            var junction = cell.Edges[direction1].Junctions[direction2 % 2];

            var dir1Edge = junction.Edges[direction1]; // Cound be null, off the board
            var dir2Edge = junction.Edges[direction2]; // Cound be null, off the board
            var dir1OppositeEdge = junction.Edges[Direction.Opposite(direction1)]; // Shouldn't be null
            var dir2OppositeEdge = junction.Edges[Direction.Opposite(direction2)]; // Shouldn't be null

            return (dir1Edge == null || dir1Edge.HasLine == false)
                && (dir2Edge == null || dir2Edge.HasLine == false)
                && dir1OppositeEdge.HasLine != false
                && dir2OppositeEdge.HasLine != false;
        }

        // If the given corner only has one outgoing possible line, mark it.
        private bool MarkOutgoingSingleLine(int row, int column, int direction1, int direction2, bool? value)
        {
            var junction = _board.GetJunction(row, column);
            var edge1 = junction.Edges[direction1];
            var edge2 = junction.Edges[direction2];

            if ((edge1 == null || edge1.HasLine == false) && edge2 != null)
            {
                return MarkJunctionEdge(row, column, direction2, value);
            }
            else if ((edge2 == null || edge2.HasLine == false) && edge1 != null)
            {
                return MarkJunctionEdge(row, column, direction1, value);
            }

            return false;
        }

        private bool InferJunctionXor(int row, int column, int direction1, int direction2)
        {
            var junction = _board.GetJunction(row, column);
            var edge1 = junction.Edges[direction1];
            var edge2 = junction.Edges[direction2];

            if ((edge1 == null || edge1.HasLine == false) && edge2 != null)
            {
                return MarkJunctionEdge(row, column, direction2, true);
            }
            
            if ((edge2 == null || edge2.HasLine == false) && edge1 != null)
            {
                return MarkJunctionEdge(row, column, direction1, true);
            }

            foreach (var inference in junction.Inferences)
            {
                if (inference.Equals(direction1, direction2))
                {
                    return false;
                }
            }

            junction.Inferences.Add(new InferenceXor(direction1, direction2));

            return true;
        }

        private bool CheckInferences()
        {
            bool progress = false;
            for (var r = 0; r <= _board.Rows; r++)
            {
                for (var c = 0; c <= _board.Columns; c++)
                {
                    progress |= CheckInferences(r, c);
                }
            }
            return progress;
        }

        private bool CheckInferences(int r, int c)
        {
            bool progress = false;

            var junction = _board.GetJunction(r, c);
            foreach (var inference in junction.Inferences)
            {
                var edge1 = junction.Edges[inference.Direction1];
                var edge2 = junction.Edges[inference.Direction2];

                if ((edge1 == null || edge1.HasLine.HasValue) && edge2 != null)
                {
                    progress |= MarkJunctionEdge(r, c, inference.Direction2, !edge1?.HasLine ?? true);
                }

                if ((edge2 == null || edge2.HasLine.HasValue) && edge1 != null)
                {
                    progress |= MarkJunctionEdge(r, c, inference.Direction1, !edge2?.HasLine ?? true);
                }
            }

            return progress;
        }

        // (SH42)
        // (LH37) When there's two adjacent exits from one junciton, infer xor (then make sure infer2 cascades xors)
        // TODO:
        // - Consider xor inferences as fixed entry/exit points or lack thereof (SH43)
        // - HH105 19,16 - inferred xor entrance, can x the other exit
        private bool CheckSingleCellParity()
        {
            bool progress = false;
            for (var r = 0; r < _board.Rows; r++)
            {
                for (var c = 0; c < _board.Columns; c++)
                {
                    var cell = _board[r, c];

                    // Does the cell have available edges?
                    if (cell.Undetermined == 0)
                    {
                        continue;
                    }

                    var nwJunction = _board.GetJunction(r, c);
                    var neJunction = _board.GetJunction(r, c + 1);
                    var seJunction = _board.GetJunction(r + 1, c + 1);
                    var swJunction = _board.GetJunction(r + 1, c);

                    // Count the incomplete lines inside and out
                    var lines = CountIncompleteLines(nwJunction, Direction.North, Direction.West)
                        + CountIncompleteLines(neJunction, Direction.North, Direction.East)
                        + CountIncompleteLines(seJunction, Direction.South, Direction.East)
                        + CountIncompleteLines(swJunction, Direction.South, Direction.West);

                    if (lines == 0)
                    {
                        continue;
                    }

                    // Is there one junction with a one or two available edges?
                    var nwAvailable = CountAvailableEdges(nwJunction, Direction.North, Direction.West);
                    var neAvailable = CountAvailableEdges(neJunction, Direction.North, Direction.East);
                    var seAvailable = CountAvailableEdges(seJunction, Direction.South, Direction.East);
                    var swAvailable = CountAvailableEdges(swJunction, Direction.South, Direction.West);

                    var availableExits = nwAvailable + neAvailable + seAvailable + swAvailable;

                    if (availableExits < 1 || 2 < availableExits) continue;


                    // If they're two, are they both on the same junction?
                    int exitJunctionR, exitJunctionC, dir1, dir2;
                    if (availableExits == nwAvailable)
                    {
                        exitJunctionR = r;
                        exitJunctionC = c;
                        dir1 = Direction.North;
                        dir2 = Direction.West;
                    }
                    else if (availableExits == neAvailable)
                    {
                        exitJunctionR = r;
                        exitJunctionC = c + 1;
                        dir1 = Direction.North;
                        dir2 = Direction.East;
                    }
                    else if (availableExits == seAvailable)
                    {
                        exitJunctionR = r + 1;
                        exitJunctionC = c + 1;
                        dir1 = Direction.South;
                        dir2 = Direction.East;
                    }
                    else if (availableExits == swAvailable)
                    {
                        exitJunctionR = r + 1;
                        exitJunctionC = c;
                        dir1 = Direction.South;
                        dir2 = Direction.West;
                    }
                    else
                    {
                        // It  could have been two edges spread across multiple junctions
                        continue;
                    }

                    // For an even number of available edges, x the exit
                    // For an odd number, mark the exit as a line
                    var even = lines % 2 == 0;
                    if (!even && availableExits == 2)
                    {
                        // Must exit one of these ways
                        progress |= InferJunctionXor(exitJunctionR, exitJunctionC, dir1, dir2);
                        progress |= InferJunctionXor(exitJunctionR, exitJunctionC, Direction.Opposite(dir1), Direction.Opposite(dir2));
                    }
                    else if (even && availableExits == 2)
                    {
                        // Can't go either way
                        // TODO: it could be an inverse corner (if none of the current lines connects to this junction?)
                        if (_board.GetJunction(exitJunctionR, exitJunctionC).CountLines() == 1)
                        {
                            // TODO: This case seems to be covered by other inferences
                            // progress |= game.MarkJunctionEdge(exitJunctionR, exitJunctionC, dir1, false);
                            // progress |= game.MarkJunctionEdge(exitJunctionR, exitJunctionC, dir2, false);
                        }
                    }
                    else
                    {
                        Debug.Assert(availableExits == 1); // Might be 1 xor with two actual possible lines
                        // Which edge was it?
                        var exitJunction = _board.GetJunction(exitJunctionR, exitJunctionC);
                        var edge1 = exitJunction.Edges[dir1];
                        var edge2 = exitJunction.Edges[dir2];

                        if (edge1?.HasLine.HasValue == false && edge2?.HasLine.HasValue == true)
                        {
                            progress |= MarkJunctionEdge(exitJunctionR, exitJunctionC, dir1, !even);
                        }
                        else if (edge1?.HasLine.HasValue == true && edge2?.HasLine.HasValue == false)
                        {
                            progress |= MarkJunctionEdge(exitJunctionR, exitJunctionC, dir2, !even);
                        }
                    }
                }
            }
            return progress;

            static int CountAvailableEdges(Junction junction, int dir1, int dir2)
            {
                var count = 0;
                // may be null
                var edge1 = junction.Edges[dir1];
                var edge2 = junction.Edges[dir2];

                if (edge1?.HasLine.HasValue == false)
                {
                    count++;
                }
                if (edge2?.HasLine.HasValue == false)
                {
                    count++;
                }
                /*
                foreach (var inference in junction.Inferences)
                {
                    // xor
                    if (inference.Equals(dir1, dir2) && count == 2)
                    {
                        return 0;
                    }
                }
                */
                return count;
            }

            // 0 or 1
            static int CountIncompleteLines(Junction junction, int dir1, int dir2)
            {
                // There's at most one line into this intersection, otherwise we can't use it for an exit.
                var lines = junction.CountLines();
                /* TODO
                foreach (var inference in junction.Inferences)
                {
                    // xor
                    if (inference.Equals(dir1, dir2))
                    {
                        return lines switch
                        {
                            0 => 1,
                            1 => 0,
                            _ => 0,
                        };
                    }
                }*/
                return lines switch
                {
                    0 => 0,
                    1 => 1,
                    2 => 0,
                    _ => throw new InvalidOperationException()
                };
            }
        }
    }
}
