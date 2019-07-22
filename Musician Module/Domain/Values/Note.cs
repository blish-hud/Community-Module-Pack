using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Musician_Module.Domain.Values
{
    public class Note
    {
        public enum Keys
        {
            None,
            C,
            D,
            E,
            F,
            G,
            A,
            B
        }
        public enum Octaves
        {
            None,
            Lowest,
            Low,
            Middle,
            High,
            Highest
        }
        public static Dictionary<string, Color> OctaveColors = new Dictionary<string, Color>{
            { "None", Color.Black },
            { "Lowest", Color.Purple },
            { "Low", Color.LightBlue },
            { "Middle", Color.LightGreen },
            { "High", Color.Gold },
            { "Highest", Color.OrangeRed }
        };
        public Note(Keys key, Octaves octave)
        {
            Key = key;
            Octave = octave;
        }

        public Keys Key { get; set; }
        public Octaves Octave { get; set; }

        public override string ToString()
        {
            return $"{(Octave >= Octaves.High ? "▲" : Octave <= Octaves.Low ? "▼" : string.Empty)}{Key}";
        }

        public override bool Equals(object obj)
        {
            return Equals((Note) obj);
        }

        protected bool Equals(Note other)
        {
            return Key == other.Key && Octave == other.Octave;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Key*397) ^ (int) Octave;
            }
        }
    }
}