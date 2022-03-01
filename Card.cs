using System;
using System.Linq;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    [Serializable]
    public class Card : ICard
    {
        #region 共有方法
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
        public Pile getPile(CardEngine game)
        {
            for (int i = 0; i < game.playerCount; i++)
            {
                Player player = game.getPlayerAt(i);
                foreach (Pile pile in player.getPiles())
                {
                    if (pile.Contains(this))
                        return pile;
                }
            }
            return null;
        }
        #endregion
        #region 属性字段
        int ICard.id => id;
        /// <summary>
        /// 卡片的id
        /// </summary>
        public int id = 0;
        #endregion
        public Player owner { get; internal set; } = null;
        /// <summary>
        /// 卡片所在的牌堆
        /// </summary>
        public Pile pile { get; internal set; } = null;
        public CardDefine define { get; private set; } = null;
        ICardDefine ICard.define
        {
            get { return define; }
        }
        List<PropModifier> modifierList { get; } = new List<PropModifier>();
        List<Buff> buffList { get; } = new List<Buff>();
        int _lastBuffId = 0;
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
        public Task<SetDefineEventArg> setDefine(IGame game, CardDefine define)
        {
            return game.triggers.doEvent(new SetDefineEventArg() { card = this, beforeDefine = this.define, afterDefine = define }, async arg =>
            {
                Card card = arg.card;
                define = arg.afterDefine;
                //禁用被动
                foreach (var effect in card.define.getEffects().OfType<IPassiveEffect>())
                {
                    await effect.onDisable(game, card, null);
                }
                //更换define
                card.define = define;
                //激活被动
                foreach (var effect in card.define.getEffects().OfType<IPassiveEffect>())
                {
                    await effect.onEnable(game, card, null);
                }
            });
        }
        public class SetDefineEventArg : EventArg, IDescribableEventArg
        {
            public CardDefine beforeDefine;
            public Card card;
            public CardDefine afterDefine;//TODO:卡牌快照
            public ICard getCard(IGame game, IPlayer viewer)
            {
                return card;
            }
            public ICard[] getTargets(IGame game, IPlayer viewer)
            {
                return null;
            }

            public object[] localizeStringArgs(IGame game, IPlayer viewer)
            {
                return new object[] { card.getFormatString() };
            }
            const string TEXT_TEMPLATE = "一张牌变形为{0}";

            public string localizeTemplateString(IGame game, IPlayer viewer)
            {
                return TEXT_TEMPLATE;
            }

            public string toString(IGame game, IPlayer viewer)
            {
                //TODO:可见性问题
                return string.Format(TEXT_TEMPLATE, localizeStringArgs(game, viewer));
            }
        }
        public PropModifier[] getModifiers()
        {
            return modifierList.ToArray();
        }
        public Task<IAddModiEventArg> addModifier(IGame game, PropModifier modifier)
        {
            if (game != null && game.triggers != null)
            {
                return game.triggers.doEvent<IAddModiEventArg>(new AddModiEventArg() { game = game, card = this, modifier = modifier, valueBefore = getProp(game, modifier.getPropName()) }, async arg =>
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
                    game?.logger?.logTrace(nameof(PropModifier), card + "获得属性修正" + modifier + "=>" + StringHelper.propToString(value));
                    await game.triggers.doEvent(new PropChangeEventArg() { game = game, card = card, propName = modifier.getPropName(), beforeValue = beforeValue, value = value }, arg2 =>
                    {
                        arg2.game?.logger?.logTrace(nameof(Card), arg2.card + "的属性" + arg2.propName + "=>" + StringHelper.propToString(arg2.value));
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
                        game?.logger?.logTrace("PropModifier", card + "移除属性修正" + modifier + "=>" + StringHelper.propToString(value));
                        await game.triggers.doEvent(new PropChangeEventArg() { game = game, card = card, propName = modifier.getPropName(), beforeValue = beforeValue, value = value },
                        arg2 =>
                        {
                            arg2.game?.logger?.logTrace(nameof(Card), arg2.card + "的属性" + arg2.propName + "=>" + StringHelper.propToString(arg2.value));
                            return Task.CompletedTask;
                        });
                    });
                else
                {
                    await modifier.beforeRemove(game, this);
                    modifierList.Remove(modifier);
                    await modifier.afterRemove(game, this);
                    object prop = getProp(game, modifier.getPropName());
                    game?.logger?.logTrace("PropModifier", this + "移除属性修正" + modifier + "=>" + StringHelper.propToString(prop));
                    return default;
                }
            }
            else
                return default;
        }
        public class RemoveModiEventArg : EventArg, IRemoveModiEventArg
        {
            public Card card;
            public PropModifier modifier;

            ICard IRemoveModiEventArg.card => card;

            IPropModifier IRemoveModiEventArg.modifier => modifier;
        }
        public async Task addBuff(IGame game, Buff buff)
        {
            if (buff == null)
                throw new ArgumentNullException(nameof(buff));
            game?.logger?.logTrace("Buff", this + "获得增益" + buff);
            buffList.Add(buff);
            _lastBuffId++;
            buff.instanceID = _lastBuffId;
            if (buff.getPropertyModifiers(game as CardEngine) != null)
            {
                foreach (var modifier in buff.getPropertyModifiers(game as CardEngine))
                {
                    await addModifier(game, modifier);
                }
            }
            if (buff.getEffects(game as CardEngine) != null)
            {
                foreach (var efffect in buff.getEffects(game as CardEngine))
                {
                    await efffect.onEnable(game, this, buff);
                }
            }
        }
        public async Task removeBuff(IGame game)
        {
            while (buffList.Count > 0)
            {
                await removeBuff(game, buffList[0]);
            }
        }
        public async Task<bool> removeBuff(IGame game, Buff buff)
        {
            if (buffList.Contains(buff))
            {
                game?.logger?.logTrace("Buff", this + "移除增益" + buff);
                buffList.Remove(buff);
                foreach (var modifier in buff.getPropertyModifiers(game as CardEngine))
                {
                    await removeModifier(game, modifier);
                }
                foreach (var effect in buff.getEffects(game as CardEngine))
                {
                    await effect.onDisable(game, this, buff);
                }
                return true;
            }
            else
                return false;
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
                if (await removeBuff(game, buff))
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
        #region 动作定义
        [ActionNodeMethod("AddIntModifier", "Card")]
        [return: ActionNodeParam("Modifier")]
        public static IntPropModifier addIntModifier(IGame game, [ActionNodeParam("Card")] Card card,
                                                     [ActionNodeParam("PropertyName")] string propName,
                                                     [ActionNodeParam("Value")] int value,
                                                     [ActionNodeParam("isSet")] bool isSet)
        {
            IntPropModifier modifier = new IntPropModifier(propName, value, isSet);
            card.addModifier(game, modifier);
            return modifier;
        }
        [ActionNodeMethod("RemoveModifier", "Card")]
        [return: ActionNodeParam("RemoveModifierEvent")]
        public static Task<IRemoveModiEventArg> removeModifier(IGame game, [ActionNodeParam("Card")] Card card, [ActionNodeParam("Modifier")] PropModifier modifier)
        {
            return card.removeModifier(game, modifier);
        }
        #endregion   
        #region 属性
        public Task<IPropChangeEventArg> setProp(IGame game, string propName, object value)
        {
            if (game != null && game.triggers != null)
                return game.triggers.doEvent<IPropChangeEventArg>(new PropChangeEventArg() { card = this, propName = propName, beforeValue = getProp(game, propName), value = value }, arg =>
                {
                    Card card = arg.card as Card;
                    propName = arg.propName;
                    var v = arg.value;
                    propDic[propName] = v;
                    game.logger?.logTrace("Game", card + "的属性" + propName + "=>" + StringHelper.propToString(v));
                    return Task.CompletedTask;
                });
            else
            {
                propDic[propName] = value;
                return Task.FromResult<IPropChangeEventArg>(default);
            }
        }
        public class PropChangeEventArg : EventArg, IPropChangeEventArg
        {
            public Card card;
            public string propName;
            public object beforeValue;
            public object value;
            ICard IPropChangeEventArg.card => card;
            string IPropChangeEventArg.propName => propName;
            object IPropChangeEventArg.beforeValue => beforeValue;
            object IPropChangeEventArg.value => value;
        }
        public T getProp<T>(IGame game, string propName)
        {
            T value = default;
            if (propDic.ContainsKey(propName) && propDic[propName] is T t)
                value = t;
            else if (define.hasProp(propName) && define[propName] is T dt)
                value = dt;
            foreach (var modifier in modifierList.OfType<PropModifier<T>>().Where(mt =>
                mt.getPropName() == propName &&
                (game == null || mt.checkCondition(game, this))))
            {
                value = modifier.calc(game, this, value);
            }
            return (T)(object)value;
        }
        public object getProp(IGame game, string propName)
        {
            object value = default;
            if (propDic.ContainsKey(propName))
                value = propDic[propName];
            else if (define.hasProp(propName))
                value = define[propName];
            foreach (var modifier in modifierList.Where(m =>
                m.getPropName() == propName &&
                (game == null || m.checkCondition(game, this))))
            {
                value = modifier.calc(game, this, value);
            }
            return value;
        }
        internal Dictionary<string, object> propDic { get; } = new Dictionary<string, object>();
        #endregion
        public override string ToString()
        {
            if (define != null)
                return "Card(" + id + ")<" + define.GetType().Name + ">";
            else
                return "Card(" + id + ")";
        }
        public string getFormatString()
        {
            return "{card:" + define.id + "}";
        }
        public static implicit operator Card[](Card card)
        {
            if (card != null)
                return new Card[] { card };
            else
                return new Card[0];
        }
    }
}