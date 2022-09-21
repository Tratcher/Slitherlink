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

        public int Lines => Edges.Count(e => e.HasLine == true);

        public int Undetermined => Edges.Count(e => !e.HasLine.HasValue);
    }
}
