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

        public Document()
        {
            lines.Add("This is the line in the document!");
        }

        /// <summary>
        /// Full text content of the document
        /// </summary>
        public string GetText() => String.Join("\n", lines);
    }
}
