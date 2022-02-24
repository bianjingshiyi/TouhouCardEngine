using System;
using System.Linq;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public class Player : IPlayer
    {
        #region 公有方法
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
        public void setProp<T>(string propName, T value)
        {
            propDict[propName] = value;
        }
        public void setProp(string propName, PropertyChangeType changeType, int value)
        {
            if (changeType == PropertyChangeType.set)
                propDict[propName] = value;
            else if (changeType == PropertyChangeType.add)
                propDict[propName] = getProp<int>(propName) + value;
        }
        public void setProp(string propName, PropertyChangeType changeType, float value)
        {
            if (changeType == PropertyChangeType.set)
                propDict[propName] = value;
            else if (changeType == PropertyChangeType.add)
                propDict[propName] = getProp<int>(propName) + value;
        }
        public void setProp(string propName, PropertyChangeType changeType, string value)
        {
            if (changeType == PropertyChangeType.set)
                propDict[propName] = value;
            else if (changeType == PropertyChangeType.add)
                propDict[propName] = getProp<string>(propName) + value;
        }
        public Pile this[string pileName]
        {
            get { return getPile(pileName); }
        }
        public void addPile(Pile pile)
        {
            pileList.Add(pile);
            pile.owner = this;
            foreach (Card card in pile)
            {
                card.owner = this;
            }
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
        public override string ToString()
        {
            return name;
        }
        public static implicit operator Player[](Player player)
        {
            return new Player[] { player };
        }
        #region 动作定义
        [ActionNodeMethod("GetPile", "Player")]
        [return: ActionNodeParam("Pile")]
        public static Pile getPile([ActionNodeParam("Player")] Player player, [ActionNodeParam("PileName")] string pileName)
        {
            return player.getPile(pileName);
        }
        #endregion
        #endregion
        #region 属性字段
        int IPlayer.id => id;
        public int id;
        public string name;
        public Dictionary<string, object> propDict { get; } = new Dictionary<string, object>();
        public List<Pile> pileList { get; } = new List<Pile>();
        #endregion
    }
}