using System.Threading.Tasks;
using NUnit.Framework;
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;
using UnityEngine;
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
            Assert.AreEqual(1, card.getProp<int>(null, "attack"));
        }
        [Test]
        public void registerEffectTest()
        {
            CardEngine game = new CardEngine();
            game.triggers = new GameObject(typeof(TriggerManager).Name).AddComponent<TriggerManager>();
            Card card = new Card();
            card.addBuff(game, new TestBuff());

        }
        class TestBuff : Buff
        {
            public const int ID = 0x001;
            public override int id { get; } = ID;
            public override PropModifier[] modifiers { get; } = new PropModifier[]
            {
                new IntPropModifier("attack",1)
            };
            public override IPassiveEffect[] effects { get; }
            public override int instanceID { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            public override Buff clone()
            {
                return new TestBuff();
            }
        }
    }
}
