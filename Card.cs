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
            return game.triggers.doEvent(new AddModiEventArg() { game = game, card = this, modifier = modifier }, onAddModi);
        }
        static Task onAddModi(AddModiEventArg arg)
        {
            IGame game = arg.game;
            Card card = arg.card;
            PropModifier modifier = arg.modifier;
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));
            game?.logger?.log("PropModifier", card + "获得属性修正" + modifier);
            modifier.beforeAdd(card);
            card.modifierList.Add(modifier);
            modifier.afterAdd(card);
            return Task.CompletedTask;
        }
        public class AddModiEventArg : EventArg
        {
            public Card card;
            public PropModifier modifier;
        }
        public Task<bool> removeModifier(IGame game, PropModifier modifier)
        {
            if (modifierList.Contains(modifier))
            {
                game?.logger?.log("PropModifier", this + "移除属性修正" + modifier);
                modifier.beforeRemove(this);
                modifierList.Remove(modifier);
                modifier.afterRemove(this);
                return Task.FromResult(true);
            }
            else
                return Task.FromResult(false);
        }
        public void addBuff(IGame game, Buff buff)
        {
            if (buff == null)
                throw new ArgumentNullException(nameof(buff));
            game?.logger?.log("Buff", this + "获得增益" + buff);
            foreach (PropModifier modifier in buff.modifiers)
            {
                modifier.beforeAdd(this);
            }
            buffList.Add(buff);
            foreach (PropModifier modifier in buff.modifiers)
            {
                modifier.afterAdd(this);
            }
            foreach (var efffect in buff.effects)
            {
                efffect.onEnable(game, this);
            }
        }
        public bool removeBuff(IGame game, Buff buff)
        {
            if (buffList.Contains(buff))
            {
                game?.logger?.log("Buff", this + "移除增益" + buff);
                foreach (PropModifier modifier in buff.modifiers)
                {
                    modifier.beforeRemove(this);
                }
                buffList.Remove(buff);
                foreach (PropModifier modifier in buff.modifiers)
                {
                    modifier.afterRemove(this);
                }
                foreach (var effect in buff.effects)
                {
                    effect.onDisable(game, this);
                }
                return true;
            }
            else
                return false;
        }
        public Buff[] getBuffs()
        {
            return buffList.ToArray();
        }
        public void setProp<T>(string propName, T value)
        {
            propDic[propName] = value;
        }
        public T getProp<T>(string propName)
        {
            T value = default;
            if (propDic.ContainsKey(propName) && propDic[propName] is T t)
                value = t;
            foreach (PropModifier<T> modifier in modifierList.Where(m => m is PropModifier<T> mt && mt.propName == propName).Cast<PropModifier<T>>())
            {
                value = modifier.calc(this, value);
            }
            foreach (Buff buff in buffList)
            {
                foreach (PropModifier<T> modifier in buff.modifiers.Where(m => m is PropModifier<T> mt && mt.propName == propName).Cast<PropModifier<T>>())
                {
                    value = modifier.calc(this, value);
                }
            }
            return (T)(object)value;
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