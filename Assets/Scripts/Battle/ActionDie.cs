namespace TheLastArk.Battle
{
    public sealed class ActionDie
    {
        public int Id { get; }
        public int Value { get; }
        public bool IsSpent { get; internal set; }
        public bool IsLocked { get; internal set; }
        public bool IsAvailable => !IsSpent && !IsLocked;

        internal ActionDie(int id, int value)
        {
            Id = id;
            Value = value;
        }
    }
}
