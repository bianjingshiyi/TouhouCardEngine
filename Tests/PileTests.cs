using NUnit.Framework;
using TouhouCardEngine;

namespace Tests
{
    public class PileTests
    {
        [Test]
        public void createPileTest()
        {
            Pile p = new Pile(null);
            p.add(null, new Card());
            Assert.AreEqual(1, p.count);
            Assert.NotNull(p[0]);
        }
        [Test]
        public void moveToTest()
        {
            Pile p1 = new Pile(null);
            Pile p2 = new Pile(null);
            Card c = new Card();
            p1.add(null, c);
            Assert.AreEqual(c, p1[0]);
            p1.moveTo(null, c, p2);
            Assert.AreEqual(0, p1.count);
            Assert.AreEqual(c, p2[0]);
        }
        [Test]
        public void moveToTest_Cards()
        {
            Pile p1 = new Pile(null);
            Pile p2 = new Pile(null);
        }
    }
}
