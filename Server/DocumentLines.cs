using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace CodeGoat.Server
{
    /// <summary>
    /// Immutable class describing textual state of the document
    /// (basicaly it's a fancy multiline string)
    /// </summary>
    public class DocumentLines : IEnumerable<string>
    {
        /// <summary>
        /// List of lines, properly set up
        /// It's never an empty list. Empty document is one empty line.
        /// There are no null lines.
        /// </summary>
        private List<string> lines;

        /// <summary>
        /// The number of lines
        /// </summary>
        public int LineCount => lines.Count;

        /// <summary>
        /// Returns given line of text or null
        /// </summary>
        public string this[int lineIndex]
        {
            get
            {
                if (lineIndex < 0)
                    return null;

                if (lineIndex >= LineCount)
                    return null;

                return lines[lineIndex];
            }
        }

        /// <summary>
        /// Last line of the document
        /// </summary>
        public string LastLine => lines.Last();

        /// <summary>
        /// Location before the first character in the document
        /// </summary>
        public Location StartLocation => new Location(0, 0, Location.Stickiness.Before);

        /// <summary>
        /// Location after the last character in the document
        /// </summary>
        public Location EndLocation => new Location(LineCount - 1, LastLine.Length, Location.Stickiness.After);

        /// <summary>
        /// Creates single empty line of text
        /// </summary>
        public DocumentLines() : this((string)null) { }

        /// <summary>
        /// Creates instance from a (possibly multiline) string
        /// </summary>
        public DocumentLines(string text) : this(text == null ? new string[] {} : text.Split('\n')) { }

        /// <summary>
        /// Creates instance from a sequence of lines
        /// </summary>
        public DocumentLines(IEnumerable<string> lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            this.lines = lines
                .Select(l => l == null ? "" : l) // remove null lines
                .ToList();

            // list cannot be empty
            if (this.lines.Count == 0)
                this.lines.Add("");
        }

        /// <summary>
        /// Create instance from a json array of strings
        /// </summary>
        public DocumentLines(JsonArray json) : this(json.Select(s => s.AsString)) { }

        /// <summary>
        /// Returns all the lines joined together by the \n character
        /// </summary>
        public override string ToString()
        {
            return String.Join("\n", lines);
        }

        /// <summary>
        /// Converts the instance into a list of lines
        /// 
        /// Empty document is an empty line, there cannot be no lines
        /// </summary>
        public List<string> ToList()
        {
            return new List<string>(lines);
        }

        /// <summary>
        /// Converts the instance into a json array of strings (lines)
        /// </summary>
        public JsonArray ToJsonArray()
        {
            return new JsonArray(this.Select(l => (JsonValue)l).ToArray());
        }

        public IEnumerator<string> GetEnumerator()
        {
            return lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Clamp a location to make sure it's inside the document
        /// </summary>
        public Location ClampLocation(Location location)
        {
            // NOTE: Whenever there's an edit outside the bounds of the document,
            // something went not quite right. That's why this method logs to the console.

            Location ret = location;

            // line
            if (ret.line < 0)
            {
                Console.WriteLine("Clamping below on lines to 0");
                ret.line = 0;
            }

            if (ret.line >= LineCount)
            {
                ret.line = LineCount - 1;
                Console.WriteLine("Clamping above on lines to " + ret.line);
            }

            // char
            if (ret.ch < 0)
            {
                Console.WriteLine("Clamping below on chars to 0");
                ret.ch = 0;
            }

            if (ret.ch > lines[ret.line].Length) // may be equal
            {
                ret.ch = lines[ret.line].Length;
                Console.WriteLine("Clamping above on chars to " + ret.ch);
            }

            return ret;
        }

        /// <summary>
        /// Remove text between two locations and return the modified document
        /// </summary>
        /// <param name="from">Start location, has to be before 'to'</param>
        /// <param name="to">End location, has to be after 'from'</param>
        public DocumentLines RemoveText(Location from, Location to)
        {
            List<string> lines = ToList();

            string lineStart = lines[from.line].Substring(0, from.ch);
            string lineEnd = lines[to.line].Substring(to.ch);

            int removeCount = to.line - from.line;
            if (removeCount > 0)
                lines.RemoveRange(from.line + 1, removeCount);

            lines[from.line] = lineStart + lineEnd;

            return new DocumentLines(lines);
        }

        /// <summary>
        /// Insert text into a location and return the modified document
        /// </summary>
        /// <param name="at">Location of insertion</param>
        /// <param name="text">Text to be inserted</param>
        public DocumentLines InsertText(Location at, DocumentLines text)
        {
            List<string> lines = ToList();

            string lineStart = lines[at.line].Substring(0, at.ch);
            string lineEnd = lines[at.line].Substring(at.ch);

            lines[at.line] = lineStart + text[0];

            lines.InsertRange(at.line + 1, text.Skip(1));
            lines[at.line + text.LineCount - 1] += lineEnd;

            return new DocumentLines(lines);
        }
    }
}
