
using System.Diagnostics;

namespace SL.Shared.Structures
{
    public class InferenceXor
    {
        public InferenceXor(int direction1, int direction2)
        {
            Debug.Assert(direction1 != direction2);
            Direction1 = direction1;
            Direction2 = direction2;
        }

        public int Direction1 { get; }
        public int Direction2 { get; }

        // Not order sensitive
        public bool Equals(int d1, int d2)
        {
            return Direction1 == d1 && Direction2 == d2
                || Direction1 == d2 && Direction2 == d1;
        }
    }
}
