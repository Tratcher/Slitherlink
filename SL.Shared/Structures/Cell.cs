using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SL.Shared.Structures
{
    [DebuggerDisplay("N: {Edges[Direction.North]?.HasLine}, E: {Edges[Direction.East]?.HasLine}, S: {Edges[Direction.South]?.HasLine}, W: {Edges[Direction.West]?.HasLine}, H: {Hint}")]
    public class Cell
    {
        public int? Hint { get; set; }

        public Edge[] Edges { get; } = new Edge[4];

        public int Xs => Edges.Count(e => e.HasLine == false);

        // Not Linq because of hot path
        public int Lines
        {
            get
            {
                var lineCount = 0;
                var edges = Edges;
                var length = edges.Length;
                for (int i = 0; i < length; i++)
                {
                    if (edges[i].HasLine == true) lineCount++;
                }
                return lineCount;
            }
        }

        public int Undetermined
        {
            get
            {
                var lineCount = 0;
                var edges = Edges;
                var length = edges.Length;
                for (int i = 0; i < length; i++)
                {
                    if (!edges[i].HasLine.HasValue) lineCount++;
                }
                return lineCount;
            }
        }

        public Junction GetJunction(int dir1, int dir2)
        {
            return Edges[dir1].Junctions[dir2 % 2];
        }
    }
}
