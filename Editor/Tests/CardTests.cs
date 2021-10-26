using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class CardTests
    {
        [Test]
        public void cardIdBitOpTest()
        {
            uint uCardId = 0x80020003;
            int cardId = (int)uCardId;
            int cardPoolId = cardId << 1 >> 17;
            Assert.AreEqual(cardPoolId, 0x0002);
            cardId = cardId << 16 >> 16;
            Assert.AreEqual(cardId, 0x0003);
            cardId = 1 << 31 | 0x0002 << 16 | 0x0003;
            Assert.AreEqual(cardId, (int)uCardId);
        }
    }
}