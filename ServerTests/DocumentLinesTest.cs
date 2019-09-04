using NUnit.Framework;
using System;
using CodeGoat.Server;

namespace ServerTests
{
    [TestFixture]
    public class DocumentLinesTest
    {
        [Test]
        public void ItCanBeCreated()
        {
            var d = new DocumentLines();
            Assert.AreEqual(1, d.LineCount);
            Assert.AreEqual(0, d[0].Length);

            d = new DocumentLines((string)null);
            Assert.AreEqual(1, d.LineCount);
            Assert.AreEqual(0, d[0].Length);

            d = new DocumentLines("");
            Assert.AreEqual(1, d.LineCount);
            Assert.AreEqual(0, d[0].Length);

            d = new DocumentLines(new string[] {});
            Assert.AreEqual(1, d.LineCount);
            Assert.AreEqual(0, d[0].Length);

            d = new DocumentLines(new string[] {""});
            Assert.AreEqual(1, d.LineCount);
            Assert.AreEqual(0, d[0].Length);

            d = new DocumentLines("\n");
            Assert.AreEqual(2, d.LineCount);
            Assert.AreEqual(0, d[0].Length);
            Assert.AreEqual(0, d[1].Length);

            d = new DocumentLines("lorem\nipsum");
            Assert.AreEqual(2, d.LineCount);
            Assert.AreEqual("lorem", d[0]);
            Assert.AreEqual("ipsum", d[1]);

            d = new DocumentLines(new string[] {"lorem", "ipsum"});
            Assert.AreEqual(2, d.LineCount);
            Assert.AreEqual("lorem", d[0]);
            Assert.AreEqual("ipsum", d[1]);

            d = new DocumentLines(new string[] {"lorem", null});
            Assert.AreEqual(2, d.LineCount);
            Assert.AreEqual("lorem", d[0]);
            Assert.AreEqual("", d[1]);

            d = new DocumentLines(new string[] {null});
            Assert.AreEqual(1, d.LineCount);
            Assert.AreEqual("", d[0]);
        }

        [Test]
        public void ItCanBePutToString()
        {
            Assert.AreEqual("", new DocumentLines().ToString());
            Assert.AreEqual("", new DocumentLines("").ToString());
            Assert.AreEqual("\n", new DocumentLines("\n").ToString());
            Assert.AreEqual("lorem", new DocumentLines("lorem").ToString());
            Assert.AreEqual("lorem\nipsum", new DocumentLines("lorem\nipsum").ToString());

            Assert.AreEqual("hello\n\nworld", new DocumentLines(new string[] {"hello", null, "world"}).ToString());
        }

        [Test]
        public void ItAccessesLastLine()
        {
            Assert.AreEqual("", new DocumentLines("").LastLine);
            Assert.AreEqual("lorem", new DocumentLines("lorem").LastLine);
            Assert.AreEqual("ipsum", new DocumentLines("lorem\nipsum").LastLine);
        }
    }
}
