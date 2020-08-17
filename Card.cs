using System;
using System.Linq;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
using System.Collections;
namespace TouhouCardEngine
{
    [Serializable]
    public class Card : ICard
    {
        /// <summary>
        /// 卡片的id
        /// </summary>
        public int id { get; internal set; } = 0;
        public Player owner { get; internal set; } = null;
        /// <summary>
        /// 卡片所在的牌堆
        /// </summary>
        public Pile pile { get; internal set; } = null;
        public CardDefine define { get; } = null;
        ICardDefine ICard.define
        {
            get { return define; }
        }
        List<PropModifier> modifierList { get; } = new List<PropModifier>();
        List<Buff> buffList { get; } = new List<Buff>();
        internal Dictionary<string, object> propDic { get; } = new Dictionary<string, object>();
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
        public PropModifier[] getModifiers()
        {
            return modifierList.ToArray();
        }
        public Task<IAddModiEventArg> addModifier(IGame game, PropModifier modifier)
        {
            if (game != null && game.triggers != null)
                return game.triggers.doEvent<IAddModiEventArg>(new AddModiEventArg() { game = game, card = this, modifier = modifier }, arg =>
                {
                    Card card = arg.card as Card;
                    modifier = arg.modifier as PropModifier;
                    if (modifier == null)
                        throw new ArgumentNullException(nameof(modifier));
                    modifier.beforeAdd(game, card);
                    card.modifierList.Add(modifier);
                    modifier.afterAdd(game, card);
                    object prop = card.getProp(game, modifier.propName);
                    string propString = propToString(prop);
                    game?.logger?.log("PropModifier", card + "获得属性修正" + modifier + "=>" + propString);
                    return Task.CompletedTask;
                });
            else
            {
                if (modifier == null)
                    throw new ArgumentNullException(nameof(modifier));
                modifier.beforeAdd(game, this);
                modifierList.Add(modifier);
                modifier.afterAdd(game, this);
                object prop = getProp(game, modifier.propName);
                string propString = propToString(prop);
                return Task.FromResult<IAddModiEventArg>(default);
            }
        }
        string propToString(object prop)
        {
            if (prop is string str)
                return str;
            else if (prop is Array a)
            {
                string s = "[";
                for (int i = 0; i < a.Length; i++)
                {
                    s += propToString(a.GetValue(i));
                    if (i != a.Length - 1)
                        s += ",";
                }
                s += "]";
                return s;
            }
            else if (prop is IEnumerable e)
            {
                string s = "{";
                bool isFirst = true;
                foreach (var obj in e)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        s += ",";
                    s += propToString(obj);
                }
                s += "}";
                return s;
            }
            else if (prop == null)
                return "null";
            else
                return prop.ToString();
        }
        public class AddModiEventArg : EventArg, IAddModiEventArg
        {
            public Card card;
            public PropModifier modifier;

            ICard IAddModiEventArg.card => card;

            IPropModifier IAddModiEventArg.modifier => modifier;
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
                        await modifier.beforeRemove(game, card);
                        card.modifierList.Remove(modifier);
                        await modifier.afterRemove(game, card);
                        object prop = card.getProp(game, modifier.propName);
                        string propString = propToString(prop);
                        game?.logger?.log("PropModifier", card + "移除属性修正" + modifier + "=>" + propString);
                    });
                else
                {
                    await modifier.beforeRemove(game, this);
                    modifierList.Remove(modifier);
                    await modifier.afterRemove(game, this);
                    object prop = getProp(game, modifier.propName);
                    string propString = propToString(prop);
                    game?.logger?.log("PropModifier", this + "移除属性修正" + modifier + "=>" + propString);
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
            game?.logger?.log("Buff", this + "获得增益" + buff);
            buffList.Add(buff);
            if (buff.modifiers != null)
            {
                foreach (var modifier in buff.modifiers)
                {
                    await addModifier(game, modifier);
                }
            }
            if (buff.effects != null)
            {
                foreach (var efffect in buff.effects)
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
                game?.logger?.log("Buff", this + "移除增益" + buff);
                buffList.Remove(buff);
                foreach (var modifier in buff.modifiers)
                {
                    await removeModifier(game, modifier);
                }
                foreach (var effect in buff.effects)
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
        public Task<ISetPropEventArg> setProp<T>(IGame game, string propName, T value)
        {
            if (game != null && game.triggers != null)
                return game.triggers.doEvent<ISetPropEventArg>(new SetPropEventArg() { card = this, propName = propName, value = value }, arg =>
                {
                    Card card = arg.card as Card;
                    propName = arg.propName;
                    var v = arg.value;
                    propDic[propName] = v;
                    return Task.CompletedTask;
                });
            else
            {
                propDic[propName] = value;
                return Task.FromResult<ISetPropEventArg>(default);
            }
        }
        public class SetPropEventArg : EventArg, ISetPropEventArg
        {
            public Card card;
            public string propName;
            public object value;
            ICard ISetPropEventArg.card => card;
            string ISetPropEventArg.propName => propName;
            object ISetPropEventArg.value => value;
        }
        public T getProp<T>(IGame game, string propName)
        {
            T value = default;
            if (propDic.ContainsKey(propName) && propDic[propName] is T t)
                value = t;
            foreach (var modifier in modifierList.OfType<PropModifier<T>>().Where(mt =>
                mt.propName == propName &&
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
            foreach (var modifier in modifierList.Where(m =>
                m.propName == propName &&
                (game == null || m.checkCondition(game, this))))
            {
                value = modifier.calc(game, this, value);
            }
            return value;
        }
        public override string ToString()
        {
            if (define != null)
                return "Card(" + id + ")<" + define.GetType().Name + ">";
            else
                return "Card(" + id + ")";
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