using NUnit.Framework;
using System;
using CodeGoat.Server;
using LightJson;
using LightJson.Serialization;

namespace ServerTests
{
    [TestFixture]
    public class DocumentChangeApplicationTest
    {
        [Test]
        public void InsertIntoEmptyDocumentOneLine()
        {
            Document doc = new Document("");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 0},
                ""to"": {""line"": 0, ""ch"": 0},
                ""text"": [""Lorem ipsum.""],
                ""removed"": [""""]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("Lorem ipsum.", doc.GetText());
        }

        [Test]
        public void InsertIntoEmptyDocumentManyLines()
        {
            Document doc = new Document("");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 0},
                ""to"": {""line"": 0, ""ch"": 0},
                ""text"": [""Lorem"", ""ipsum""],
                ""removed"": [""""]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("Lorem\nipsum", doc.GetText());
        }

        [Test]
        public void InsertIntoLineCharacters()
        {
            Document doc = new Document("aaabbb");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 3},
                ""text"": [""ccc""],
                ""removed"": [""""]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("aaacccbbb", doc.GetText());
        }

        [Test]
        public void InsertIntoLineLines()
        {
            Document doc = new Document("\naaabbb\n");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 1, ""ch"": 3},
                ""to"": {""line"": 1, ""ch"": 3},
                ""text"": ["""", ""ccc"", """"],
                ""removed"": [""""]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("\naaa\nccc\nbbb\n", doc.GetText());
        }

        [Test]
        public void RemoveLineBreak()
        {
            Document doc = new Document("Lorem\nipsum");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 5},
                ""to"": {""line"": 1, ""ch"": 0},
                ""text"": [""""],
                ""removed"": ["""", """"]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("Loremipsum", doc.GetText());
        }

        [Test]
        public void RemoveLineCenter()
        {
            Document doc = new Document("aaacccbbb");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""ccc""]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("aaabbb", doc.GetText());
        }

        [Test]
        public void RemoveMultipleLines()
        {
            Document doc = new Document("aaaccc\ncccbbb");

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 1, ""ch"": 3},
                ""text"": [""""],
                ""removed"": [""ccc"", ""ccc""]
            }"));

            doc.ApplyChange(change);

            Assert.AreEqual("aaabbb", doc.GetText());
        }
    }
}
