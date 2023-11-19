using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class CardSnapshot : ICardData, IChangeableCard
    {
        #region 公有方法

        #region 构造器
        public CardSnapshot()
        {
        }
        public CardSnapshot(IGame game, Card card) : this()
        {
            Update(game, card);
        }
        #endregion 构造器

        public void Update(IGame game, Card card)
        {
            id = card.id;
            this.card = card;
            pile = card.pile;
            define = card.define;
            owner = card.owner;
            position = card.pile?.indexOf(card) ?? -1;
            propDic.Clear();
            buffs.Clear();
            buffs.AddRange(card.getBuffs().Select(b => b.clone()));
            foreach (var pair in card.getAllProps(game, true))
            {
                propDic.Add(pair.Key, pair.Value);
            }
        }
        public T getProp<T>(IGame game, string propName, bool raw = false)
        {
            T value = default;
            if (propDic.ContainsKey(propName) && propDic[propName] is T t)
                value = t;
            else if (define.hasProp(propName) && define[propName] is T dt)
                value = dt;
            if (!raw)
            {
                var engine = game as CardEngine;
                foreach (var buff in buffs)
                {
                    foreach (var modifier in buff.getModifiers(engine))
                    {
                        if (modifier is not PropModifier<T> tModi)
                            continue;
                        value = tModi.calcProp(game, this, buff, propName, value);
                    }
                }
            }
            return (T)(object)value;
        }
        public object getProp(IGame game, string propName, bool raw = false)
        {
            object value = default;
            if (propDic.ContainsKey(propName))
                value = propDic[propName];
            else if (define.hasProp(propName))
                value = define[propName];
            if (!raw)
            {
                var engine = game as CardEngine;
                foreach (var buff in buffs)
                {
                    foreach (var modifier in buff.getModifiers(engine))
                    {
                        value = modifier.calcProp(game, this, buff, propName, value);
                    }
                }
            }
            return value;
        }
        public Buff[] getBuffs()
        {
            return buffs.ToArray();
        }
        #endregion

        #region 私有方法

        #region 接口实现
        T ICardData.getProp<T>(IGame game, string propName) => getProp<T>(game, propName, false);
        object ICardData.getProp(IGame game, string propName) => getProp(game, propName, false);
        void IChangeableCard.setDefine(CardDefine define) => this.define = define;
        void IChangeableCard.moveTo(Pile to, int toPos)
        {
            pile = to;
            owner = pile?.owner;
            position = toPos;
        }
        void IChangeableCard.setProp(string propName, object value) => setPropRaw(propName, value);
        void IChangeableCard.addBuff(Buff buff) => addBuffRaw(buff);
        void IChangeableCard.removeBuff(Buff buff) => removeBuffRaw(buff);
        #endregion
        private void setPropRaw(string propName, object value)
        {
            if (propDic.ContainsKey(propName))
                propDic[propName] = value;
            else
                propDic.Add(propName, value);
        }
        private void addBuffRaw(Buff buff)
        {
            buffs.Add(buff);
        }
        private void removeBuffRaw(Buff buff)
        {
            buffs.Remove(buff);
        }
        #endregion

        #region 属性字段
        public int id { get; private set; }
        public Card card { get; private set; }
        public Pile pile { get; private set; }
        public CardDefine define { get; private set; }
        public Player owner { get; private set; }
        public int position { get; private set; }
        private Dictionary<string, object> propDic = new Dictionary<string, object>();
        private List<Buff> buffs = new List<Buff>();
        #endregion
    }
}