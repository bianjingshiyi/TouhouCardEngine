using System;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class CardSnapshot : ITrackableCard
    {
        public CardSnapshot()
        {
        }
        public CardSnapshot(IGame game, Card card) : this()
        {
            Update(game, card);
        }

        public void Update(IGame game, Card card)
        {
            id = card.id;
            this.card = card;
            pile = card.pile;
            define = card.define;
            owner = card.owner;
            position = card.pile?.indexOf(card) ?? -1;
            propDic.Clear();
            buffs = card.getBuffs();
            foreach (var pair in card.getAllProps(game))
            {
                propDic.Add(pair.Key, pair.Value);
            }
        }
        public T getProp<T>(string propName)
        {
            T value = default;
            if (propDic.ContainsKey(propName) && propDic[propName] is T t)
                value = t;
            else if (define.hasProp(propName) && define[propName] is T dt)
                value = dt;
            return (T)(object)value;
        }
        public object getProp(string propName)
        {
            object value = default;
            if (propDic.ContainsKey(propName))
                value = propDic[propName];
            else if (define.hasProp(propName))
                value = define[propName];
            return value;
        }
        public void setProp(string propName, object value)
        {
            if (propDic.ContainsKey(propName))
                propDic[propName] = value;
            else
                propDic.Add(propName, value);
        }
        public void setDefine(CardDefine define)
        {
            this.define = define;
        }
        public void moveTo(Pile to, int toPos)
        {
            pile = to;
            owner = pile.owner;
            position = toPos;
        }
        public Buff[] getBuffs()
        {
            return buffs;
        }
        public void setBuffs(Buff[] buffs)
        {
            this.buffs = buffs;
        }
        public int id { get; private set; }
        public Card card { get; private set; }
        public Pile pile { get; private set; }
        public CardDefine define { get; private set; }
        public Player owner { get; private set; }
        public int position { get; private set; }
        private Dictionary<string, object> propDic = new Dictionary<string, object>();
        private Buff[] buffs = Array.Empty<Buff>();
    }
}