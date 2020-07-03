using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    /// <summary>
    /// Pile（牌堆）表示一个可以容纳卡片的有序集合，比如卡组，手牌，战场等等。一个Pile中可以包含可枚举数量的卡牌。
    /// 注意，卡片在Pile中的顺序代表了它的位置。0是最左边（手牌），0也是最底部（卡组）。
    /// </summary>
    public class Pile : IEnumerable<Card>
    {
        public string name { get; } = null;
        public Player owner { get; internal set; } = null;
        public int maxCount { get; set; }
        public Pile(IGame game, string name = null, int maxCount = -1)
        {
            this.name = name;
            this.maxCount = maxCount;
        }
        public Task add(IGame game, Card card)
        {
            return insert(game, card, cardList.Count);
        }
        public async Task add(IGame game, IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                await add(game, card);
            }
        }
        public Task insert(IGame game, Card card, int position)
        {
            return moveTo(game, card, null, this, position);
        }
        /// <summary>
        /// 将位于该牌堆中的一张牌移动到其他的牌堆中。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="to"></param>
        /// <param name="position"></param>
        public Task moveTo(IGame game, Card card, Pile to, int position)
        {
            return moveTo(game, card, this, to, position);
        }
        public Task moveTo(IGame game, Card card, Pile from, Pile to, int position)
        {
            if (game != null)
                return game.triggers.doEvent(new MoveCardEventArg() { from = this, to = to, card = card, position = position }, arg =>
                {
                    from = arg.from;
                    to = arg.to;
                    card = arg.card;
                    position = arg.position;
                    if (from != null)
                    {
                        if (from.cardList.Remove(card))
                        {
                            foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                            {
                                if (effect.piles.Contains(from.name))
                                    effect.onDisable(game, card);
                            }
                            if (position < 0)
                                position = 0;
                            if (position < to.cardList.Count)
                                to.cardList.Insert(position, card);
                            else
                                to.cardList.Add(card);
                            foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                            {
                                if (effect.piles.Contains(to.name))
                                    effect.onEnable(game, card);
                            }
                            card.pile = to;
                            card.owner = to.owner;
                        }
                    }
                    else
                    {
                        if (position < 0)
                            position = 0;
                        if (position < to.cardList.Count)
                            to.cardList.Insert(position, card);
                        else
                            to.cardList.Add(card);
                        foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                        {
                            if (effect.piles.Contains(to.name))
                                effect.onEnable(game, card);
                        }
                        card.pile = to;
                        card.owner = to.owner;
                    }
                    return Task.CompletedTask;
                });
            else
            {
                if (from != null)
                {
                    if (from.cardList.Remove(card))
                    {
                        if (position < 0)
                            position = 0;
                        if (position < to.cardList.Count)
                            to.cardList.Insert(position, card);
                        else
                            to.cardList.Add(card);
                        card.pile = to;
                        card.owner = to.owner;
                    }
                }
                else
                {
                    if (position < 0)
                        position = 0;
                    if (position < to.cardList.Count)
                        to.cardList.Insert(position, card);
                    else
                        to.cardList.Add(card);
                    card.pile = to;
                    card.owner = to.owner;
                }
                return Task.CompletedTask;
            }
        }
        public class MoveCardEventArg : EventArg
        {
            public Pile from;
            public Pile to;
            public Card card;
            public int position;
        }
        public void moveTo(IGame game, Card card, Pile targetPile)
        {
            moveTo(game, card, targetPile, targetPile.count);
        }
        public void moveTo(IGame game, IEnumerable<Card> cards, Pile targetPile, int position)
        {
            foreach (var card in cards.Reverse())
            {
                moveTo(game, card, this, targetPile, position);
            }
        }
        /// <summary>
        /// 将该牌堆中的一些卡换成其他牌堆中的另一些卡。
        /// </summary>
        /// <param name="originalCards"></param>
        /// <param name="replacedCards"></param>
        public void replace(IGame game, Card[] originalCards, Card[] replacedCards)
        {
            if (originalCards.Length != replacedCards.Length)
                throw new IndexOutOfRangeException("originalCards与replacedCards数量不一致");
            for (int i = 0; i < originalCards.Length; i++)
            {
                int originIndex = indexOf(originalCards[i]);
                if (originIndex < 0)
                    throw new InvalidOperationException(originalCards[i] + "不在" + this + "中");
                else
                {
                    int replaceIndex = replacedCards[i].pile.indexOf(replacedCards[i]);
                    foreach (IPassiveEffect effect in this[originIndex].define.effects.OfType<IPassiveEffect>())
                    {
                        if (effect.piles.Contains(name))
                            effect.onDisable(game, this[originIndex]);
                    }
                    this[originIndex] = replacedCards[i];
                    foreach (IPassiveEffect effect in this[originIndex].define.effects.OfType<IPassiveEffect>())
                    {
                        if (effect.piles.Contains(name))
                            effect.onEnable(game, this[originIndex]);
                    }
                    foreach (IPassiveEffect effect in replacedCards[i].pile[replaceIndex].define.effects.OfType<IPassiveEffect>())
                    {
                        if (effect.piles.Contains(replacedCards[i].pile.name))
                            effect.onDisable(game, replacedCards[i].pile[replaceIndex]);
                    }
                    replacedCards[i].pile[replaceIndex] = originalCards[i];

                    originalCards[i].pile = replacedCards[i].pile;
                    originalCards[i].owner = replacedCards[i].pile.owner;
                    replacedCards[i].pile = this;
                    replacedCards[i].owner = owner;
                }
            }
        }
        /// <summary>
        /// 将牌堆中的一些牌与目标牌堆中随机的一些牌相替换。
        /// </summary>
        /// <param name="engine">用于提供随机功能的引擎</param>
        /// <param name="originalCards">要进行替换的卡牌</param>
        /// <param name="pile">目标牌堆</param>
        /// <returns>返回替换原有卡牌的卡牌数组，顺序与替换的顺序相同</returns>
        public Card[] replaceByRandom(CardEngine engine, Card[] originalCards, Pile pile)
        {
            int[] indexArray = new int[originalCards.Length];
            for (int i = 0; i < originalCards.Length; i++)
            {
                //记录当前牌堆中的空位
                Card card = originalCards[i];
                indexArray[i] = indexOf(card);
                if (indexArray[i] < 0)
                    throw new IndexOutOfRangeException(this + "中不存在" + card + "，" + this + "：" + string.Join("，", cardList));
                //把牌放回去
                pile.cardList.Insert(engine.randomInt(0, pile.cardList.Count), card);
                foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                {
                    if (effect.piles.Contains(pile.name))
                        effect.onEnable(engine, card);
                }
            }
            for (int i = 0; i < indexArray.Length; i++)
            {
                //将牌堆中的随机卡片填入空位
                int targetIndex = engine.randomInt(0, pile.count - 1);
                cardList[indexArray[i]] = pile.cardList[targetIndex];
                Card card = cardList[indexArray[i]];
                foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                {
                    if (effect.piles.Contains(name))
                        effect.onEnable(engine, card);
                }
                //并将其从牌堆中移除
                pile.cardList.RemoveAt(targetIndex);
                foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                {
                    if (effect.piles.Contains(pile.name))
                        effect.onDisable(engine, card);
                }
            }
            return indexArray.Select(i => cardList[i]).ToArray();
        }
        public void remove(IGame game, Card card)
        {
            if (cardList.Remove(card))
            {
                foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
                {
                    if (effect.piles.Contains(name))
                        effect.onDisable(game, card);
                }
            }
        }
        public void shuffle(CardEngine engine)
        {
            for (int i = 0; i < cardList.Count; i++)
            {
                int index = engine.randomInt(i, cardList.Count - 1);
                Card card = cardList[i];
                cardList[i] = cardList[index];
                cardList[index] = card;
            }
        }
        /// <summary>
        /// 牌堆顶上的那一张，也就是列表中的最后一张。
        /// </summary>
        public Card top
        {
            get
            {
                if (cardList.Count < 1)
                    return null;
                return cardList[cardList.Count - 1];
            }
        }
        public int indexOf(Card card)
        {
            return cardList.IndexOf(card);
        }
        public int count
        {
            get { return cardList.Count; }
        }
        public Card this[int index]
        {
            get { return cardList[index]; }
            internal set
            {
                cardList[index] = value;
            }
        }
        public Card[] this[int startIndex, int endIndex]
        {
            get
            {
                return cardList.GetRange(startIndex, endIndex - startIndex + 1).ToArray();
            }
            internal set
            {
                for (int i = 0; i < value.Length; i++)
                {
                    cardList[startIndex + i] = value[i];
                }
            }
        }
        public Card getCard<T>() where T : CardDefine
        {
            return cardList.FirstOrDefault(c => c.define is T);
        }
        public IEnumerator<Card> GetEnumerator()
        {
            return ((IEnumerable<Card>)cardList).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Card>)cardList).GetEnumerator();
        }
        internal List<Card> cardList { get; } = new List<Card>();
        public override string ToString()
        {
            return owner.name + "[" + name + "]";
        }
        public static implicit operator Pile[](Pile pile)
        {
            if (pile != null)
                return new Pile[] { pile };
            else
                return new Pile[0];
        }
        public static implicit operator Card[](Pile pile)
        {
            if (pile != null)
                return pile.cardList.ToArray();
            else
                return new Card[0];
        }
    }
    public enum RegionType
    {
        none,
        deck,
        hand
    }
}