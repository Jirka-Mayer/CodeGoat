using NUnit.Framework;
using System;
using CodeGoat.Server;

namespace ServerTests
{
    [TestFixture]
    public class ColorGeneratorTest
    {
        private string[] testPalette = new string[] {
            "red", "green", "blue"
        };

        private ColorGenerator generator;

        [SetUp]
        public void SetUp()
        {
            generator = new ColorGenerator(testPalette);
        }

        [Test]
        public void ItGeneratesUniqueColorsInOrderUpToPaletteSizeAndThenRepeats()
        {
            Assert.AreEqual("red", generator.NextColor());
            Assert.AreEqual("green", generator.NextColor());
            Assert.AreEqual("blue", generator.NextColor());

            Assert.AreEqual("red", generator.NextColor());
            Assert.AreEqual("green", generator.NextColor());
            Assert.AreEqual("blue", generator.NextColor());

            Assert.AreEqual("red", generator.NextColor());
            Assert.AreEqual("green", generator.NextColor());
            Assert.AreEqual("blue", generator.NextColor());
        }

        [Test]
        public void ItGeneratesLeastUsedColorIfItsReleased()
        {
            Assert.AreEqual("red", generator.NextColor());
            Assert.AreEqual("green", generator.NextColor());
            Assert.AreEqual("blue", generator.NextColor());

            generator.ReleaseColor("green");

            Assert.AreEqual("green", generator.NextColor());

            Assert.AreEqual("red", generator.NextColor());
            Assert.AreEqual("green", generator.NextColor());
            Assert.AreEqual("blue", generator.NextColor());
        }
    }
}
