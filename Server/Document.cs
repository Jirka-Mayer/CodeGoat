using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGoat.Server
{
    /// <summary>
    /// The text document that's being edited
    /// It includes the history of changes so it can merge old changes properly
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Lines of the document (current state)
        /// </summary>
        public DocumentLines Lines { get; private set; } = new DocumentLines();

        /// <summary>
        /// History of comitted changes
        /// </summary>
        private DocumentHistory history = new DocumentHistory();

        /// <summary>
        /// Current state of the document
        /// This is either initial, or the id of last committed change
        /// </summary>
        public string State => history.LatestChange?.Id ?? InitialState;

        /// <summary>
        /// State identifier of the document when no changes have been applied
        /// </summary>
        public const String InitialState = "initial";

        /// <summary>
        /// Creates new document with the given content
        /// </summary>
        public Document(string content = null)
        {
            Lines = new DocumentLines(content);
        }

        /// <summary>
        /// Full text content of the document
        /// </summary>
        public string GetText() => Lines.ToString();

        /// <summary>
        /// Sets the document content as a new change to the document
        /// </summary>
        public void SetText(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            ApplyChange(
                new Change(
                    Str.Random(16),
                    Lines.StartLocation,
                    Lines.EndLocation,
                    text,
                    GetText()
                )
            );
        }

        /// <summary>
        /// Returns a line of the document via a zero-based index
        /// </summary>
        public string GetLine(int index) => Lines[index];

        /// <summary>
        /// Apply a change to the document
        /// </summary>
        public void ApplyChange(Change change)
        {
            var from = Lines.ClampLocation(change.From);
            var to = Lines.ClampLocation(change.To);

            Lines = Lines.RemoveText(from, to);
            Lines = Lines.InsertText(from, new DocumentLines(change.Text));

            history.Add(change);
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
            // no point in history found, this is weird
            if (!history.Contains(documentState) && documentState != Document.InitialState)
                return null;

            // go through new changes one by one and update position of our given change accordingly
            // (start at the base point in history)
            foreach (Change c in history.IterateChangesAfter(documentState))
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
