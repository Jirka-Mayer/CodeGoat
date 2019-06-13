using System;
using LightJson;
using System.Collections.Generic;
using System.Linq;

namespace CodeGoat.Server
{
    /// <summary>
    /// Represents a document change
    /// </summary>
    public class Change
    {
        public Location from;
        public Location to;
        public List<string> text = new List<string>();
        public List<string> removed = new List<string>();

        /// <summary>
        /// Creates a change instance from a json object in a given document context
        /// </summary>
        public static Change FromJsonObject(JsonObject obj, Document doc = null)
        {
            return new Change {
                from = Location.FromJsonObject(obj["from"].AsJsonObject, doc),
                to = Location.FromJsonObject(obj["to"].AsJsonObject, doc),
                text = new List<string>(obj["text"].AsJsonArray.Select(x => x.AsString)),
                removed = new List<string>(obj["removed"].AsJsonArray.Select(x => x.AsString))
            };
        }
    }
}
