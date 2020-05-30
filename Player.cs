using System;
using System.Linq;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public class Player : IPlayer
    {
        public int id { get; }
        public string name { get; set; }
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
        public T getProp<T>(string propName)
        {
            if (dicProp.ContainsKey(propName) && dicProp[propName] is T)
                return (T)dicProp[propName];
            return default;
        }
        public void setProp<T>(string propName, T value)
        {
            dicProp[propName] = value;
        }
        public void setProp(string propName, PropertyChangeType changeType, int value)
        {
            if (changeType == PropertyChangeType.set)
                dicProp[propName] = value;
            else if (changeType == PropertyChangeType.add)
                dicProp[propName] = getProp<int>(propName) + value;
        }
        public void setProp(string propName, PropertyChangeType changeType, float value)
        {
            if (changeType == PropertyChangeType.set)
                dicProp[propName] = value;
            else if (changeType == PropertyChangeType.add)
                dicProp[propName] = getProp<int>(propName) + value;
        }
        public void setProp(string propName, PropertyChangeType changeType, string value)
        {
            if (changeType == PropertyChangeType.set)
                dicProp[propName] = value;
            else if (changeType == PropertyChangeType.add)
                dicProp[propName] = getProp<string>(propName) + value;
        }
        internal Dictionary<string, object> dicProp { get; } = new Dictionary<string, object>();
        public Pile this[string pileName]
        {
            get { return getPile(pileName); }
        }
        public void addPile(Pile pile)
        {
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
        private List<Pile> pileList { get; } = new List<Pile>();
        public override string ToString()
        {
            return name;
        }
        public static implicit operator Player[](Player player)
        {
            return new Player[] { player };
        }
    }
}