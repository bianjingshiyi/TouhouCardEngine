using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    /// <summary>
    /// Pile（牌堆）表示一个可以容纳卡片的有序集合，比如卡组，手牌，战场等等。一个Pile中可以包含可枚举数量的卡牌。
    /// 注意，卡片在Pile中的顺序代表了它的位置。0是最左边（手牌），0也是最底部（卡组）。
    /// </summary>
    public class Pile : IEnumerable<Card>
    {
        #region 公有方法
        public Pile(string name = null, int maxCount = -1)
        {
            this.name = name;
            this.maxCount = maxCount;
        }
        public void addCard(Card card)
        {
            insertCard(card, count);
        }
        public void insertCard(Card card, int position)
        {
            if (position < 0)
                position = 0;
            if (position < cardList.Count)
                cardList.Insert(position, card);
            else
                cardList.Add(card);
        }
        public bool removeCard(Card card)
        {
            return cardList.Remove(card);
        }
        public async Task<bool> moveCard(CardEngine game, Card card, Pile to, int toPosition)
        {
            return await moveCard(game, card, this, to, toPosition);
        }
        public static async Task<bool> moveCard(CardEngine game, Card card, Pile from, Pile to, int toPosition)
        {
            if (!canMove(card, from, to))
                return false;

            int fromPosition = -1;
            if (from != null)
            {
                fromPosition = from.indexOf(card);
                from.removeCard(card);
            }
            if (to != null)
                to.insertCard(card, toPosition);
            card.pile = to;
            card.owner = to?.owner;
            await updateCardEnableStates(game, card, from, to);
            game.triggers.addChange(new CardMoveChange(card, from, to, fromPosition, toPosition));
            return true;
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
        public static bool isMoveSourceValid(Card card, Pile from)
        {
            if (card == null)
                return false;
            // 源牌堆
            if (from == null)
            {
                // 源牌堆为空，要求卡牌的位置也是空。
                if (card.pile != null)
                    return false;
            }
            else
            {
                // 源牌堆不为空，要求卡牌就在这个牌堆内。
                if (!from.Contains(card))
                    return false;
            }
            return true;
        }
        public static bool isMoveDestinationValid(Card card, Pile from, Pile to)
        {
            if (card == null)
                return false;
            // 目标牌堆
            if (to != null)
            {
                // 目标牌堆满了，并且不是在同一个牌堆内移动
                if (to.isFull && from != to)
                    return false;
            }
            return true;
        }
        public static bool canMove(Card card, Pile from, Pile to)
        {
            return isMoveSourceValid(card, from) && isMoveDestinationValid(card, from, to);
        }
        public static async Task updateCardEnableStates(CardEngine game, Card card, Pile from, Pile to)
        {
            await disableCardEffects(game, card, from, to);
            await enableCardEffects(game, card, from, to);
        }
        public static (Buff buff, Effect effect)[] getMoveShouldEnableCardEffects(Card card, Pile from, Pile to)
        {
            List<(Buff buff, Effect effect)> effects = new List<(Buff buff, Effect effect)>();
            if (to == null)
                return effects.ToArray();

            foreach (var effect in card.define.getEffects())
            {
                checkEffect(effect, null);
            }
            foreach (var buff in card.getBuffs())
            {
                foreach (var effect in buff.getEffects())
                {
                    checkEffect(effect, buff);
                }
            }
            return effects.ToArray();

            void checkEffect(Effect effect, Buff buff)
            {
                if (effect is not IPileRangedEffect pileEffect)
                    return;

                var validPiles = pileEffect.getPiles();
                bool fromIsNotValid = from == null || !validPiles.ContainsPileOrAny(from.name);
                bool toIsValid = validPiles.ContainsPileOrAny(to.name);
                if (fromIsNotValid && toIsValid)
                {
                    effects.Add((buff, effect));
                }
            }
        }
        public static (Buff buff, Effect effect)[] getMoveShouldDisableCardEffects(Card card, Pile from, Pile to)
        {
            List<(Buff buff, Effect effect)> effects = new List<(Buff buff, Effect effect)>();
            if (from == null)
                return effects.ToArray();

            foreach (var effect in card.define.getEffects())
            {
                checkEffect(effect, null);
            }
            foreach (var buff in card.getBuffs())
            {
                foreach (var effect in buff.getEffects())
                {
                    checkEffect(effect, buff);
                }
            }
            return effects.ToArray();

            void checkEffect(Effect effect, Buff buff)
            {
                if (effect is not IPileRangedEffect pileEffect)
                    return;

                var validPiles = pileEffect.getPiles();
                bool fromIsValid = validPiles.ContainsPileOrAny(from.name);
                bool toIsNotValid = to == null || !validPiles.ContainsPileOrAny(to.name);
                if (fromIsValid && toIsNotValid)
                {
                    effects.Add((buff, effect));
                }
            }
        }

        public Card getCard<T>() where T : CardDefine
        {
            return cardList.FirstOrDefault(c => c.define is T);
        }
        public Card[] getCards<T>() where T : CardDefine
        {
            return cardList.Where(c => c.define is T).ToArray();
        }
        /// <summary>
        /// 获取卡片实例
        /// </summary>
        /// <param name="id">实例ID</param>
        /// <returns></returns>
        public Card getCard(int id)
        {
            return cardList.FirstOrDefault(c => c.id == id);
        }
        /// <summary>
        /// 获取指定类型的卡片
        /// </summary>
        /// <param name="defineId">类型ID</param>
        /// <returns></returns>
        public Card getCardByDefine(int defineId)
        {
            return cardList.FirstOrDefault(c => c.define != null && c.define.id == defineId);
        }
        public Card getCardByRandom(IGame game)
        {
            if (cardList.Count < 1)
                return null;
            else if (cardList.Count == 1)
                return cardList[0];
            else
            {
                return cardList[game.randomInt(0, cardList.Count - 1)];
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
        public override string ToString()
        {
            if (owner != null)
                return $"{owner.name}[{name}]";
            else
                return $"[{name}]";
        }
        public Card[] ToArray()
        {
            return cardList.ToArray();
        }
        #endregion
        #region 私有方法
        /// <summary>
        /// 简单地将卡牌移动到牌堆，而不进行任何其他逻辑上的更改。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="to"></param>
        /// <param name="toPosition"></param>
        /// <returns></returns>
        internal bool moveCardRaw(Card card, Pile to, int toPosition)
        {
            return moveCardRaw(card, this, to, toPosition);
        }
        /// <summary>
        /// 简单地将卡牌移动到牌堆，而不进行任何其他逻辑上的更改。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="toPosition"></param>
        /// <returns></returns>
        internal static bool moveCardRaw(Card card, Pile from, Pile to, int toPosition)
        {
            bool moveSuccess = true;
            if (from != null)
            {
                if (!from.removeCard(card))
                    moveSuccess = false;
            }

            if (moveSuccess)
            {
                to?.insertCard(card, toPosition);
                card.pile = to;
                card.owner = to?.owner;
            }
            return moveSuccess;
        }
        private static async Task enableCardEffects(CardEngine game, Card card, Pile from, Pile to)
        {
            if (to == null)
                return;

            try
            {
                var effects = getMoveShouldEnableCardEffects(card, from, to);
                foreach (var (buff, effect) in effects)
                {
                    await effect.enable(game, card, buff);
                }
            }
            catch (Exception e)
            {
                game.logger.logError($"将{card}从{from}移动到{to}时激活效果引发异常：{e}");
            }
        }
        private static async Task disableCardEffects(CardEngine game, Card card, Pile from, Pile to)
        {
            if (from == null)
                return;

            try
            {
                var effects = getMoveShouldDisableCardEffects(card, from, to);
                foreach (var (buff, effect) in effects)
                {
                    await effect.disable(game, card, buff);
                }
            }
            catch (Exception e)
            {
                game.logger.logError($"将{card}从{from}移动到{to}时禁用效果引发异常：{e}");
            }
        }
        #endregion
        #region 运算符
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
        #endregion
        #region 属性字段
        public const string PILE_RANGE_ANY = "BUILTIN_ANY";
        public string name { get; } = null;
        public Player owner { get; internal set; } = null;
        public int maxCount { get; set; }
        /// <summary>
        /// 牌堆顶上的那一张，也就是列表中的最后一张。
        /// </summary>
        public Card top
        {
            get { return cardList.Count < 1 ? null : cardList[cardList.Count - 1]; }
        }
        /// <summary>
        /// 牌堆最右边的那一张，也就是列表中的最后一张。
        /// </summary>
        public Card right
        {
            get { return cardList.Count < 1 ? null : cardList[cardList.Count - 1]; }
        }
        public int indexOf(Card card)
        {
            return cardList.IndexOf(card);
        }
        public int count
        {
            get { return cardList.Count; }
        }
        public bool isFull
        {
            get { return maxCount < 0 ? false : count >= maxCount; }
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
        private List<Card> cardList = new List<Card>();
        #endregion
    }
}