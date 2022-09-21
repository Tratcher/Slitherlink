using SL.Shared.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SL.Shared
{
    public static class GameLoader
    {
        public static Game Load(StreamReader streamReader)
        {
            var line = ReadLine(streamReader);

            if (string.IsNullOrEmpty(line))
            {
                throw new FormatException("Missing board size line 'RxC'");
            }

            // Rows x Columns
            var segments = line.Split('x');
            if (segments.Length != 2)
            {
                throw new FormatException($"Maformed board size line 'RxC': '{line}'");
            }

            var rows = int.Parse(segments[0], System.Globalization.NumberStyles.None);
            var cols = int.Parse(segments[1], System.Globalization.NumberStyles.None);

            var board = new Board(rows, cols);

            // Hints
            // Row,Column:Number
            line = ReadLine(streamReader);
            while (!string.IsNullOrEmpty(line))
            {
                // TODO: Parse
                var hintSegments = line.Split(':');
                if (hintSegments.Length != 2)
                {
                    throw new FormatException($"Malformed hint line 'R,C:N': '{line}'");
                }

                // Row,Column
                segments = hintSegments[0].Split(',');
                if (segments.Length != 2)
                {
                    throw new FormatException($"Malformed hint line 'R,C:N': '{line}'");
                }

                var row = int.Parse(segments[0], System.Globalization.NumberStyles.None);
                var col = int.Parse(segments[1], System.Globalization.NumberStyles.None);
                var hint = int.Parse(hintSegments[1], System.Globalization.NumberStyles.None);

                board[row, col].Hint = hint;

                line = ReadLine(streamReader);
            }

            // Actions
            // Row,Column,Direction:Before,After
            line = ReadLine(streamReader);
            while (!string.IsNullOrEmpty(line))
            {
                // TODO: Parse

                line = ReadLine(streamReader);
            }

            return new Game(board);
        }

        private static string? ReadLine(StreamReader streamReader)
        {
            var line = streamReader.ReadLine();
            // Skip comments
            while (line != null && line.StartsWith("/", StringComparison.Ordinal))
            {
                line = streamReader.ReadLine();
            }
            return line;
        }

        public static void Save(Game game, StreamWriter streamWriter)
        {

        }

        // Assumes a square board
        public static Board ReadCsv(StreamReader reader)
        {
            var line = ReadLine(reader);
            var entries = line!.Split(',');
            var board = new Board(entries.Length, entries.Length);
            var row = 0;
            do
            {
                entries = line!.Split(',');
                for (var c = 0; c < entries.Length; c++)
                {
                    if (!string.IsNullOrEmpty(entries[c]))
                    {
                        board[row, c].Hint = int.Parse(entries[c]);
                    }
                }

                line = ReadLine(reader);
                row++;
            } while (row < entries.Length);

            return board;
        }
    }
}
