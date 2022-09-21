namespace SL.Shared.Structures
{
    public record Move
    {
        public int Row { get; init; }
        public int Column { get; init; }
        public int Direction { get; init; }
        public bool? OldValue { get; init; }
        public bool? NewValue { get; init; }
    }
}
