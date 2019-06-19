using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGoat.Server
{
    /// <summary>
    /// A thread-safe color generator
    /// </summary>
    public class ColorGenerator
    {
        // immutable
        private readonly List<string> palette;
        
        // mutable, here the lock is needed
        private List<int> counts;

        // makes the instance thread-safe
        private object syncLock = new object();

        public ColorGenerator(IEnumerable<string> palette)
        {
            this.palette = new List<string>(palette);
            this.counts = new List<int>();
            
            for (int i = 0; i < this.palette.Count; i++)
                this.counts.Add(0);
        }

        public ColorGenerator() : this(StandatdPalette) { }

        public string NextColor()
        {
            lock (syncLock)
            {
                int min = counts.Min();
                int minIndex = counts.IndexOf(min);

                counts[minIndex]++;
                return palette[minIndex];
            }
        }

        public void ReleaseColor(string color)
        {
            int index = palette.IndexOf(color);
                
            if (index < 0)
                return;

            lock (syncLock)
                counts[index]--;
        }

        public static readonly string[] StandatdPalette = new string[] {
            "#c3c3f3", // blue
            "#c6f3c3", // green
            "#f3c7c3", // red
            "#f2f3c3", // yellow
            "#c6f3c3", // cyan
            "#f3c3de" // purple
        };
    }
}
