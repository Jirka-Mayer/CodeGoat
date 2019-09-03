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

        /// <summary>
        /// Tracks where a location in the document moves when a change is applied
        /// </summary>
        public Location UpdateByChange(Change change)
        {
            var ret = new Location(line, ch, sticky);

            // location is before everything so it doesn't move
            if (line < change.From.line)
                return ret;
            
            if (line == change.From.line)
            {
                if (ch < change.From.ch)
                    return ret;
                
                if (ch == change.From.ch && sticky == Stickiness.Before)
                    return ret;
            }

            /////////////////
            // Delete text //
            /////////////////
            
            // location is inside the deleted region so move it to the "from" (keep stickiness)
            if (
                line < change.To.line || // inside by line
                (line == change.To.line && (ch < change.To.ch || // or the same line but inside by char
                    (ch == change.To.ch && sticky == Stickiness.Before) // or same char but inside by stickiness
                ))
            )
            {
                ret.line = change.From.line;
                ret.ch = change.From.ch;
            }

            // location is after the deleted region
            else
            {
                // if the location is on the last line of deletion, perform leftwise movement
                if (ret.line == change.To.line)
                {
                    ret.ch -= change.To.ch - change.From.ch;
                }

                // move line up by the number of deleted lines
                ret.line -= change.To.line - change.From.line;
            }
            
            /////////////////
            // Insert text //
            /////////////////

            // the case when location is before the edited region is already handled here
            // so the location has to be after the region

            // unless it was inside and is now sticking before
            if (ret.line == change.From.line && ret.ch == change.From.ch && sticky == Stickiness.Before)
                return ret;

            // now it's after the inserted region so just perform the movement

            // if the location is on the line of insertion, perform rightwise movement
            if (ret.line == change.From.line)
            {
                // multiline
                if (change.Text.Count > 1)
                {
                    ret.ch += change.Text[change.Text.Count - 1].Length - change.From.ch;
                }
                else // inline
                {
                    ret.ch += change.Text[change.Text.Count - 1].Length;
                }
            }

            // move line down by the number of inserted lines
            ret.line += change.Text.Count - 1;

            return ret;
        }
    }
}
