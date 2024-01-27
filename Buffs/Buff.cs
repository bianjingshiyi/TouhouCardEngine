using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class Buff : IBuff, IChangeableBuff
    {
        #region 公有方法

        #region 构造器
        public Buff(BuffDefine define)
        {
            this.define = define;
            if (define.existLimitList != null)
                _existLimits.AddRange(define.existLimitList.Select(e => new BuffExistLimit(e)));
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
            if (define == null)
                return Array.Empty<PropModifier>();
            return define.propModifierList.ToArray();
        }
        public Effect[] getEffects()
        {
            if (define == null)
                return Array.Empty<Effect>();
            return define.getEffects();
        }
        public BuffExistLimit[] getExistLimits()
        {
            return _existLimits.ToArray();
        }
        public async Task enable(CardEngine game, Card card)
        {
            var effects = getEffects();
            var existLimits = getExistLimits();
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    await effect.updateEnable(game, card, this);
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
            var effects = getEffects();
            var existLimits = getExistLimits();
            foreach (var effect in effects)
            {
                await effect.disable(game, card, this);
            }
            if (existLimits != null)
            {
                foreach (var limit in existLimits)
                {
                    limit.remove(game, card, this);
                }
            }
        }
        public Buff clone()
        {
            return new Buff(this);
        }
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
            define = other.define;
            _existLimits.AddRange(other._existLimits.Select(e => e.clone()));
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
        public int instanceID { get; set; }
        public BuffDefine define { get; set; }
        public Card card { get; set; }
        public virtual bool canClone { get; set; } = true;
        protected List<BuffExistLimit> _existLimits = new List<BuffExistLimit>();
        private Dictionary<string, object> propDict = new Dictionary<string, object>();
        private Dictionary<string, List<Player>> _invisibleProps { get; } = new Dictionary<string, List<Player>>();
        #endregion

    }
}