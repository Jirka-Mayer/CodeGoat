using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGoat.Server
{
    /// <summary>
    /// The text document that's being edited
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Lines of the document (current state)
        /// (without the line break)
        /// </summary>
        private List<string> lines = new List<string>();

        public int LineCount => lines.Count;

        // HACK
        public List<Change> changes = new List<Change>();

        /// <summary>
        /// Current state of the document
        /// This is either initial, or the id of last committed change
        /// </summary>
        public string State => changes.Count == 0 ? "initial" : changes[changes.Count - 1].Id;

        public Document(string content = null)
        {
            lines.Add("");

            if (content != null)
                this.SetText(content);
        }

        /// <summary>
        /// Full text content of the document
        /// </summary>
        public string GetText() => String.Join("\n", lines);

        /// <summary>
        /// Sets the document content
        /// </summary>
        public void SetText(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            lines.Clear();
            lines.AddRange(text.Split('\n'));

            if (lines.Count == 0)
                lines.Add("");
        }

        /// <summary>
        /// Returns a line of the document via a zero-based index
        /// </summary>
        public string GetLine(int index) => lines[index];

        /// <summary>
        /// Apply a change to the document
        /// </summary>
        public void ApplyChange(Change change)
        {
            var from = ClampLocation(change.From);
            var to = ClampLocation(change.To);

            RemoveText(from, to);
            InsertText(from, change.Text.ToList());

            // HACK
            changes.Add(change);
        }

        private void RemoveText(Location from, Location to)
        {
            string lineStart = lines[from.line].Substring(0, from.ch);
            string lineEnd = lines[to.line].Substring(to.ch);

            int removeCount = to.line - from.line;
            if (removeCount > 0)
                lines.RemoveRange(from.line + 1, removeCount);

            lines[from.line] = lineStart + lineEnd;
        }

        private void InsertText(Location from, List<string> text)
        {
            string lineStart = lines[from.line].Substring(0, from.ch);
            string lineEnd = lines[from.line].Substring(from.ch);

            lines[from.line] = lineStart + text[0];

            lines.InsertRange(from.line + 1, text.Skip(1));
            lines[from.line + text.Count - 1] += lineEnd;
        }

        private Location ClampLocation(Location location)
        {
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
    }
}
