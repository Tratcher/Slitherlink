using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace SL.Shared.Structures
{
    [DebuggerDisplay("HasLine: {HasLine}")]
    public class Edge
    {
        public bool? HasLine;

        public bool Vertical { get; init; }

        public bool Horizontile => !Vertical;

        public Junction[] Junctions { get; } = new Junction[2];
    }
}
