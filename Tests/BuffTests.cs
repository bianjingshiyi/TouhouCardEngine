using NUnit.Framework;
using TouhouCardEngine;

namespace Tests
{
    public class BuffTests
    {
        [Test]
        public void addAndGetBuffTest()
        {
            Card card = new Card(0);
            card.addBuff(null, new TestBuff());
            Buff[] buffs = card.getBuffs();
            Assert.AreEqual(1, buffs.Length);
            Assert.IsInstanceOf<TestBuff>(buffs[0]);
        }
        [Test]
        public void modifyIntPropTest()
        {
            Card card = new Card(0);
            card.addBuff(null, new TestBuff());
            Assert.AreEqual(1, card.getProp<int>("attack"));
        }
        class TestBuff : Buff
        {
            public const int ID = 0x001;
            public override int id { get; } = ID;
            public override PropModifier[] modifiers { get; } = new PropModifier[]
            {
                new IntPropModifier("attack",1)
            };
            public override Buff clone()
            {
                return new TestBuff();
            }
        }
    }
}
