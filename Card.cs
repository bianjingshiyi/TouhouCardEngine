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
        #endregion

        #region 属性
        public async Task<PropChangeEventArg> setProp(IGame game, string propName, object value)
        {
            if (game != null && game.triggers != null)
            {
                return await game.triggers.doEvent(new PropChangeEventArg() { card = this, propName = propName, beforeValue = getProp(game, propName), value = value }, arg =>
                {
                    var argCard = arg.card;
                    var argName = arg.propName;
                    var beforeValueRaw = getProp(game, argName, true);
                    var argValue = arg.value;

                    argCard.setPropRaw(argName, argValue);
                    game.triggers.addChange(new CardPropChange(argCard, argName, beforeValueRaw, argValue));

                    game.logger?.logTrace("Game", $"{argCard}的属性{argName}=>{StringHelper.propToString(argValue)}");
                    return Task.CompletedTask;
                });
            }
            else
            {
                setPropRaw(propName, value);
                return default;
            }
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
        public Task<AddBuffEventArg> addBuff(IGame game, Buff buff)
        {
            if (buff == null)
                throw new ArgumentNullException(nameof(buff));

            async Task func(AddBuffEventArg arg)
            {
                var argCard = arg.card;
                var argBuff = arg.buff;

                // 如果有属性修正器的属性和Buff关联，记录与其有关的卡牌属性的值。
                var modifiers = argBuff.getModifiers();
                List<CardModifierState> modifierStates = new List<CardModifierState>();
                foreach (var modifier in modifiers)
                {
                    var state = new CardModifierState(game, modifier, argCard, argBuff);
                    modifierStates.Add(state);
                    // 调用beforeAdd。
                    await modifier.beforeAdd(game, argCard, buff);
                }

                // 添加增益。
                game?.logger?.logTrace("Buff", $"{argCard}获得增益{argBuff}");
                var buffId = (argCard.buffList.Count > 0 ? argCard.buffList.Max(b => b.instanceID) : 0) + 1;
                argBuff.card = argCard;
                argBuff.instanceID = buffId;
                argCard.addBuffRaw(argBuff);
                game.triggers.addChange(new AddBuffChange(argCard, argBuff));

                // 启用增益。
                CardEngine engine = game as CardEngine;
                await argBuff.enable(engine, argCard);

                // 更新与该增益属性名绑定的修改器的值。
                foreach (var state in modifierStates)
                {
                    var modifier = state.modifier;
                    // 调用afterAdd。
                    await modifier.afterAdd(game, argCard, argBuff);
                    // 更新卡牌属性变动，以及修改器的值。
                    await modifier.updateValue(game, argCard, argBuff, state.cardBeforeProperty, state.modifierBeforeValue);
                }

            }
            var eventArg = new AddBuffEventArg(this, buff);
            return game.triggers.doEvent(eventArg, func);
        }
        public async Task removeBuff(IGame game)
        {
            while (buffList.Count > 0)
            {
                await removeBuff(game, buffList[0]);
            }
        }
        public Task<RemoveBuffEventArg> removeBuff(IGame game, Buff buff)
        {
            if (buff == null)
                return null;
            var eventArg = new RemoveBuffEventArg(this, buff);
            return game.triggers.doEvent(eventArg, func);
            async Task func(RemoveBuffEventArg arg)
            {
                var argCard = arg.card;
                var argBuff = arg.buff;
                if (argCard.buffList.Contains(argBuff))
                {
                    // 如果有属性修正器的属性和Buff关联，记录与其有关的卡牌属性的值。
                    var modifiers = argBuff.getModifiers();
                    List<CardModifierState> modifierStates = new List<CardModifierState>();
                    foreach (var modifier in modifiers)
                    {
                        var state = new CardModifierState(game, modifier, argCard, argBuff);
                        modifierStates.Add(state);
                        // 调用BeforeRemove。
                        await modifier.beforeRemove(game, argCard, argBuff);
                    }

                    // 移除增益
                    game?.logger?.logTrace("Buff", $"{argCard}移除增益{argBuff}");
                    argCard.buffList.Remove(argBuff);
                    game.triggers.addChange(new RemoveBuffChange(argCard, argBuff));

                    var engine = game as CardEngine;
                    // 禁用增益
                    await argBuff.disable(engine, argCard);
                    arg.removed = true;

                    foreach (var state in modifierStates)
                    {
                        var modifier = state.modifier;
                        // 调用AfterRemove。
                        await modifier.afterRemove(game, argCard, argBuff);
                        // 更新卡牌属性。
                        await modifier.updateCardProp(game, argCard, state.cardBeforeProperty);
                    }
                }
            }
        }
        public Task<int> removeBuff(IGame game, int buffId)
        {
            return removeBuff(game, getBuffs(b => b.id == buffId));
        }
        public async Task<int> removeBuff(IGame game, IEnumerable<Buff> buffs)
        {
            int count = 0;
            foreach (var buff in buffs)
            {
                bool removed = (await removeBuff(game, buff))?.removed ?? false;
                if (removed)
                    count++;
            }
            return count;
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
        public bool containBuff(int buffId)
        {
            return buffList.Exists(b => b.id == buffId);
        }
        public bool containBuff(Buff buff)
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
        public Task<SetDefineEventArg> setDefine(IGame game, CardDefine define)
        {
            return game.triggers.doEvent(new SetDefineEventArg() { card = this, beforeDefine = this.define, afterDefine = define }, async arg =>
            {
                var argCard = arg.card;
                var argDefine = arg.afterDefine;
                var argBeforeDefine = arg.beforeDefine;
                var argAfterDefine = arg.afterDefine;

                //禁用之前的所有效果
                foreach (var effect in argCard.define.getEffects())
                {
                    await effect.onDisable(game, argCard, null);
                }
                //更换define
                argCard.setDefineRaw(argDefine);
                game.triggers.addChange(new SetCardDefineChange(argCard, argBeforeDefine, argAfterDefine));
                //激活效果
                foreach (var effect in argCard.define.getEffects())
                {
                    if (effect is IPileRangedEffect pileEffect)
                    {
                        if (pileEffect.piles.Contains(argCard.pile?.name))
                            await effect.onEnable(game, argCard, null);
                    }
                    else
                    {
                        await effect.onEnable(game, argCard, null);
                    }
                }
            });
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
            return $"{{card:{define.cardPoolId},{define.id}}}";
        }
        #endregion

        #endregion

        #region 私有方法

        #region 接口实现
        async Task<IPropChangeEventArg> ICard.setProp(IGame game, string propName, object value) => await setProp(game, propName, value);
        void IChangeableCard.setProp(string propName, object value) => setPropRaw(propName, value);
        void IChangeableCard.setDefine(CardDefine define) => setDefineRaw(define);
        void IChangeableCard.addBuff(Buff buff) => addBuffRaw(buff);
        void IChangeableCard.removeBuff(int buffInstanceId) => removeBuffRaw(buffInstanceId);
        IChangeableBuff IChangeableCard.getBuff(int instanceId) => buffList.FirstOrDefault(b => b.instanceID == instanceId);
        void IChangeableCard.moveTo(Pile to, int position) => pile.moveCardRaw(this, to, position);
        #endregion

        private void setPropRaw(string propName, object value)
        {
            propDic[propName] = value;
        }
        private void setDefineRaw(CardDefine define)
        {
            this.define = define;
        }
        private void addBuffRaw(Buff buff)
        {
            buffList.Add(buff);
        }
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

        #region 内嵌类

        #region 事件
        public class SetDefineEventArg : EventArg, ICardEventArg
        {
            public CardDefine beforeDefine
            {
                get => getVar<CardDefine>(VAR_BEFORE_DEFINE);
                set => setVar(VAR_BEFORE_DEFINE, value);
            }
            public Card card
            {
                get => getVar<Card>(VAR_CARD);
                set => setVar(VAR_CARD, value);
            }
            public CardDefine afterDefine
            {
                get => getVar<CardDefine>(VAR_AFTER_DEFINE);
                set => setVar(VAR_AFTER_DEFINE, value);
            }
            ICard ICardEventArg.getCard()
            {
                return card;
            }
            public override void Record(IGame game, EventRecord record)
            {
                record.setVar(VAR_BEFORE_DEFINE, beforeDefine);
                record.setCardState(VAR_CARD, card);
                record.setVar(VAR_AFTER_DEFINE, afterDefine);
            }
            public const string VAR_BEFORE_DEFINE = "beforeDefine";
            public const string VAR_CARD = "card";
            public const string VAR_AFTER_DEFINE = "afterDefine";
        }
        [EventChildren(typeof(PropChangeEventArg))]
        public class AddBuffEventArg : EventArg, ICardEventArg
        {
            public Card card
            {
                get => getVar<Card>(VAR_CARD);
                set => setVar(VAR_CARD, value);
            }
            public Buff buff
            {
                get => getVar<Buff>(VAR_BUFF);
                set => setVar(VAR_BUFF, value);
            }
            public AddBuffEventArg()
            {
            }
            public AddBuffEventArg(Card card, Buff buff)
            {
                this.card = card;
                this.buff = buff;
            }
            public ICard getCard(IGame game, IPlayer viewer)
            {
                return card;
            }
            public ICard[] getTargets(IGame game, IPlayer viewer)
            {
                return null;
            }
            ICard ICardEventArg.getCard()
            {
                return card;
            }
            public override void Record(IGame game, EventRecord record)
            {
                record.setCardState(VAR_CARD, card);
                record.setVar(VAR_BUFF, buff);
            }
            public object[] localizeStringArgs(IGame game, IPlayer viewer)
            {
                return new object[] { card.getFormatString() };
            }
            const string TEXT_TEMPLATE = "卡牌{0}添加增益";
            public string toString(IGame game, IPlayer viewer)
            {
                //TODO:可见性问题
                return string.Format(TEXT_TEMPLATE, localizeStringArgs(game, viewer));
            }
            public override EventVariableInfo[] getBeforeEventVarInfos()
            {
                return new EventVariableInfo[]
                {
                    new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                    new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff) },
                };
            }
            public override EventVariableInfo[] getAfterEventVarInfos()
            {
                return new EventVariableInfo[]
                {
                    new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                    new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff) },
                };
            }
            public const string VAR_CARD = "卡牌";
            public const string VAR_BUFF = "增益";
        }
        [EventChildren(typeof(PropChangeEventArg))]
        public class RemoveBuffEventArg : EventArg, ICardEventArg
        {
            public Card card
            {
                get => getVar<Card>(VAR_CARD);
                set => setVar(VAR_CARD, value);
            }
            public Buff buff
            {
                get => getVar<Buff>(VAR_BUFF);
                set => setVar(VAR_BUFF, value);
            }
            public bool removed
            {
                get => getVar<bool>(VAR_REMOVED);
                set => setVar(VAR_REMOVED, value);
            }
            public RemoveBuffEventArg()
            {
            }
            public RemoveBuffEventArg(Card card, Buff buff)
            {
                this.card = card;
                this.buff = buff;
            }
            public ICard getCard(IGame game, IPlayer viewer)
            {
                return card;
            }
            public ICard[] getTargets(IGame game, IPlayer viewer)
            {
                return null;
            }
            ICard ICardEventArg.getCard()
            {
                return card;
            }
            public override void Record(IGame game, EventRecord record)
            {
                record.setCardState(VAR_CARD, card);
                record.setVar(VAR_BUFF, buff);
            }
            public object[] localizeStringArgs(IGame game, IPlayer viewer)
            {
                return new object[] { card.getFormatString() };
            }
            const string TEXT_TEMPLATE = "卡牌{0}移除增益";
            public string toString(IGame game, IPlayer viewer)
            {
                //TODO:可见性问题
                return string.Format(TEXT_TEMPLATE, localizeStringArgs(game, viewer));
            }
            public override EventVariableInfo[] getBeforeEventVarInfos()
            {
                return new EventVariableInfo[]
                {
                    new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                    new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff) },
                };
            }
            public override EventVariableInfo[] getAfterEventVarInfos()
            {
                return new EventVariableInfo[]
                {
                    new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                    new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff) },
                    new EventVariableInfo() { name = VAR_REMOVED, type = typeof(bool) },
                };
            }
            public const string VAR_CARD = "卡牌";
            public const string VAR_BUFF = "增益";
            public const string VAR_REMOVED = "是否成功移除";
        }
        public class PropChangeEventArg : EventArg, IPropChangeEventArg, ICardEventArg
        {
            public Card card
            {
                get => getVar<Card>(VAR_CARD);
                set => setVar(VAR_CARD, value);
            }
            public string propName
            {
                get => getVar<string>(VAR_PROP_NAME);
                set => setVar(VAR_PROP_NAME, value);
            }
            public object beforeValue
            {
                get => getVar<object>(VAR_VALUE_BEFORE);
                set => setVar(VAR_VALUE_BEFORE, value);
            }
            public object value
            {
                get => getVar<object>(VAR_VALUE_AFTER);
                set => setVar(VAR_VALUE_AFTER, value);
            }
            ICard ICardEventArg.getCard() => card;
            ICard IPropChangeEventArg.card => card;
            string IPropChangeEventArg.propName => propName;
            object IPropChangeEventArg.beforeValue => beforeValue;
            object IPropChangeEventArg.value => value;
            public override void Record(IGame game, EventRecord record)
            {
                record.setCardState(VAR_CARD, card);
                record.setVar(VAR_PROP_NAME, propName);
                record.setVar(VAR_VALUE_BEFORE, beforeValue);
                record.setVar(VAR_VALUE_AFTER, value);
            }
            public const string VAR_CARD = "card";
            public const string VAR_VALUE_BEFORE = "beforeValue";
            public const string VAR_PROP_NAME = "propName";
            public const string VAR_VALUE_AFTER = "value";
        }
        #endregion

        #endregion
    }
}