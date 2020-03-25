using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    /// <summary>
    /// Pile（牌堆）表示一个可以容纳卡片的有序集合，比如卡组，手牌，战场等等。一个Pile中可以包含可枚举数量的卡牌。
    /// 注意，卡片在Pile中的顺序代表了它的位置。0是最左边（手牌），0也是最底部（卡组）。
    /// </summary>
    public class Pile : IEnumerable<Card>
    {
        public int maxCount { get; set; }
        public Pile(string name = null, Card[] cards = null, int maxCount = -1)
        {
            this.name = name;
            this.maxCount = maxCount;
            if (cards == null)
                return;
            foreach (Card card in cards)
            {
                card.pile = this;
            }
            cardList.AddRange(cards);
        }
        public Player owner { get; set; } = null;
        public string name { get; } = null;
        public void insert(Card card, int position)
        {
            cardList.Insert(position, card);
        }
        /// <summary>
        /// 将位于该牌堆中的一张牌移动到其他的牌堆中。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="targetPile"></param>
        /// <param name="position"></param>
        public void moveTo(Card card, Pile targetPile, int position)
        {
            if (cardList.Remove(card))
            {
                card.pile = targetPile;
                targetPile.cardList.Insert(position, card);
            }
        }
        public void moveTo(Card card, Pile targetPile)
        {
            moveTo(card, targetPile, targetPile.count);
        }
        public void moveTo(Card[] cards, Pile targetPile, int position)
        {
            List<Card> removedCardList = new List<Card>(cards.Length);
            foreach (Card card in cards)
            {
                if (cardList.Remove(card))
                {
                    card.pile = targetPile;
                    removedCardList.Add(card);
                }
            }
            targetPile.cardList.InsertRange(position, removedCardList);
        }
        /// <summary>
        /// 将该牌堆中的一些卡换成其他牌堆中的另一些卡。
        /// </summary>
        /// <param name="originalCards"></param>
        /// <param name="replacedCards"></param>
        public void replace(Card[] originalCards, Card[] replacedCards)
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
                    this[originIndex] = replacedCards[i];
                    replacedCards[i].pile[replaceIndex] = originalCards[i];
                    originalCards[i].pile = replacedCards[i].pile;
                    replacedCards[i].pile = this;
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
                indexArray[i] = indexOf(originalCards[i]);
                //把牌放回去
                pile.cardList.Insert(engine.randomInt(0, pile.cardList.Count), originalCards[i]);
                originalCards[i].pile = pile;
            }
            for (int i = 0; i < indexArray.Length; i++)
            {
                //将牌堆中的随机卡片填入空位
                int targetIndex = engine.randomInt(0, pile.count - 1);
                cardList[indexArray[i]] = pile.cardList[targetIndex];
                cardList[indexArray[i]].pile = this;
                //并将其从牌堆中移除
                pile.cardList.RemoveAt(targetIndex);
            }
            return indexArray.Select(i => cardList[i]).ToArray();
        }
        internal void remove(Card card)
        {
            if (cardList.Remove(card))
            {
                card.pile = null;
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
            return name + "[" + cardList.Count + "]";
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