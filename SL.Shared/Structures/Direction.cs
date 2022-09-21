using System;
using System.Collections.Generic;
using System.Text;

namespace SL.Shared.Structures
{
    public static class Direction
    {
        public const int North = 0;
        public const int South = 1;
        public const int West = 2;
        public const int East = 3;

        public static int Opposite(int direction)
        {
            return direction switch
            {
                North => South,
                South => North,
                East => West,
                West => East,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, string.Empty)
            };
        }
    }
}