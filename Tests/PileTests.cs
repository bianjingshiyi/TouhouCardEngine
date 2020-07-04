using NUnit.Framework;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
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
            for (int i = 0; i < 10; i++)
            {
                p1.add(null, new Card());
            }
            for (int i = 0; i < 10; i++)
            {
                p2.add(null, new Card());
            }
            Card[] cards = p1[1, 5];
            p1.moveTo(null, cards, p2, 1);
            Assert.AreEqual(5, p1.count);
            Assert.AreEqual(15, p2.count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(cards[i], p2[1 + i]);
            }
        }
        [UnityTest]
        public IEnumerator replaceTest()
        {
            Pile p1 = new Pile(null);
            Pile p2 = new Pile(null);
            for (int i = 0; i < 4; i++)
            {
                p1.add(null, new Card(i));
                p2.add(null, new Card(i));
            }
            Card c1 = p1[2];
            Card c2 = p2[3];
            var task = p1.replace(null, c1, c2);
            yield return new WaitUntil(() => task.IsCompleted);
            Assert.AreEqual(c2, p1[2]);
            Assert.AreEqual(c1, p2[3]);
        }
    }
}
