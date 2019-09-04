using NUnit.Framework;
using System;
using CodeGoat.Server;
using System.Linq;

namespace ServerTests
{
    [TestFixture]
    public class DocumentHistoryTest
    {
        private Change FakeChange(string id)
        {
            return new Change(id, default(Location), default(Location), null, null);
        }

        [TestCase]
        public void ChangesCanBeAdded()
        {
            var h = new DocumentHistory();

            Assert.IsNull(h.LatestChange);
            
            h.Add(FakeChange("foo"));
            Assert.AreEqual(1, h.ToList().Count);
            Assert.AreEqual("foo", h.ToList()[0].Id);
            Assert.AreEqual("foo", h.LatestChange.Id);

            h.Add(FakeChange("bar"));
            Assert.AreEqual(2, h.ToList().Count);
            Assert.AreEqual("foo", h.ToList()[0].Id);
            Assert.AreEqual("bar", h.ToList()[1].Id);
            Assert.AreEqual("bar", h.LatestChange.Id);

            h.Add(FakeChange("baz"));
            Assert.AreEqual(3, h.ToList().Count);
            Assert.AreEqual("foo", h.ToList()[0].Id);
            Assert.AreEqual("bar", h.ToList()[1].Id);
            Assert.AreEqual("baz", h.ToList()[2].Id);
            Assert.AreEqual("baz", h.LatestChange.Id);
        }

        [TestCase]
        public void ChangesCanBeTestedForContainment()
        {
            var h = new DocumentHistory();
            h.Add(FakeChange("foo"));
            h.Add(FakeChange("bar"));
            h.Add(FakeChange("baz"));

            Assert.IsTrue(h.Contains("foo"));
            Assert.IsTrue(h.Contains("bar"));
            Assert.IsTrue(h.Contains("baz"));

            Assert.IsFalse(h.Contains("xxx"));
            Assert.IsFalse(h.Contains("yyy"));
            Assert.IsFalse(h.Contains("zzz"));
        }

        [TestCase]
        public void ChangesCanBeIteratedOverSinceAChange()
        {
            var h = new DocumentHistory();
            h.Add(FakeChange("foo"));
            h.Add(FakeChange("bar"));
            h.Add(FakeChange("baz"));

            Assert.AreEqual(
                new string[] {"bar", "baz"},
                h.IterateChangesAfter("foo").Select(c => c.Id).ToArray()
            );

            Assert.AreEqual(
                new string[] {"baz"},
                h.IterateChangesAfter("bar").Select(c => c.Id).ToArray()
            );

            Assert.AreEqual(
                new string[] {},
                h.IterateChangesAfter("baz").Select(c => c.Id).ToArray()
            );

            Assert.AreEqual(
                new string[] {},
                h.IterateChangesAfter("xxx").Select(c => c.Id).ToArray()
            );

            Assert.AreEqual(
                new string[] {},
                new DocumentHistory().IterateChangesAfter("xxx").Select(c => c.Id).ToArray()
            );
        }
    }
}
