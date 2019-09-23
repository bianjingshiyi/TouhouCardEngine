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
                new Card(CardDefine.empty.setID(1)),
                new Card(CardDefine.empty.setID(2)),
                new Card(CardDefine.empty.setID(3))
            });
            Pile p2 = new Pile(cards: new Card[]
            {
                new Card(CardDefine.empty.setID(4)),
                new Card(CardDefine.empty.setID(5)),
                new Card(CardDefine.empty.setID(6))
            });
            
        }
    }
}
