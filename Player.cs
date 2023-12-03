using System;
using System.Linq;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
using TouhouCardEngine.Histories;

namespace TouhouCardEngine
{
    [Serializable]
    public class Player : IPlayer, IChangeablePlayer
    {
        #region 公有方法

        #region 构造器
        public Player(int id, string name, params Pile[] piles)
        {
            this.id = id;
            this.name = name;
            foreach (Pile pile in piles)
            {
                pile.owner = this;
            }
            pileList.AddRange(piles);
        }
        public Player() : this(0, null)
        {
        }
        #endregion

        #region 属性
        public T getProp<T>(string propName)
        {
            if (getProp(propName) is T t)
                return t;
            return default;
        }
        public object getProp(string propName)
        {
            if (propDict.ContainsKey(propName))
                return propDict[propName];
            return null;
        }
        public bool hasProp(string propName)
        {
            return propDict.ContainsKey(propName);
        }
        public void setProp(IGame game, string propName, object value)
        {
            var beforeValue = getProp(propName);
            setPropRaw(propName, value);
            game.triggers.addChange(new PlayerPropChange(this, propName, beforeValue, value));
        }
        public void setProp<T>(IGame game, string propName, T value)
        {
            setProp(game, propName, (object)value);
        }
        #endregion

        #region 牌堆
        public void addPile(IGame game, Pile pile)
        {
            addPileRaw(pile);
            game.triggers.addChange(new AddPileChange(this, pile));
        }
        public bool removePile(IGame game, Pile pile)
        {
            if (removePileRaw(pile))
            {
                game.triggers.addChange(new RemovePileChange(this, pile));
                return true;
            }
            return false;
        }
        public Pile getPile(string name)
        {
            return pileList.FirstOrDefault(e => { return e.name == name; });
        }
        public Pile[] getPiles()
        {
            return pileList.ToArray();
        }
        public Pile[] getPiles(IEnumerable<string> pileNames)
        {
            if (pileNames == null)
                return new Pile[0];
            List<Pile> pileList = new List<Pile>();
            foreach (var pileName in pileNames)
            {
                Pile pile = getPile(pileName);
                if (pile != null)
                {
                    pileList.Add(pile);
                }
            }
            return pileList.ToArray();
        }
        #endregion

        public override string ToString()
        {
            return name;
        }
        public static implicit operator Player[](Player player)
        {
            return new Player[] { player };
        }
        #endregion

        #region 私有方法

        #region 接口实现
        void IChangeablePlayer.addPile(Pile pile) => addPileRaw(pile);
        void IChangeablePlayer.removePile(Pile pile) => removePileRaw(pile);
        void IChangeablePlayer.setProp(string propName, object value) => setPropRaw(propName, value);
        #endregion

        private void addPileRaw(Pile pile)
        {
            pileList.Add(pile);
            pile.owner = this;
            foreach (Card card in pile)
            {
                card.owner = this;
            }
        }
        private bool removePileRaw(Pile pile)
        {
            if (pileList.Remove(pile))
            {
                pile.owner = null;
                foreach (Card card in pile)
                {
                    card.owner = null;
                }
                return true;
            }
            return false;
        }
        private void setPropRaw(string propName, object value)
        {
            propDict[propName] = value;
        }

        #endregion

        #region 属性字段
        public int id { get; }
        public string name { get; }
        public Pile this[string pileName] => getPile(pileName);
        private Dictionary<string, object> propDict { get; } = new Dictionary<string, object>();
        private List<Pile> pileList { get; } = new List<Pile>();
        #endregion
    }
}