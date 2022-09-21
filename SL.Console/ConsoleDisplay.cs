using SL.Shared.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SL.Console
{
    public static class ConsoleDisplay
    {
        public static void ShowBoard(Board board)
        {
            for (int r = 0; r < board.Rows; r++)
            {
                // Line above
                for (int c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];
                    var edge = cell.Edges[Direction.North];

                    System.Console.Write(edge.HasLine switch
                    {
                        null => "+···",
                        true => "+---",
                        false => "+ x ",
                    });
                }
                System.Console.WriteLine("+");

                // West line, hint, East line
                for (int c = 0; c < board.Columns; c++)
                {
                    var cell = board[r, c];

                    var edge = cell.Edges[Direction.West];
                    System.Console.Write(edge.HasLine switch
                    {
                        null => ":",
                        true => "|",
                        false => "x",
                    });

                    System.Console.Write($" {cell.Hint?.ToString() ?? " "} ");


                    // if it's the last column, east line
                    if (c == board.Columns - 1)
                    {
                        edge = cell.Edges[Direction.East];
                        System.Console.Write(edge.HasLine switch
                        {
                            null => ":",
                            true => "|",
                            false => "x",
                        });
                    }
                }

                System.Console.WriteLine();

                // if it's the last row, line below
                if (r == board.Rows - 1)
                {
                    for (int c = 0; c < board.Columns; c++)
                    {
                        var cell = board[r, c];
                        var edge = cell.Edges[Direction.South];
                        System.Console.Write(edge.HasLine switch
                        {
                            null => "+···",
                            true => "+---",
                            false => "+ x ",
                        });
                    }
                }
            }

            System.Console.WriteLine("+");
        }

        public static void ShowEdges(Board board)
        {
            for (int r = 0; r <= board.Rows; r++)
            {
                // Line above
                for (int c = 0; c < board.Columns; c++)
                {
                    var junction = board.GetJunction(r, c);
                    var edge = junction.Edges[Direction.East];

                    System.Console.Write(edge.HasLine switch
                    {
                        null => " ·",
                        true => " -",
                        false => " x",
                    });
                }

                System.Console.WriteLine();

                if (r < board.Rows)
                {
                    // West line, space, East line
                    for (int c = 0; c <= board.Columns; c++)
                    {
                        var junction = board.GetJunction(r, c);

                        var edge = junction.Edges[Direction.South];
                        System.Console.Write(edge.HasLine switch
                        {
                            null => "· ",
                            true => "| ",
                            false => "x ",
                        });
                    }
                }

                System.Console.WriteLine();
            }

            System.Console.WriteLine();
        }
    }
}
