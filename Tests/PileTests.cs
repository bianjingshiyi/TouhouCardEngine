using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using TouhouHeartstone;

namespace Tests
{
    public class PileTests
    {
        [Test]
        public void replaceTest()
        {
            Pile p1 = new Pile(cards: new Card[]
            {
                new Card(1),
                new Card(2),
                new Card(3)
            });
            Pile p2 = new Pile(cards: new Card[]
            {
                new Card(4),
                new Card(5),
                new Card(6)
            });
            p1.replace(p1[0, 1], p2[0, 1]);
            Assert.AreEqual(1, p2[0].id);
            Assert.AreEqual(p2, p2[0].pile);
            Assert.AreEqual(2, p2[1].id);
            Assert.AreEqual(p2, p2[1].pile);
            Assert.AreEqual(4, p1[0].id);
            Assert.AreEqual(p1, p1[0].pile);
            Assert.AreEqual(5, p1[1].id);
            Assert.AreEqual(p1, p1[1].pile);
        }
    }
}
