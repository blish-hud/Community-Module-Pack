namespace Musician_Module.Domain.Values
{
    public class Beat
    {
        public Beat(decimal value)
        {
            Value = value;
        }

        public decimal Value { get; }

        public static implicit operator decimal(Beat beat) => beat.Value;
    }
}