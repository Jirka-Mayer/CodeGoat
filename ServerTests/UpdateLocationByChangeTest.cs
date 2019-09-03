using NUnit.Framework;
using System;
using CodeGoat.Server;
using LightJson;
using LightJson.Serialization;

namespace ServerTests
{
    [TestFixture]
    public class UpdateLocationByChangeTest
    {
        [Test]
        public void InlineInsertAfter()
        {
            Location loc = new Location(0, 0, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 10},
                ""to"": {""line"": 0, ""ch"": 10},
                ""text"": [""asd""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(0, loc.ch);
        }

        [Test]
        public void InlineInsertAfterSticky()
        {
            Location loc = new Location(0, 10, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 10},
                ""to"": {""line"": 0, ""ch"": 10},
                ""text"": [""asd""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(10, loc.ch);
        }

        [Test]
        public void InlineInsertBefore()
        {
            Location loc = new Location(0, 10, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 0},
                ""to"": {""line"": 0, ""ch"": 0},
                ""text"": [""asd""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(13, loc.ch);
        }

        [Test]
        public void InlineInsertBeforeSticky()
        {
            Location loc = new Location(0, 10, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 10},
                ""to"": {""line"": 0, ""ch"": 10},
                ""text"": [""asd""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(13, loc.ch);
        }

        [Test]
        public void MultilineInsertBefore()
        {
            // asd^xxx|f

            // asd---
            // -xxx|f

            Location loc = new Location(0, 6, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 3},
                ""text"": [""---"", ""-""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(1, loc.line);
            Assert.AreEqual(4, loc.ch);
        }

        [Test]
        public void MultilineInsertBeforeStickyAfter()
        {
            // asd^|xxxf

            // asd---
            // -|xxxf

            Location loc = new Location(0, 3, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 3},
                ""text"": [""---"", ""-""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(1, loc.line);
            Assert.AreEqual(1, loc.ch);
        }

        [Test]
        public void MultilineInsertBeforeStickyBefore()
        {
            // asd|^xxxf

            // asd|---
            // -xxxf

            Location loc = new Location(0, 3, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 3},
                ""text"": [""---"", ""-""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void MultilineInsertBeforeManyLines()
        {
            // asd^f
            // fooxxxy|f

            // asd---
            // -
            // -f
            // fooxxxy|f

            Location loc = new Location(1, 7, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 3},
                ""text"": [""---"", ""-"", ""-""],
                ""removed"": [""""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(3, loc.line);
            Assert.AreEqual(7, loc.ch);
        }

        [Test]
        public void InlineDeleteAfter()
        {
            // a|sd___foo

            Location loc = new Location(0, 1, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(1, loc.ch);
        }

        [Test]
        public void InlineDeleteAfterStickyBefore()
        {
            // asd|___foo

            Location loc = new Location(0, 3, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void InlineDeleteAfterStickyAfter()
        {
            // asd|___foo

            Location loc = new Location(0, 3, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void InlineDeleteBefore()
        {
            // asd___f|oo

            Location loc = new Location(0, 7, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(4, loc.ch);
        }

        [Test]
        public void InlineDeleteBeforeStickyAfter()
        {
            // asd___|foo

            Location loc = new Location(0, 6, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void InlineDeleteBeforeStickyBefore()
        {
            // asd___|foo

            Location loc = new Location(0, 6, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 0, ""ch"": 6},
                ""text"": [""""],
                ""removed"": [""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void MultilineDeleteBefore()
        {
            // asd___
            // ______
            // ____f|oo

            Location loc = new Location(2, 5, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 2, ""ch"": 4},
                ""text"": [""""],
                ""removed"": [""___"", ""______"", ""____""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(4, loc.ch);
        }

        [Test]
        public void MultilineDeleteInside()
        {
            // asd___
            // ___|___
            // ___foo

            Location loc = new Location(1, 3, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 2, ""ch"": 3},
                ""text"": [""""],
                ""removed"": [""___"", ""______"", ""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void MultilineDeleteBeforeStickyAfter()
        {
            // asd___
            // ______
            // ___|foo

            Location loc = new Location(2, 3, Location.Stickiness.After);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 2, ""ch"": 3},
                ""text"": [""""],
                ""removed"": [""___"", ""______"", ""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }

        [Test]
        public void MultilineDeleteBeforeStickyBefore()
        {
            // asd___
            // ______
            // ___|foo

            Location loc = new Location(2, 3, Location.Stickiness.Before);

            Change change = Change.FromCodemirrorJson(JsonReader.Parse(@"{
                ""from"": {""line"": 0, ""ch"": 3},
                ""to"": {""line"": 2, ""ch"": 3},
                ""text"": [""""],
                ""removed"": [""___"", ""______"", ""___""]
            }"));

            loc = loc.UpdateByChange(change);

            Assert.AreEqual(0, loc.line);
            Assert.AreEqual(3, loc.ch);
        }
    }
}
