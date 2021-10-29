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
        public static Task moveTo(IGame game, Card card, Pile from, Pile to, int position)
        {
            if (game != null)
                return game.triggers.doEvent(new MoveCardEventArg(from, to, card, position), arg =>
                {
                    from = arg.from;
                    to = arg.to;
                    card = arg.card;
                    position = arg.position;
                    if (from != null)
                    {
                        if (from.cardList.Remove(card))
                        {
                            try
                            {
                                foreach (var effect in card.define.getEffects())
                                {
                                    if (effect is IPassiveEffect passiveEffect)
                                    {
                                        if (passiveEffect.piles.Contains(from.name) && (to == null || !passiveEffect.piles.Contains(to.name)))
                                            passiveEffect.onDisable(game, card, null);
                                    }
                                    else if (effect is GeneratedEffect generatedEffect)
                                    {
                                        if (generatedEffect.pileList.Contains(from.name) && (to == null || !generatedEffect.pileList.Contains(to.name)))
                                            generatedEffect.onDisable(game, card, null);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                game.logger.logError("将" + card + "从" + from + "移动到" + to + "时禁用被动效果引发异常：" + e);
                            }
                            if (to != null)
                            {
                                if (position < 0)
                                    position = 0;
                                if (position < to.cardList.Count)
                                    to.cardList.Insert(position, card);
                                else
                                    to.cardList.Add(card);
                                card.pile = to;
                                card.owner = to.owner;
                                try
                                {
                                    foreach (var effect in card.define.getEffects())
                                    {
                                        if (effect is IPassiveEffect passiveEffect)
                                        {
                                            if ((from == null || !passiveEffect.piles.Contains(from.name)) && passiveEffect.piles.Contains(to.name))
                                                passiveEffect.onEnable(game, card, null);
                                        }
                                        else if (effect is GeneratedEffect generatedEffect)
                                        {
                                            if ((from == null || !generatedEffect.pileList.Contains(from.name)) && generatedEffect.pileList.Contains(to.name))
                                                generatedEffect.onEnable(game, card, null);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    game.logger.logError("将" + card + "从" + from + "移动到" + to + "时激活被动效果引发异常：" + e);
                                }
                            }
                            else
                            {
                                card.pile = null;
                                card.owner = null;
                            }
                        }
                    }
                    else
                    {
                        if (to != null)
                        {
                            if (position < 0)
                                position = 0;
                            if (position < to.cardList.Count)
                                to.cardList.Insert(position, card);
                            else
                                to.cardList.Add(card);
                            card.pile = to;
                            card.owner = to.owner;
                            try
                            {
                                foreach (var effect in card.define.getEffects())
                                {
                                    if (effect is IPassiveEffect passiveEffect)
                                    {
                                        if (passiveEffect.piles.Contains(to.name))
                                            passiveEffect.onEnable(game, card, null);
                                    }
                                    else if (effect is GeneratedEffect generatedEffect)
                                    {
                                        if (generatedEffect.pileList.Contains(to.name))
                                            generatedEffect.onEnable(game, card, null);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                game.logger.logError("将" + card + "从" + from + "中移除时激活被动效果引发异常：" + e);
                            }
                        }
                        else
                        {
                            card.pile = null;
                            card.owner = null;
                        }
                    }
                    return Task.CompletedTask;
                });
            else
            {
                if (from != null)
                {
                    if (from.cardList.Remove(card))
                    {
                        if (to != null)
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
                        else
                        {
                            card.pile = null;
                            card.owner = null;
                        }
                    }
                }
                else
                {
                    if (to != null)
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
                    else
                    {
                        card.pile = null;
                        card.owner = null;
                    }
                }
                return Task.CompletedTask;
            }
        }
        public class MoveCardEventArg : EventArg
        {
            public MoveCardEventArg(Pile from, Pile to, Card card, int position)
            {
                this.from = from;
                this.to = to;
                this.card = card;
                this.position = position;
            }
            public override string ToString()
            {
                return "将" + card + "从" + from + "移动到" + to + "的" + position;
            }
            #region 动作定义
            [ActionNodeMethod("IsPlayerPileChanged", "Pile")]
            [return: ActionNodeParam("IsChanged")]
            public static bool isPlayerPileChanged(MoveCardEventArg moveCardEvent, [ActionNodeParam("Player")] Player player, [ActionNodeParam("PileName")] string pileName)
            {
                return (moveCardEvent.from.owner == player && moveCardEvent.from.name == pileName) ||
                    (moveCardEvent.to.owner == player && moveCardEvent.to.name == pileName);
            }
            #endregion
            public Pile from;
            public Pile to;
            public Card card;
            public int position;
        }
        public Task moveTo(IGame game, Card card, Pile targetPile)
        {
            return moveTo(game, card, this, targetPile, targetPile.count);
        }
        public async Task moveTo(IGame game, IEnumerable<Card> cards, Pile targetPile)
        {
            foreach (var card in cards.Reverse())
            {
                await moveTo(game, card, targetPile);
            }
        }
        public async Task moveTo(IGame game, IEnumerable<Card> cards, Pile targetPile, int position)
        {
            foreach (var card in cards.Reverse())
            {
                await moveTo(game, card, this, targetPile, position);
            }
        }
        public async Task replace(IGame game, Card origin, Card target)
        {
            int originIndex = indexOf(origin);
            int targetIndex = target.pile.indexOf(target);
            await moveTo(game, origin, this, target.pile, targetIndex);
            await moveTo(game, target, target.pile, this, originIndex);
        }
        /// <summary>
        /// 将牌堆中的一些牌与目标牌堆中随机的一些牌相替换。
        /// </summary>
        /// <param name="engine">用于提供随机功能的引擎</param>
        /// <param name="originalCards">要进行替换的卡牌</param>
        /// <param name="pile">目标牌堆</param>
        /// <returns>返回替换原有卡牌的卡牌数组，顺序与替换的顺序相同</returns>
        public async Task<Card[]> replaceByRandom(CardEngine engine, Card[] originalCards, Pile pile)
        {
            Card[] replaced = new Card[originalCards.Length];
            for (int i = 0; i < originalCards.Length; i++)
            {
                replaced[i] = pile.getCardByRandom(engine);
                await replace(engine, originalCards[i], replaced[i]);
            }
            return replaced;
            //int[] indexArray = new int[originalCards.Length];
            //for (int i = 0; i < originalCards.Length; i++)
            //{
            //    //记录当前牌堆中的空位
            //    Card card = originalCards[i];
            //    indexArray[i] = indexOf(card);
            //    if (indexArray[i] < 0)
            //        throw new IndexOutOfRangeException(this + "中不存在" + card + "，" + this + "：" + string.Join("，", cardList));
            //    //把牌放回去
            //    pile.cardList.Insert(engine.randomInt(0, pile.cardList.Count), card);
            //    foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
            //    {
            //        if (effect.piles.Contains(pile.name))
            //            effect.onEnable(engine, card);
            //    }
            //}
            //for (int i = 0; i < indexArray.Length; i++)
            //{
            //    //将牌堆中的随机卡片填入空位
            //    int targetIndex = engine.randomInt(0, pile.count - 1);
            //    cardList[indexArray[i]] = pile.cardList[targetIndex];
            //    Card card = cardList[indexArray[i]];
            //    foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
            //    {
            //        if (effect.piles.Contains(name))
            //            effect.onEnable(engine, card);
            //    }
            //    //并将其从牌堆中移除
            //    pile.cardList.RemoveAt(targetIndex);
            //    foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
            //    {
            //        if (effect.piles.Contains(pile.name))
            //            effect.onDisable(engine, card);
            //    }
            //}
            //return indexArray.Select(i => cardList[i]).ToArray();
        }
        public Task remove(IGame game, Card card)
        {
            return moveTo(game, card, this, null, 0);
            //if (cardList.Remove(card))
            //{
            //    foreach (IPassiveEffect effect in card.define.effects.OfType<IPassiveEffect>())
            //    {
            //        if (effect.piles.Contains(name))
            //            effect.onDisable(game, card);
            //    }
            //}
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
        internal List<Card> cardList { get; } = new List<Card>();
        public override string ToString()
        {
            if (owner != null)
                return owner.name + "[" + name + "]";
            else
                return "[" + name + "]";
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
        #region 动作定义
        //[ActionNodeMethod("GetCount")]
        //[return: ActionNodeParam("Count")]
        //public static int getCount([ActionNodeParam("Pile")] Pile pile)
        //{
        //    return pile.count;
        //}
        #endregion
    }
    public enum RegionType
    {
        none,
        deck,
        hand
    }
}