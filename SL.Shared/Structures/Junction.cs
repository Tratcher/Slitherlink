using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SL.Shared.Structures
{
    public class Junction
    {
        public Edge[] Edges { get; } = new Edge[4];

        public List<InferenceXor> Inferences { get; } = new List<InferenceXor>();

        public int CountLines()
        {
            var lineCount = 0;
            foreach (var edge in Edges)
            {
                if (edge?.HasLine == true)
                {
                    lineCount++;
                }
            }
            return lineCount;
        }

        public int EdgeCount => Edges.Count(e => e != null);

        public int LineCount => Edges.Count(e => e?.HasLine == true);

        public int NotLineCount => Edges.Count(e => e?.HasLine == false);

        public int UnknownCount => Edges.Count(e => e?.HasLine.HasValue == false);
    }
}
