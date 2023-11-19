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
                    argCard.addChange(game, new CardPropChange(argCard, argName, beforeValueRaw, argValue));

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
                foreach (var modifier in modifierList.OfType<PropModifier<T>>())
                {
                    if (modifier.getPropName() != propName)
                        continue;
                    value = modifier.calcGeneric(game, this, value);
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
                foreach (var modifier in modifierList)
                {
                    if (modifier.getPropName() != propName)
                        continue;
                    value = modifier.calc(game, this, value);
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
                foreach (var modifier in modifierList.OfType<PropModifier>())
                {
                    var propName = modifier.getPropName();
                    if (props.TryGetValue(propName, out var value))
                    {
                        props[propName] = modifier.calc(game, this, value);
                    }
                    else
                    {
                        value = define.getProp<object>(propName);
                        props.Add(propName, modifier.calc(game, this, value));
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

                game?.logger?.logTrace("Buff", $"{argCard}获得增益{argBuff}");
                argCard.addBuffRaw(argBuff);
                var buffId = Math.Max(0, argCard.getBuffs().Max(b => b.instanceID)) + 1;
                argBuff.setInfo(game, argCard, buffId);

                CardEngine engine = game as CardEngine;
                argBuff.updateModifierProps(engine);
                var propModis = argBuff.getPropertyModifiers(engine);
                var effects = argBuff.getEffects(engine);
                var existLimits = argBuff.getExistLimits(engine);
                if (propModis != null)
                {
                    foreach (var modifier in propModis)
                    {
                        await argCard.addModifier(game, modifier);
                    }
                }
                if (effects != null)
                {
                    foreach (var effect in effects)
                    {
                        if (effect is IPileRangedEffect pileEffect)
                        {
                            if (pileEffect.piles.Contains(argCard.pile?.name))
                                await effect.onEnable(game, argCard, argBuff);
                        }
                        else
                        {
                            await effect.onEnable(game, argCard, argBuff);
                        }
                    }
                }
                if (existLimits != null)
                {
                    foreach (var limit in existLimits)
                    {
                        limit.apply(engine, argCard, argBuff);
                    }
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
                var card = arg.card;
                var argBuff = arg.buff;
                if (card.buffList.Contains(argBuff))
                {
                    game?.logger?.logTrace("Buff", $"{card}移除增益{argBuff}");
                    card.buffList.Remove(argBuff);
                    var engine = game as CardEngine;
                    var existLimits = argBuff.getExistLimits(engine);
                    foreach (var modifier in argBuff.getPropertyModifiers(engine))
                    {
                        await card.removeModifier(game, modifier);
                    }
                    foreach (var effect in argBuff.getEffects(engine))
                    {
                        await effect.onDisable(game, card, argBuff);
                    }
                    if (existLimits != null)
                    {
                        foreach (var limit in existLimits)
                        {
                            limit.remove(engine, card, argBuff);
                        }
                    }
                    arg.removed = true;
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

        #region 修改器

        public PropModifier[] getModifiers()
        {
            return modifierList.ToArray();
        }
        public Task<IAddModiEventArg> addModifier(IGame game, PropModifier modifier)
        {
            if (game != null && game.triggers != null)
            {
                return game.triggers.doEvent<IAddModiEventArg>(new AddModiEventArg() { card = this, modifier = modifier, valueBefore = getProp(game, modifier.getPropName()) }, async arg =>
                {
                    Card card = arg.card as Card;
                    modifier = arg.modifier as PropModifier;
                    if (modifier == null)
                        throw new ArgumentNullException(nameof(modifier));
                    object beforeValue = card.getProp(game, modifier.getPropName());
                    await modifier.beforeAdd(game, card);
                    card.modifierList.Add(modifier);
                    await modifier.afterAdd(game, card);
                    object value = card.getProp(game, modifier.getPropName());
                    (arg as AddModiEventArg).valueAfter = value;
                    game?.logger?.logTrace(nameof(PropModifier), $"{card}获得属性修正{modifier}=>{StringHelper.propToString(value)}");
                    await game.triggers.doEvent(new PropChangeEventArg() { card = card, propName = modifier.getPropName(), beforeValue = beforeValue, value = value }, arg2 =>
                    {
                        var arg2Card = arg2.card;
                        var arg2Name = arg2.propName;
                        var arg2Value = arg2.value;
                        game?.logger?.logTrace(nameof(Card), $"{arg2Card}的属性{arg2Name}=>{StringHelper.propToString(arg2Value)}");
                        return Task.CompletedTask;
                    });
                });
            }
            else
            {
                if (modifier == null)
                    throw new ArgumentNullException(nameof(modifier));
                modifier.beforeAdd(game, this);
                modifierList.Add(modifier);
                modifier.afterAdd(game, this);
                object prop = getProp(game, modifier.getPropName());
                string propString = StringHelper.propToString(prop);
                return Task.FromResult<IAddModiEventArg>(default);
            }
        }
        public async Task<IRemoveModiEventArg> removeModifier(IGame game, PropModifier modifier)
        {
            if (modifierList.Contains(modifier))
            {
                if (game != null && game.triggers != null)
                    return await game.triggers.doEvent<IRemoveModiEventArg>(new RemoveModiEventArg() { card = this, modifier = modifier }, async arg =>
                    {
                        Card card = arg.card as Card;
                        modifier = arg.modifier as PropModifier;
                        object beforeValue = card.getProp(game, modifier.getPropName());
                        await modifier.beforeRemove(game, card);
                        card.modifierList.Remove(modifier);
                        await modifier.afterRemove(game, card);
                        object value = card.getProp(game, modifier.getPropName());
                        game?.logger?.logTrace("PropModifier", $"{card}移除属性修正{modifier}=>{StringHelper.propToString(value)}");
                        await game.triggers.doEvent(new PropChangeEventArg() { card = card, propName = modifier.getPropName(), beforeValue = beforeValue, value = value },
                        arg2 =>
                        {
                            var arg2Card = arg2.card;
                            arg2Card.addChange(game, new CardPropChange(arg2Card, arg2.propName, arg2.beforeValue, arg2.value));
                            game?.logger?.logTrace(nameof(Card), $"{arg2.card}的属性{arg2.propName}=>{StringHelper.propToString(arg2.value)}");
                            return Task.CompletedTask;
                        });
                    });
                else
                {
                    await modifier.beforeRemove(game, this);
                    modifierList.Remove(modifier);
                    await modifier.afterRemove(game, this);
                    object prop = getProp(game, modifier.getPropName());
                    game?.logger?.logTrace("PropModifier", $"{this}移除属性修正{modifier}=>{StringHelper.propToString(prop)}");
                    return default;
                }
            }
            else
                return default;
        }
        #endregion

        #region 变更
        public int addChange(IGame game, CardChange change)
        {
            game.triggers.addChange(change);
            _changes.Add(change);
            return getCurrentHistory();
        }
        public int getCurrentHistory()
        {
            return _changes.Count;
        }
        public CardChange getHistory(int index)
        {
            if (index < 0 || index >= _changes.Count)
                return null;
            return _changes[index];
        }
        public CardChange[] getHistories()
        {
            return _changes.ToArray();
        }
        public void revertToHistory(IChangeableCard trackable, int historyIndex)
        {
            if (historyIndex < 0)
                historyIndex = 0;
            for (int i = _changes.Count - 1; i >= historyIndex; i--)
            {
                var history = _changes[i];
                history.revertFor((IChangeable)trackable);
            }
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
                argCard.addChange(game, new SetCardDefineChange(argCard, argBeforeDefine, argAfterDefine));
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
        void IChangeableCard.removeBuff(Buff buff) => removeBuffRaw(buff);
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
        private void removeBuffRaw(Buff buff)
        {
            buffList.Remove(buff);
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
        private List<PropModifier> modifierList { get; } = new List<PropModifier>();
        private List<Buff> buffList { get; } = new List<Buff>();
        private Dictionary<string, object> propDic { get; } = new Dictionary<string, object>();
        private List<(IBuff buff, IEffect effect)> enabledEffects = new List<(IBuff buff, IEffect effect)>();
        private List<CardChange> _changes = new List<CardChange>();
        #endregion

        #region 内嵌类
        public class SetDefineEventArg : EventArg, ICardEventArg
        {
            public CardDefine beforeDefine;
            public Card card;
            public CardDefine afterDefine;
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
        [EventChildren(typeof(AddModiEventArg))]
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
        [EventChildren(typeof(RemoveModiEventArg))]
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
        [EventChildren(typeof(PropChangeEventArg))]
        public class AddModiEventArg : EventArg, IAddModiEventArg
        {
            public Card card;
            public object valueBefore;
            public PropModifier modifier;
            public object valueAfter;
            ICard IAddModiEventArg.card => card;
            IPropModifier IAddModiEventArg.modifier => modifier;
            object IAddModiEventArg.valueBefore => valueBefore;
            object IAddModiEventArg.valueAfter => valueAfter;
            public override void Record(IGame game, EventRecord record)
            {
                record.setCardState(VAR_CARD, card);
                record.setVar(VAR_MODIFIER, modifier);
                record.setVar(VAR_VALUE_BEFORE, valueBefore);
                record.setVar(VAR_VALUE_AFTER, valueAfter);
            }
            public const string VAR_CARD = "card";
            public const string VAR_VALUE_BEFORE = "valueBefore";
            public const string VAR_MODIFIER = "modifier";
            public const string VAR_VALUE_AFTER = "valueAfter";
        }
        [EventChildren(typeof(PropChangeEventArg))]
        public class RemoveModiEventArg : EventArg, IRemoveModiEventArg
        {
            public Card card;
            public PropModifier modifier;

            ICard IRemoveModiEventArg.card => card;

            IPropModifier IRemoveModiEventArg.modifier => modifier;
            public override void Record(IGame game, EventRecord record)
            {
                record.setCardState(VAR_CARD, card);
                record.setVar(VAR_MODIFIER, modifier);
            }
            public const string VAR_CARD = "card";
            public const string VAR_MODIFIER = "modifier";
        }
        public class PropChangeEventArg : EventArg, IPropChangeEventArg, ICardEventArg
        {
            public Card card;
            public string propName;
            public object beforeValue;
            public object value;
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
    }
}