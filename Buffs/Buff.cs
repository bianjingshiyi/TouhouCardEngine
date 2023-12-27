using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public abstract class Buff : IBuff, IChangeableBuff
    {
        #region 公有方法

        #region 构造器
        public Buff(IEnumerable<PropModifier> modifiers = null, IEnumerable<IEffect> effects = null, IEnumerable<BuffExistLimit> existLimits = null)
        {
            if (modifiers != null)
                _modifiers.AddRange(modifiers);
            if (effects != null)
                _effects.AddRange(effects);
            if (existLimits != null)
                _existLimits.AddRange(existLimits);
        }
        #endregion

        #region 属性
        public object getProp(string propName)
        {
            if (propDict.TryGetValue(propName, out object value))
            {
                return value;
            }
            return null;
        }
        public T getProp<T>(string propName)
        {
            if (propDict.TryGetValue(propName, out object value) && value is T t)
                return t;
            else
                return default;
        }
        public void setProp(string propName, object value)
        {
            propDict[propName] = value;
        }
        #endregion

        #region 属性可见性
        public Task<EventArg> setPropInvisibleTo(CardEngine game, string propName, Player player, bool invisible)
        {
            return ChangeBuffPropVisibilityEventDefine.doEvent(game, this, propName, player, invisible);
        }

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

        public PropModifier[] getModifiers()
        {
            return _modifiers.ToArray();
        }
        public IEffect[] getEffects()
        {
            return _effects.ToArray();
        }
        public BuffExistLimit[] getExistLimits()
        {
            return _existLimits.ToArray();
        }
        public async Task enable(CardEngine game, Card card)
        {
            var effects = _effects;
            var existLimits = _existLimits;
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    if (effect is IPileRangedEffect pileEffect)
                    {
                        if (pileEffect.getPiles().Contains(card.pile?.name))
                            await effect.onEnable(game, card, this);
                    }
                    else
                    {
                        await effect.onEnable(game, card, this);
                    }
                }
            }
            if (existLimits != null)
            {
                foreach (var limit in existLimits)
                {
                    limit.apply(game, card, this);
                }
            }
        }
        public async Task disable(CardEngine game, Card card)
        {
            var effects = _effects;
            var existLimits = _existLimits;
            foreach (var effect in effects)
            {
                await effect.onDisable(game, card, this);
            }
            if (existLimits != null)
            {
                foreach (var limit in existLimits)
                {
                    limit.remove(game, card, this);
                }
            }
        }
        public abstract Buff clone();
        #endregion

        #region 私有方法

        #region 构造器
        protected Buff(Buff other)
        {
            instanceID = other.instanceID;
            card = other.card;
            foreach (var pair in other.propDict)
            {
                propDict.Add(pair.Key, pair.Value);
            }
            _modifiers.AddRange(other._modifiers);
            _existLimits.AddRange(other._existLimits.Select(e => e.clone()));
            _effects.AddRange(other._effects);
            foreach (var pair in other._invisibleProps)
            {
                _invisibleProps.Add(pair.Key, pair.Value.ToList());
            }
        }
        #endregion

        #region 接口实现
        void IChangeableBuff.setProp(string propName, object value) => setProp(propName, value);
        void IChangeableBuff.setPropInvisible(string propName, Player player, bool invisible) => setPropInvisible(propName, player, invisible);
        #endregion
        internal bool setPropInvisible(string propName, Player player, bool invisible)
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
        [Obsolete]
        public abstract int id { get; }
        public int instanceID { get; set; }
        public Card card { get; set; }
        public virtual bool canClone { get; set; } = true;
        protected List<PropModifier> _modifiers = new List<PropModifier>();
        protected List<IEffect> _effects = new List<IEffect>();
        protected List<BuffExistLimit> _existLimits = new List<BuffExistLimit>();
        private Dictionary<string, object> propDict = new Dictionary<string, object>();
        private Dictionary<string, List<Player>> _invisibleProps { get; } = new Dictionary<string, List<Player>>();
        #endregion

    }
}