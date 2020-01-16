using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using TouhouCardEngine;
using TouhouHeartstone;

namespace Tests
{
    public class CardPoolTests
    {
        [Test]
        public void registerTest()
        {
            CardPool pool = new CardPool();
        }
    }
    class TestCardDefine : CardDefine
    {
        public override int id { get; set; }
        public override CardDefineType type => throw new System.NotImplementedException();

        public override Effect[] effects => throw new System.NotImplementedException();

        public override string isUsable(CardEngine engine, Player player, Card card)
        {
            throw new System.NotImplementedException();
        }
    }
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
            });//首先，我们有牌库p1和牌库p2，里面分别有卡片1,2,3和4,5,6

            p1.replace(p1[0, 1], p2[0, 1]);//让p1将p1中的第0~1张卡和p2中的0~1张卡相互替换

            Assert.AreEqual(1, p2[0].id);
            Assert.AreEqual(p2, p2[0].pile);
            Assert.AreEqual(2, p2[1].id);
            Assert.AreEqual(p2, p2[1].pile);
            Assert.AreEqual(4, p1[0].id);
            Assert.AreEqual(p1, p1[0].pile);
            Assert.AreEqual(5, p1[1].id);
            Assert.AreEqual(p1, p1[1].pile);//检查上面的操作是否产生了想要的结果
            //如果不出问题，p2的第0~1张卡的pile应该都是p2，id应该分别是1和2
            //反之p1亦然
        }
    }
}
