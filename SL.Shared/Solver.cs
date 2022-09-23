﻿using SL.Shared.Structures;
using System;
using System.Diagnostics;

namespace SL.Shared
{
    public static class Solver
    {
        public static bool Solve(Game game)
        {
            // MarkZeros(game); // Generalized by MarkLinesMatchingHints

            MarkAdjacentThrees(game);

            // MarkDiagonalThrees(game); // Generalized by InferThrees

            do
            {
                if (MarkLinesMatchingHints(game)) continue;
                if (MarkDeadEnds(game)) continue;
                if (ExtendLines(game)) continue;
                if (MarkOnesInACorner(game)) continue;
                if (MarkThreesInACorner(game)) continue;
                if (MarkThreesWithIncomingLines(game)) continue;
                if (MarkTwosInACorner(game)) continue;
                if (InferOnes(game)) continue;
                if (InferTwos(game)) continue;
                if (InferThrees(game)) continue;
                if (InferExit(game)) continue;
                if (CheckInferences(game)) continue;
                if (CheckSingleCellParity(game)) continue;
                if (DetectLoops(game)) continue;

                // Ones next to threes on an edge

                // Check parity, Isolated reagons (LH37)
                // - Can't close off a section from the rest of the board
                // - If a cell only has one possible external exit, and there's an odd number of loose ends, the line that can exit must.
                break;
            } while (true);

            return IsSolved(game);
        }

        private static bool IsSolved(Game game)
        {
            // Are all hints satisfied
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint.HasValue)
                    {
                        if (cell.Lines != cell.Hint) return false;
                    }
                }
            }

            // TODO:
            // Do all lines belong to one continuous loop

            return true;
        }
        /*
        private static void MarkZeros(Game game)
        {
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint == 0)
                    {
                        game.MarkCellEdge(r, c, Direction.North, false);
                        game.MarkCellEdge(r, c, Direction.East, false);
                        game.MarkCellEdge(r, c, Direction.South, false);
                        game.MarkCellEdge(r, c, Direction.West, false);
                    }
                }
            }
        }
        */
        private static void MarkAdjacentThrees(Game game)
        {
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint == 3)
                    {
                        // Since we check every cell we only need to look east and south.

                        // East
                        if (c < board.Columns - 1 && board[r, c + 1].Hint == 3)
                        {
                            game.MarkCellEdge(r, c, Direction.West, true);
                            game.MarkCellEdge(r, c, Direction.East, true);
                            game.MarkCellEdge(r, c + 1, Direction.East, true);

                            // Also x the north and south east edged as innaccessible

                            if (r > 0)
                            {
                                game.MarkCellEdge(r - 1, c, Direction.East, false);
                            }

                            if (r < board.Rows - 1)
                            {
                                game.MarkCellEdge(r + 1, c, Direction.East, false);
                            }
                        }

                        // South
                        if (r < board.Rows - 1 && board[r + 1, c].Hint == 3)
                        {
                            game.MarkCellEdge(r, c, Direction.North, true);
                            game.MarkCellEdge(r, c, Direction.South, true);
                            game.MarkCellEdge(r + 1, c, Direction.South, true);

                            // Also x the east and west south edged as innaccessible

                            if (c > 0)
                            {
                                game.MarkCellEdge(r, c - 1, Direction.South, false);
                            }

                            if (c < board.Columns - 1)
                            {
                                game.MarkCellEdge(r, c + 1, Direction.South, false);
                            }
                        }
                    }
                }
            }
        }
        /*
        // Diagonal three's have lines on their outside corners.
        // TODO:
        // - This also happens if there is a consecutive diagonal of 2s in between
        private static void MarkDiagonalThrees(Game game)
        {
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint == 3)
                    {
                        // Since we check every cell we only need to look north east and south east.

                        // North East
                        if (r > 0 && c < board.Columns - 1 && board[r - 1, c + 1].Hint == 3)
                        {
                            game.MarkCellEdge(r, c, Direction.West, true);
                            game.MarkCellEdge(r, c, Direction.South, true);
                            game.MarkCellEdge(r - 1, c + 1, Direction.North, true);
                            game.MarkCellEdge(r - 1, c + 1, Direction.East, true);
                            InferJunctionXor(game, r, c + 1, Direction.North, Direction.East);
                            InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);
                        }

                        // South East
                        if (r < board.Rows - 1 && c < board.Columns - 1 && board[r + 1, c + 1].Hint == 3)
                        {
                            game.MarkCellEdge(r, c, Direction.West, true);
                            game.MarkCellEdge(r, c, Direction.North, true);
                            game.MarkCellEdge(r + 1, c + 1, Direction.South, true);
                            game.MarkCellEdge(r + 1, c + 1, Direction.East, true);
                            InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);
                            InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);
                        }
                    }
                }
            }
        }
        */
        // Mark 3s with a line entering, opposite corner must have lines
        // - the other line possible exit line can be x'd. (covered by inference)
        private static bool MarkThreesWithIncomingLines(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 3)
                    {
                        continue;
                    }

                    // Check each junction. If it has an external line then mark the opposite edges as lines.

                    // NW
                    if (JunctionHasOneOutwardLine(board.GetJunction(r, c), Direction.North, Direction.West, allowUnknown: true))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        progress |= game.MarkCellEdge(r, c, Direction.South, true);
                    }
                    // NE
                    if (JunctionHasOneOutwardLine(board.GetJunction(r, c + 1), Direction.North, Direction.East, allowUnknown: true))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.South, true);
                        progress |= game.MarkCellEdge(r, c, Direction.West, true);
                    }
                    // SE
                    if (JunctionHasOneOutwardLine(board.GetJunction(r + 1, c + 1), Direction.South, Direction.East, allowUnknown: true))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.North, true);
                        progress |= game.MarkCellEdge(r, c, Direction.West, true);
                    }
                    // SW
                    if (JunctionHasOneOutwardLine(board.GetJunction(r + 1, c), Direction.South, Direction.West, allowUnknown: true))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        progress |= game.MarkCellEdge(r, c, Direction.North, true);
                    }
                }
            }

            return progress;
        }

        // X 1's edges in a corner
        // - other lines are xOr
        private static bool MarkOnesInACorner(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 1)
                    {
                        continue;
                    }

                    if (IsCorner(cell, Direction.North, Direction.East))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.North, false);
                        progress |= game.MarkCellEdge(r, c, Direction.East, false);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East); // SW
                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West); // SW
                    }

                    if (IsCorner(cell, Direction.South, Direction.East))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.South, false);
                        progress |= game.MarkCellEdge(r, c, Direction.East, false);
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East); // NW
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West); // NW
                    }

                    if (IsCorner(cell, Direction.South, Direction.West))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.South, false);
                        progress |= game.MarkCellEdge(r, c, Direction.West, false);
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West); // NE
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East); // NE
                    }

                    if (IsCorner(cell, Direction.North, Direction.West))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.North, false);
                        progress |= game.MarkCellEdge(r, c, Direction.West, false);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West); // SE
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East); // SE
                    }
                }
            }
            return progress;
        }

        // Mark 3s in corners
        // - opposite corner is xor
        private static bool MarkThreesInACorner(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 3)
                    {
                        continue;
                    }

                    if (IsCorner(cell, Direction.North, Direction.East))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.North, true);
                        progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East); // SW
                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West); // SW
                    }

                    if (IsCorner(cell, Direction.South, Direction.East))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.South, true);
                        progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West); // NW
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East); // NW
                    }

                    if (IsCorner(cell, Direction.South, Direction.West))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.South, true);
                        progress |= game.MarkCellEdge(r, c, Direction.West, true);
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East); // NE
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West); // NE
                    }

                    if (IsCorner(cell, Direction.North, Direction.West))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.North, true);
                        progress |= game.MarkCellEdge(r, c, Direction.West, true);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West); // SE
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East); // SE
                    }
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
        private static bool MarkTwosInACorner(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 2)
                    {
                        continue;
                    }

                    if (IsCorner(cell, Direction.North, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West); // NW c
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);

                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);  // SE c
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);

                        progress |= MarkOutgoingSingleLine(game, r + 1, c, Direction.South, Direction.West, false);
                        if (IsAThree(game, r + 1, c - 1)
                            || JunctionHasOneOutwardLine(board.GetJunction(r + 1, c), Direction.South, Direction.West, allowUnknown: true)) // SW
                        {
                            progress |= game.MarkCellEdge(r, c, Direction.North, true);
                            progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        }
                    }

                    if (IsCorner(cell, Direction.South, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East); // NE c
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);

                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West);  // SW c
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);

                        progress |= MarkOutgoingSingleLine(game, r, c, Direction.North, Direction.West, false);
                        if (IsAThree(game, r - 1, c - 1)
                            || JunctionHasOneOutwardLine(board.GetJunction(r, c), Direction.North, Direction.West, allowUnknown: true)) // NW
                        {
                            progress |= game.MarkCellEdge(r, c, Direction.South, true);
                            progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        }
                    }

                    if (IsCorner(cell, Direction.South, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West); // NW c
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);

                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);  // SE c
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);

                        progress |= MarkOutgoingSingleLine(game, r, c + 1, Direction.North, Direction.East, false);
                        if (IsAThree(game, r - 1, c + 1)
                            || JunctionHasOneOutwardLine(board.GetJunction(r, c + 1), Direction.North, Direction.East, allowUnknown: true)) // NE
                        {
                            progress |= game.MarkCellEdge(r, c, Direction.South, true);
                            progress |= game.MarkCellEdge(r, c, Direction.West, true);
                        }
                    }

                    if (IsCorner(cell, Direction.North, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East); // NE c
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);

                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West);  // SW c
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);

                        progress |= MarkOutgoingSingleLine(game, r + 1, c + 1, Direction.South, Direction.East, false);
                        if (IsAThree(game, r + 1, c + 1)
                            || JunctionHasOneOutwardLine(board.GetJunction(r + 1, c + 1), Direction.South, Direction.East, allowUnknown: true)) // SE
                        {
                            progress |= game.MarkCellEdge(r, c, Direction.North, true);
                            progress |= game.MarkCellEdge(r, c, Direction.West, true);
                        }
                    }
                }
            }

            return progress;

            static bool IsAThree(Game game, int r, int c)
            {
                return r >= 0 && r < game.Board.Rows && c >= 0 && c < game.Board.Columns && game.Board[r, c].Hint == 3;
            }
        }

        // X edges that dead end
        private static bool MarkDeadEnds(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r <= board.Rows; r++)
            {
                for (var c = 0; c <= board.Columns; c++)
                {
                    var junction = board.GetJunction(r, c);
                    var north = junction.Edges[Direction.North];
                    var south = junction.Edges[Direction.South];
                    var east = junction.Edges[Direction.East];
                    var west = junction.Edges[Direction.West];

                    var northIsAlive = north != null && north.HasLine != false;
                    var southIsAlive = south != null && south.HasLine != false;
                    var eastIsAlive = east != null && east.HasLine != false;
                    var westIsAlive = west != null && west.HasLine != false;
                    var totalAlive = (northIsAlive ? 1 : 0) + (southIsAlive ? 1 : 0) + (eastIsAlive ? 1 : 0) + (westIsAlive ? 1 : 0);

                    var northHasLine = north?.HasLine == true;
                    var southHasLine = south?.HasLine == true;
                    var eastHasLine = east?.HasLine == true;
                    var westHasLine = west?.HasLine == true;
                    var totalLines = (northHasLine ? 1 : 0) + (southHasLine ? 1 : 0) + (eastHasLine ? 1 : 0) + (westHasLine ? 1 : 0);

                    if (totalAlive == 1)
                    {
                        // No exit, x out the reaming edge
                        if (north != null && !north.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.North, false);
                        }
                        else if (south != null && !south.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.South, false);
                        }
                        else if (east != null && !east.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.East, false);
                        }
                        else if (west != null && !west.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.West, false);
                        }
                    }
                    else if (totalLines == 2)
                    {
                        // The line has already entered and exited this intercection. Remaining edges can be x'd.
                        if (north != null && !north.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.North, false);
                        }
                        if (south != null && !south.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.South, false);
                        }
                        if (east != null && !east.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.East, false);
                        }
                        if (west != null && !west.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.West, false);
                        }
                    }
                }
            }
            return progress;
        }

        // When all marked or unknown edges total to match the hint
        // OR when all known lines match the hint
        private static bool MarkLinesMatchingHints(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint.HasValue)
                    {
                        var north = cell.Edges[Direction.North];
                        var south = cell.Edges[Direction.South];
                        var east = cell.Edges[Direction.East];
                        var west = cell.Edges[Direction.West];
                        var availableEdges = cell.Lines + cell.Undetermined;
                        var totalLines = cell.Lines;

                        // Possible edges matches hit, mark them all as lines
                        if (availableEdges == cell.Hint)
                        {
                            if (!north.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.North, true);
                            }
                            if (!south.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.South, true);
                            }
                            if (!east.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.East, true);
                            }
                            if (!west.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.West, true);
                            }
                        }
                        // There are already enough lines to satisfy the hint, x out the remainder
                        else if (totalLines == cell.Hint)
                        {
                            if (!north.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.North, false);
                            }
                            if (!south.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.South, false);
                            }
                            if (!east.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.East, false);
                            }
                            if (!west.HasLine.HasValue)
                            {
                                progress |= game.MarkCellEdge(r, c, Direction.West, false);
                            }
                        }
                    }
                }
            }
            return progress;
        }

        // Extend lines with only one option
        private static bool ExtendLines(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r <= board.Rows; r++)
            {
                for (var c = 0; c <= board.Columns; c++)
                {
                    var junction = board.GetJunction(r, c);
                    var north = junction.Edges[Direction.North];
                    var south = junction.Edges[Direction.South];
                    var east = junction.Edges[Direction.East];
                    var west = junction.Edges[Direction.West];
                    var northIsDead = north == null || north.HasLine == false;
                    var southIsDead = south == null || south.HasLine == false;
                    var eastIsDead = east == null || east.HasLine == false;
                    var westIsDead = west == null || west.HasLine == false;
                    var totalAlive = (northIsDead ? 0 : 1) + (southIsDead ? 0 : 1) + (eastIsDead ? 0 : 1) + (westIsDead ? 0 : 1);

                    var northHasLine = north?.HasLine == true;
                    var southHasLine = south?.HasLine == true;
                    var eastHasLine = east?.HasLine == true;
                    var westHasLine = west?.HasLine == true;
                    var totalLines = (northHasLine ? 1 : 0) + (southHasLine ? 1 : 0) + (eastHasLine ? 1 : 0) + (westHasLine ? 1 : 0);

                    if (totalAlive == 2 && totalLines == 1)
                    {
                        // Two possible lines and one is marked. Mark the other.
                        if (north != null && !north.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.North, true);
                        }
                        else if (south != null && !south.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.South, true);
                        }
                        else if (east != null && !east.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.East, true);
                        }
                        else if (west != null && !west.HasLine.HasValue)
                        {
                            progress |= game.MarkJunctionEdge(r, c, Direction.West, true);
                        }
                    }
                }
            }
            return progress;
        }

        // Check if making an edge a line would close a loop.
        // Mark X if this this is a sub loop.
        // TODO: Mark as a line if all clues have/would be satisfied and this ends the game.
        private static bool DetectLoops(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r <= board.Rows; r++)
            {
                for (var c = 0; c <= board.Columns; c++)
                {
                    var junction = board.GetJunction(r, c);
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
                        progress |= DetectLoop(game, east, r, c, Direction.East);
                    }

                    if (south != null && !south.HasLine.HasValue)
                    {
                        progress |= DetectLoop(game, south, r, c, Direction.South);
                    }
                }
            }
            return progress;
        }

        // Trace if marking this edge would create a loop.
        private static bool DetectLoop(Game game, Edge edge, int row, int column, int direction)
        {
            var startJunction = edge.Junctions[0];
            var endJunction = edge.Junctions[1];

            if (!JunctionHasOneLine(startJunction) || !JunctionHasOneLine(endJunction))
            {
                // These two can't be connected right now.
                return false;
            }

            if (FollowLine(startJunction) == endJunction)
            {
                // Found a loop, x out this edge
                return game.MarkJunctionEdge(row, column, direction, false);
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

        // - One line entering a 1, x the opposite edges (LH37)
        //  - Mark diagonal 1's, 2s, 3s. (covered by inference)
        // - Two connected edges available, must exit from that corner, single line or infered xor (SM43)
        private static bool InferOnes(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 1 || cell.Lines > 0)
                    {
                        continue;
                    }

                    if (JunctionHasOneOutwardLine(board.GetJunction(r, c), Direction.North, Direction.West))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.East, false);
                        progress |= game.MarkCellEdge(r, c, Direction.South, false);
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);
                    }
                    if (JunctionHasOneOutwardLine(board.GetJunction(r, c + 1), Direction.North, Direction.East))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.West, false);
                        progress |= game.MarkCellEdge(r, c, Direction.South, false);
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);
                    }
                    if (JunctionHasOneOutwardLine(board.GetJunction(r + 1, c), Direction.South, Direction.West))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.East, false);
                        progress |= game.MarkCellEdge(r, c, Direction.North, false);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);
                    }
                    if (JunctionHasOneOutwardLine(board.GetJunction(r + 1, c + 1), Direction.South, Direction.East))
                    {
                        progress |= game.MarkCellEdge(r, c, Direction.North, false);
                        progress |= game.MarkCellEdge(r, c, Direction.West, false);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);
                    }

                    // Two connected edges available, must exit from that corner, single line or infered xor (SM43)
                    int junctionR = r, junctionC = c;
                    if (HasTwoAdjacentAvailableEdges(cell, ref junctionR, ref junctionC, out int dir1, out int dir2))
                    {
                        progress |= InferJunctionXor(game, junctionR, junctionC, dir1, dir2);
                        progress |= InferJunctionXor(game, junctionR, junctionC, Direction.Opposite(dir1), Direction.Opposite(dir2));
                    }
                }
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

        // Opposite corners
        // Two with a line touching opposite corners, both must enter, xor that corner (LH38)
        // TODO:
        // - one line, one x, two adjacent lines makes an inference
        // - Two diagonal to a three, with three available edges (possibly with intermediate twos). The two edges adjacent connected to the 3 are xor and the 3rd is a line, then xor the 3's corner (MH29, SH41)
        // - Line into a two's junction with three available edges. Exiting would eliminate two edges from the 2. If that does not leave enough remaining edges, exiting here isn't possible. (LH38 - 7,13)
        private static bool InferTwos(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 2 || cell.Lines > 1)
                    {
                        continue;
                    }

                    var nwJ = board.GetJunction(r, c);
                    var neJ = board.GetJunction(r, c + 1);
                    var swJ = board.GetJunction(r + 1, c);
                    var seJ = board.GetJunction(r + 1, c + 1);

                    // Opposite corners
                    if (JunctionHasOneOutwardLine(nwJ, Direction.North, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);
                    }
                    if (JunctionHasOneOutwardLine(neJ, Direction.North, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West);
                    }
                    if (JunctionHasOneOutwardLine(swJ, Direction.South, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East);
                    }
                    if (JunctionHasOneOutwardLine(seJ, Direction.South, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West);
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);
                    }

                    // Two with a line touching opposite corners, both must enter, xor that corner
                    if (OppositeCornersHaveLines(nwJ, seJ, Direction.North, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West);
                        progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);
                    }
                    if (OppositeCornersHaveLines(neJ, swJ, Direction.North, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East);
                        progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);
                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West);
                    }
                }
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
        private static bool InferThrees(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    if (cell.Hint != 3 || cell.Lines > 2)
                    {
                        continue;
                    }

                    // For each available corner
                    // Diagonally adjacent to a hint
                    if (IsCornerAvailable(board.GetJunction(r, c), Direction.South, Direction.East) && GetHint(board, r - 1, c - 1) is int hintNW) // NW
                    {
                        // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                        var diagonalCell = board[r - 1, c - 1];
                        if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintNW)
                        {
                            // If so, infer xor on that corner, and lines on the opposite corner
                            progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West);
                            progress |= InferJunctionXor(game, r, c, Direction.South, Direction.East);
                            progress |= game.MarkCellEdge(r, c, Direction.South, true);
                            progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        }
                    }
                    if (IsCornerAvailable(board.GetJunction(r, c + 1), Direction.South, Direction.West) && GetHint(board, r - 1, c + 1) is int hintNE) // NE
                    {
                        // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                        var diagonalCell = board[r - 1, c + 1];
                        if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintNE)
                        {
                            // If so, infer xor on that corner, and lines on the opposite corner
                            progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East);
                            progress |= InferJunctionXor(game, r, c + 1, Direction.South, Direction.West);
                            progress |= game.MarkCellEdge(r, c, Direction.South, true);
                            progress |= game.MarkCellEdge(r, c, Direction.West, true);
                        }
                    }
                    if (IsCornerAvailable(board.GetJunction(r + 1, c + 1), Direction.North, Direction.West) && GetHint(board, r + 1, c + 1) is int hintSE) // SE
                    {
                        // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                        var diagonalCell = board[r + 1, c + 1];
                        if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintSE)
                        {
                            // If so, infer xor on that corner, and lines on the opposite corner
                            progress |= InferJunctionXor(game, r + 1, c + 1, Direction.North, Direction.West);
                            progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);
                            progress |= game.MarkCellEdge(r, c, Direction.North, true);
                            progress |= game.MarkCellEdge(r, c, Direction.West, true);
                        }
                    }
                    if (IsCornerAvailable(board.GetJunction(r + 1, c), Direction.North, Direction.East) && GetHint(board, r + 1, c - 1) is int hintSW) // SW
                    {
                        // check if making that a corner would reduce the diagonally adjacent cell below it's hint count. E.g. it would remove two possible edges.
                        var diagonalCell = board[r + 1, c - 1];
                        if (diagonalCell.Lines + diagonalCell.Undetermined - 2 < hintSW)
                        {
                            // If so, infer xor on that corner, and lines on the opposite corner
                            progress |= InferJunctionXor(game, r + 1, c, Direction.North, Direction.East);
                            progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West);
                            progress |= game.MarkCellEdge(r, c, Direction.North, true);
                            progress |= game.MarkCellEdge(r, c, Direction.East, true);
                        }
                    }
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

            static int? GetHint(Board board, int r, int c)
            {
                if (0 <= r && r < board.Rows
                    && 0 <= c && c < board.Columns)
                {
                    return board[r, c].Hint;
                }
                return null;
            }
        }

        //  - Line into a hint's junction with three available edges. Exiting would eliminate two edges from the cell. If that does not leave enough remaining edges, exiting here isn't possible. (LH38 - 7,13)
        private static bool InferExit(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    // Check if exiting and marking the two cell edges as X would reduce the available cell edges below the hint count.
                    if (!(cell.Hint > 0) || cell.Undetermined + cell.Lines - 2 >= cell.Hint)
                    {
                        continue;
                    }

                    // For each junction
                    // if it has one external line in and three unassigned edges available
                    // If yes, x the exit.
                    if (CheckIfExitingEliminatesTooManyEdges(board.GetJunction(r, c), Direction.North, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r, c, Direction.North, Direction.West);
                    }
                    if (CheckIfExitingEliminatesTooManyEdges(board.GetJunction(r, c + 1), Direction.North, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r, c + 1, Direction.North, Direction.East);
                    }
                    if (CheckIfExitingEliminatesTooManyEdges(board.GetJunction(r + 1, c), Direction.South, Direction.West))
                    {
                        progress |= InferJunctionXor(game, r + 1, c, Direction.South, Direction.West);
                    }
                    if (CheckIfExitingEliminatesTooManyEdges(board.GetJunction(r + 1, c + 1), Direction.South, Direction.East))
                    {
                        progress |= InferJunctionXor(game, r + 1, c + 1, Direction.South, Direction.East);
                    }
                }
            }
            return progress;

            static bool CheckIfExitingEliminatesTooManyEdges(Junction junction, int dir1, int dir2)
            {
                return junction.EdgeCount == 4 && junction.LineCount == 1 && junction.UnknownCount == 3
                    && (junction.Edges[dir1]?.HasLine == true || junction.Edges[dir2]?.HasLine == true);
            }
        }

        private static bool JunctionHasOneLine(Junction junction)
        {
            var north = junction.Edges[Direction.North];
            var south = junction.Edges[Direction.South];
            var east = junction.Edges[Direction.East];
            var west = junction.Edges[Direction.West];

            var northHasLine = north?.HasLine == true;
            var southHasLine = south?.HasLine == true;
            var eastHasLine = east?.HasLine == true;
            var westHasLine = west?.HasLine == true;
            var totalLines = (northHasLine ? 1 : 0) + (southHasLine ? 1 : 0) + (eastHasLine ? 1 : 0) + (westHasLine ? 1 : 0);
            return totalLines == 1;
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
        private static bool MarkOutgoingSingleLine(Game game, int row, int column, int direction1, int direction2, bool? value)
        {
            var junction = game.Board.GetJunction(row, column);
            var edge1 = junction.Edges[direction1];
            var edge2 = junction.Edges[direction2];

            if ((edge1 == null || edge1.HasLine == false) && edge2 != null)
            {
                return game.MarkJunctionEdge(row, column, direction2, value);
            }
            else if ((edge2 == null || edge2.HasLine == false) && edge1 != null)
            {
                return game.MarkJunctionEdge(row, column, direction1, value);
            }

            return false;
        }

        private static bool InferJunctionXor(Game game, int row, int column, int direction1, int direction2)
        {
            var junction = game.Board.GetJunction(row, column);
            var edge1 = junction.Edges[direction1];
            var edge2 = junction.Edges[direction2];

            if ((edge1 == null || edge1.HasLine == false) && edge2 != null)
            {
                return game.MarkJunctionEdge(row, column, direction2, true);
            }
            
            if ((edge2 == null || edge2.HasLine == false) && edge1 != null)
            {
                return game.MarkJunctionEdge(row, column, direction1, true);
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

        private static bool CheckInferences(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r <= board.Rows; r++)
            {
                for (var c = 0; c <= board.Columns; c++)
                {
                    var junction = board.GetJunction(r, c);
                    foreach (var inference in junction.Inferences)
                    {
                        var edge1 = junction.Edges[inference.Direction1];
                        var edge2 = junction.Edges[inference.Direction2];

                        if ((edge1 == null || edge1.HasLine.HasValue) && edge2 != null)
                        {
                            progress |= game.MarkJunctionEdge(r, c, inference.Direction2, !edge1?.HasLine ?? true);
                        }

                        if ((edge2 == null || edge2.HasLine.HasValue) && edge1 != null)
                        {
                            progress |= game.MarkJunctionEdge(r, c, inference.Direction1, !edge2?.HasLine ?? true);
                        }
                    }
                }
            }
            return progress;
        }

        // (SH42)
        // (LH37) When there's two adjacent exits from one junciton, infer xor (then make sure infer2 cascades xors)
        // TODO:
        // - Consider xor inferences as fixed entry/exit points or lack thereof (SH43)
        private static bool CheckSingleCellParity(Game game)
        {
            bool progress = false;
            var board = game.Board;
            for (var r = 0; r < board.Rows; r++)
            {
                for (var c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];

                    // Does the cell have available edges?
                    if (cell.Undetermined == 0)
                    {
                        continue;
                    }

                    var nwJunction = board.GetJunction(r, c);
                    var neJunction = board.GetJunction(r, c + 1);
                    var seJunction = board.GetJunction(r + 1, c + 1);
                    var swJunction = board.GetJunction(r + 1, c);

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
                        progress |= InferJunctionXor(game, exitJunctionR, exitJunctionC, dir1, dir2);
                        progress |= InferJunctionXor(game, exitJunctionR, exitJunctionC, Direction.Opposite(dir1), Direction.Opposite(dir2));
                    }
                    else if (even && availableExits == 2)
                    {
                        // Can't go either way
                        // TODO: it could be an inverse corner (if none of the current lines connects to this junction?)
                        if (board.GetJunction(exitJunctionR, exitJunctionC).CountLines() == 1)
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
                        var exitJunction = board.GetJunction(exitJunctionR, exitJunctionC);
                        var edge1 = exitJunction.Edges[dir1];
                        var edge2 = exitJunction.Edges[dir2];

                        if (edge1?.HasLine.HasValue == false && edge2?.HasLine.HasValue == true)
                        {
                            progress |= game.MarkJunctionEdge(exitJunctionR, exitJunctionC, dir1, !even);
                        }
                        else if (edge1?.HasLine.HasValue == true && edge2?.HasLine.HasValue == false)
                        {
                            progress |= game.MarkJunctionEdge(exitJunctionR, exitJunctionC, dir2, !even);
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
