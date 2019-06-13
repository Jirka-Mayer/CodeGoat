using System;
using LightJson;

namespace CodeGoat.Server
{
    /// <summary>
    /// Location within the document
    /// </summary>
    public struct Location
    {
        /// <summary>
        /// Line index starting from 0
        /// </summary>
        public int line;

        /// <summary>
        /// Character index, line begining is 0
        /// </summary>
        public int ch;

        public Location(int line, int ch)
        {
            this.line = line;
            this.ch = ch;
        }

        /// <summary>
        /// Creates location from given JSON object
        /// Resolves "ch: null" if document is provided and line bounds
        /// </summary>
        public static Location FromJsonObject(JsonObject obj, Document doc = null)
        {
            Location loc = new Location(
                obj["line"].AsInteger,
                obj["ch"].AsInteger
            );

            if (loc.line < 0)
                loc.line = 0;

            if (doc != null && loc.line >= doc.LineCount)
                loc.line = doc.LineCount - 1;

            if (doc != null && obj["ch"].IsNull)
                loc.ch = doc.GetLine(loc.line).Length;

            if (loc.ch < 0)
                loc.ch = 0;

            if (doc != null && loc.ch > doc.GetLine(loc.line).Length)
                loc.ch = doc.GetLine(loc.line).Length;

            return loc;
        }
    }
}
