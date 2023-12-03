using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public class Card : ICard, IChangeableCard
    {
        #region 公有方法

        #region 构造器
        public Card()
        {
        }
        public Card(CardDefine define)
        {
            if (define != null)
                this.define = define;
            else
                throw new ArgumentNullException(nameof(define));
        }
        public Card(int id)
        {
            this.id = id;
        }
        public Card(int id, CardDefine define)
        {
            this.id = id;
            if (define != null)
                this.define = define;
            else
                throw new ArgumentNullException(nameof(define));
        }
        #endregion

        #region 效果
        public void enableEffect(IBuff buff, IEffect effect)
        {
            enabledEffects.Add((buff, effect));
        }
        public void disableEffect(IBuff buff, IEffect effect)
        {
            enabledEffects.Remove((buff, effect));
        }
        public bool isEffectEnabled(IBuff buff, IEffect effect)
        {
            return enabledEffects.Contains((buff, effect));
        }
        public (IBuff buff, IEffect effect)[] getEnabledEffects()
        {
            return enabledEffects.ToArray();
        }
        #endregion

        #region 属性
        public void setProp(string propName, object value)
        {
            propDic[propName] = value;
        }
        public T getProp<T>(IGame game, string propName, bool raw)
        {
            T value = default;
            if (propDic.ContainsKey(propName) && propDic[propName] is T t)
                value = t;
            else if (define.hasProp(propName) && define[propName] is T dt)
                value = dt;
            if (!raw)
            {
                foreach (var buff in buffList)
                {
                    foreach (var modifier in buff.getModifiers())
                    {
                        if (modifier is not PropModifier<T> tModi)
                            continue;
                        value = tModi.calcProp(game, this, buff, propName, value);
                    }
                }
            }
            return (T)(object)value;
        }
        public object getProp(IGame game, string propName, bool raw)
        {
            object value = default;
            if (propDic.ContainsKey(propName))
                value = propDic[propName];
            else if (define.hasProp(propName))
                value = define[propName];
            if (!raw)
            {
                foreach (var buff in buffList)
                {
                    foreach (var modifier in buff.getModifiers())
                    {
                        value = modifier.calcProp(game, this, buff, propName, value);
                    }
                }
            }
            return value;
        }
        public T getProp<T>(IGame game, string propName)
        {
            return getProp<T>(game, propName, false);
        }
        public object getProp(IGame game, string propName)
        {
            return getProp(game, propName, false);
        }
        /// <summary>
        /// 获取所有相对于卡牌定义进行变更的属性名称。
        /// </summary>
        /// <param name="game">游戏对象。</param>
        /// <param name="raw">是否忽略属性修改器修改的属性？</param>
        /// <returns></returns>
        public Dictionary<string, object> getAllProps(IGame game, bool raw = false)
        {
            Dictionary<string, object> props = new Dictionary<string, object>(propDic);
            if (!raw)
            {
                foreach (var buff in buffList)
                {
                    foreach (var modifier in buff.getModifiers())
                    {
                        var propName = modifier.getPropName();
                        if (props.TryGetValue(propName, out var value))
                        {
                            props[propName] = modifier.calcProp(game, this, buff, propName, value);
                        }
                        else
                        {
                            value = define.getProp<object>(propName);
                            props.Add(propName, modifier.calcProp(game, this, buff, propName, value));
                        }
                    }
                }
            }
            return props;
        }
        #endregion

        #region 增益
        public void addBuff(Buff buff)
        {
            if (buff == null)
                throw new ArgumentNullException(nameof(buff));
            buffList.Add(buff);
        }
        public bool removeBuff(Buff buff)
        {
            if (buff == null)
                return false;
            return buffList.Remove(buff);
        }
        public Buff[] getBuffs()
        {
            return buffList.ToArray();
        }
        public Buff[] getBuffs(Func<Buff, bool> filter)
        {
            if (filter == null)
                return buffList.ToArray();
            else
                return buffList.Where(filter).ToArray();
        }
        public bool containsBuff(Buff buff)
        {
            return buffList.Contains(buff);
        }
        public bool hasBuff(BuffDefine buffDefine)
        {
            return buffList.Exists(b =>
                b is GeneratedBuff generatedBuff &&
                generatedBuff.defineRef.cardPoolId == buffDefine.cardPoolId &&
                generatedBuff.defineRef.defineId == buffDefine.id);
        }
        #endregion

        #region 杂项
        public Player getOwner(CardEngine game)
        {
            for (int i = 0; i < game.playerCount; i++)
            {
                Player player = game.getPlayerAt(i);
                foreach (Pile pile in player.getPiles())
                {
                    if (pile.Contains(this))
                        return player;
                }
            }
            return null;
        }
        public void setDefine(CardDefine define)
        {
            this.define = define;
        }
        public override string ToString()
        {
            if (define != null)
                return $"Card({id})<{define.GetType().Name}>";
            else
                return $"Card({id})";
        }
        public string getFormatString()
        {
            return define.getFormatString();
        }
        #endregion

        #endregion

        #region 私有方法

        #region 接口实现
        void IChangeableCard.setProp(string propName, object value) => setProp(propName, value);
        void IChangeableCard.setDefine(CardDefine define) => setDefine(define);
        void IChangeableCard.addBuff(Buff buff) => addBuff(buff);
        void IChangeableCard.removeBuff(int buffInstanceId) => removeBuffRaw(buffInstanceId);
        IChangeableBuff IChangeableCard.getBuff(int instanceId) => buffList.FirstOrDefault(b => b.instanceID == instanceId);
        void IChangeableCard.moveTo(Pile to, int position) => pile.moveCardRaw(this, to, position);
        #endregion

        private void removeBuffRaw(int buffInstanceId)
        {
            buffList.RemoveAll(b => b.instanceID == buffInstanceId);
        }
        #endregion

        #region 属性字段
        /// <summary>
        /// 卡片的id
        /// </summary>
        public int id { get; }
        public Player owner { get; internal set; } = null;
        /// <summary>
        /// 卡片所在的牌堆
        /// </summary>
        public Pile pile { get; internal set; } = null;
        public CardDefine define { get; private set; } = null;
        private List<Buff> buffList { get; } = new List<Buff>();
        private Dictionary<string, object> propDic { get; } = new Dictionary<string, object>();
        private List<(IBuff buff, IEffect effect)> enabledEffects = new List<(IBuff buff, IEffect effect)>();
        #endregion
    }
}