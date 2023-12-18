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
            _visiblePlayers.AddRange(card.getCardVisiblePlayers());
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
                value = modifyProp(game, propName, value);
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
                foreach (var buff in buffs)
                {
                    foreach (var modifier in buff.getModifiers())
                    {
                        value = modifier.calcProp(game, this, buff, propName, value);
                    }
                }
            }
            return value;
        }
        public T modifyProp<T>(IGame game, string propName, T value)
        {
            foreach (var buff in buffs)
            {
                foreach (var modifier in buff.getModifiers())
                {
                    if (modifier is not PropModifier<T> tModi)
                        continue;
                    value = tModi.calcProp(game, this, buff, propName, value);
                }
            }
            return value;
        }
        public object modifyProp(IGame game, string propName, object value)
        {
            foreach (var buff in buffs)
            {
                foreach (var modifier in buff.getModifiers())
                {
                    value = modifier.calcProp(game, this, buff, propName, value);
                }
            }
            return value;
        }
        public Buff[] getBuffs()
        {
            return buffs.ToArray();
        }

        #region 卡牌可见性
        public bool isCardVisibleTo(Player player)
        {
            return _visiblePlayers.Contains(player);
        }
        public Player[] getCardVisiblePlayers()
        {
            return _visiblePlayers.ToArray();
        }
        #endregion

        #region 属性可见性
        public bool isPropInvisibleTo(string propName, Player player)
        {
            if (_invisibleProps.TryGetValue(propName, out var playerList))
            {
                return playerList.Contains(player);
            }
            return false;
        }
        public string[] getInvisibleProps()
        {
            return _invisibleProps.Keys.ToArray();
        }
        public Player[] getPropInvisiblePlayers(string propName)
        {
            if (_invisibleProps.TryGetValue(propName, out var playerList))
            {
                return playerList.ToArray();
            }
            return null;
        }
        #endregion

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
        void IChangeableCard.removeBuff(int buffInstanceId) => removeBuffRaw(buffInstanceId);
        IChangeableBuff IChangeableCard.getBuff(int id) => buffs.FirstOrDefault(b => b.instanceID == id);
        void IChangeableCard.setVisible(Player player, bool visible) => setVisibleRaw(player, visible);
        void IChangeableCard.setPropInvisible(string propName, Player player, bool invisible) => setPropInvisibleRaw(propName, player, invisible);
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
        private void removeBuffRaw(int buffInstanceId)
        {
            buffs.RemoveAll(b => b.instanceID == buffInstanceId);
        }
        private bool setVisibleRaw(Player player, bool visible)
        {
            if (visible)
            {
                if (!_visiblePlayers.Contains(player))
                {
                    _visiblePlayers.Add(player);
                    return true;
                }
            }
            else
            {
                if (_visiblePlayers.Remove(player))
                {
                    return true;
                }
            }
            return false;
        }
        private bool setPropInvisibleRaw(string propName, Player player, bool invisible)
        {
            if (invisible)
            {
                if (!_invisibleProps.TryGetValue(propName, out var playerList))
                {
                    playerList = new List<Player>();
                    _invisibleProps.Add(propName, playerList);
                }
                if (!playerList.Contains(player))
                {
                    playerList.Add(player);
                    return true;
                }
            }
            else
            {
                if (_invisibleProps.TryGetValue(propName, out var playerList))
                {
                    if (playerList.Remove(player))
                    {
                        if (playerList.Count <= 0)
                        {
                            _invisibleProps.Remove(propName);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region 属性字段
        public int id { get; private set; }
        public Card card { get; private set; }
        public Pile pile { get; private set; }
        public CardDefine define { get; private set; }
        public Player owner { get; private set; }
        public int position { get; private set; }
        private List<Player> _visiblePlayers { get; } = new List<Player>();
        private Dictionary<string, List<Player>> _invisibleProps { get; } = new Dictionary<string, List<Player>>();
        private Dictionary<string, object> propDic = new Dictionary<string, object>();
        private List<Buff> buffs = new List<Buff>();
        #endregion
    }
}