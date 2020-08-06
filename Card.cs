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
        public Task addModifier(IGame game, PropModifier modifier)
        {
            return game.triggers.doEvent(new AddModiEventArg() { game = game, card = this, modifier = modifier }, arg =>
            {
                Card card = arg.card;
                modifier = arg.modifier;
                if (modifier == null)
                    throw new ArgumentNullException(nameof(modifier));
                modifier.beforeAdd(game, card);
                card.modifierList.Add(modifier);
                modifier.afterAdd(game, card);
                object prop = card.getProp(game, modifier.propName);
                string propString = prop != null ? (prop is IEnumerable c ? string.Join("，", c) : (prop is Array array ? string.Join("，", array) : prop.ToString())) : "null";
                game?.logger?.log("PropModifier", card + "获得属性修正" + modifier + "=>" + propString);
                return Task.CompletedTask;
            });
        }
        public class AddModiEventArg : EventArg
        {
            public Card card;
            public PropModifier modifier;
        }
        public async Task<bool> removeModifier(IGame game, PropModifier modifier)
        {
            if (modifierList.Contains(modifier))
            {
                await game.triggers.doEvent(new RemoveModiEventArg() { card = this, modifier = modifier }, arg =>
                {
                    Card card = arg.card;
                    modifier.beforeRemove(game, card);
                    card.modifierList.Remove(modifier);
                    modifier.afterRemove(game, card);
                    object prop = card.getProp(game, modifier.propName);
                    string propString = prop != null ? (prop is IEnumerable c ? string.Join("，", c) : prop.ToString()) : "null";
                    game?.logger?.log("PropModifier", card + "移除属性修正" + modifier + "=>" + propString);
                    return Task.CompletedTask;
                });
                return true;
            }
            else
                return false;
        }
        public class RemoveModiEventArg : EventArg
        {
            public Card card;
            public PropModifier modifier;
        }
        public async Task addBuff(IGame game, Buff buff)
        {
            if (buff == null)
                throw new ArgumentNullException(nameof(buff));
            game?.logger?.log("Buff", this + "获得增益" + buff);
            buffList.Add(buff);
            foreach (var modifier in buff.modifiers)
            {
                await addModifier(game, modifier);
            }
            foreach (var efffect in buff.effects)
            {
                efffect.onEnable(game, this, buff);
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
                    effect.onDisable(game, this, buff);
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
        public void setProp<T>(string propName, T value)
        {
            propDic[propName] = value;
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