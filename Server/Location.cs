using System;
using LightJson;

namespace CodeGoat.Server
{
    /// <summary>
    /// Location within the document, represents a place between two characters
    /// Character index 0 is before the first character
    /// </summary>
    public struct Location
    {
        public enum Stickiness
        {
            After = 0, // default value
            Before = 1
        }

        /// <summary>
        /// Line index starting from 0
        /// </summary>
        public int line;

        /// <summary>
        /// Character index, line begining is 0
        /// </summary>
        public int ch;

        /// <summary>
        /// What character does this location stick to
        /// </summary>
        public Stickiness sticky;

        public Location(int line, int ch, Stickiness sticky = Stickiness.After)
        {
            this.line = line;
            this.ch = ch;
            this.sticky = sticky;
        }

        /// <summary>
        /// Creates location from given JSON object
        /// Resolves "ch: null" and line bounds when a document is provided
        /// </summary>
        public static Location FromCodemirrorJson(JsonObject obj, Document doc = null)
        {
            Location loc = new Location(
                obj["line"].AsInteger,
                obj["ch"].AsInteger,
                obj["sticky"].AsString == "before" ? Stickiness.Before : Stickiness.After
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

        /// <summary>
        /// Converts this change into the codemirror representation of a location as a json object
        /// </summary>
        public JsonObject ToCodemirrorJson()
        {
            return new JsonObject()
                .Add("line", line)
                .Add("ch", ch)
                .Add("sticky", sticky == Stickiness.Before ? "before" : "after");
        }
    }
}
