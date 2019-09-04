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
        public string State => changes.Count == 0 ? InitialState : changes[changes.Count - 1].Id;

        /// <summary>
        /// State identifier of the document when no changes have been applied
        /// </summary>
        public const String InitialState = "initial";

        /// <summary>
        /// Location before the first character in the document
        /// </summary>
        public Location StartLocation => new Location(0, 0, Location.Stickiness.Before);

        /// <summary>
        /// Location after the last character in the document
        /// </summary>
        public Location EndLocation => new Location(LineCount - 1, lines.Last().Length, Location.Stickiness.After);

        /// <summary>
        /// Creates new document with the given content
        /// </summary>
        public Document(string content = null)
        {
            lines.Add("");

            if (!String.IsNullOrEmpty(content))
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

            ApplyChange(
                new Change(
                    Str.Random(16),
                    StartLocation,
                    EndLocation,
                    text,
                    GetText()
                )
            );
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

        /// <summary>
        /// Clamp a location to make sure it's inside the document
        /// </summary>
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

        /// <summary>
        /// Updates location of a given change to be positioned properly relative to newer changes
        /// that were committed when this change travelled to the server.
        /// </summary>
        /// <param name="change">Given chage</param>
        /// <param name="documentState">Committed state of the document the change is based on</param>
        /// <param name="dependencies">
        /// Other changes that the given change was speculatively based on,
        /// meaning that it already counts with those and we can skip them.
        /// (they will have arrived to the server by now, order of changes cannot be swapped when trasmitted)
        /// </param>
        /// <returns>The newly positioned change, or null if the documentState is not found</returns>
        public Change UpdateChangeLocationByNewerChanges(
            Change change, string documentState, IEnumerable<string> dependencies
        )
        {
            // find the point in history this given change is based on
            int i = changes.FindIndex(c => c.Id == documentState);

            // no point in history found, this is weird
            if (i == -1 && documentState != Document.InitialState)
                return null;

            // go through new changes one by one and update position of our given chagne accordingly
            // (start at the base point in history)
            foreach (Change c in changes.Skip(i + 1))
            {
                // skip dependencies, since the given change already counts with them
                if (dependencies.Contains(c.Id))
                    continue;

                change = change.UpdateLocationByChange(c);
            }

            return change;
        }
    }
}
