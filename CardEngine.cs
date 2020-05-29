using System;
using System.Linq;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public partial class CardEngine : IGame
    {
        public ITimeManager time { get; set; } = null;
        public ITriggerManager triggers { get; set; } = null;
        IAnswerManager _answers;
        public IAnswerManager answers
        {
            get { return _answers; }
            set
            {
                if (_answers != null)
                    _answers.onResponse -= onAnswer;
                _answers = value;
                if (_answers != null)
                    _answers.onResponse += onAnswer;
            }
        }
        public ILogger logger { get; set; }
        public IGameEnvironment env { get; }
        public Rule rule { get; }
        public CardEngine(IGameEnvironment env, Rule rule, int randomSeed, params CardDefine[] defines)
        {
            this.env = env;
            this.rule = rule;
            random = new Random(randomSeed);
            foreach (CardDefine define in defines)
            {
                addCardDefine(define);
            }
        }
        public virtual void onAnswer(IResponse response)
        {
        }
        #region CardDefine
        public void addCardDefine(CardDefine define)
        {
            if (cardDefineDic.ContainsKey(define.id))
                throw new ConflictDefineException(cardDefineDic[define.id], define);
            cardDefineDic.Add(define.id, define);
        }
        public T getCardDefine<T>() where T : CardDefine
        {
            foreach (var pair in cardDefineDic)
            {
                if (pair.Value is T t)
                    return t;
            }
            return null;
        }
        public CardDefine getCardDefine(int id)
        {
            if (cardDefineDic.ContainsKey(id))
                return cardDefineDic[id];
            else
                return null;
        }
        public T getCardDefine<T>(int id) where T : CardDefine
        {
            if (cardDefineDic.ContainsKey(id) && cardDefineDic[id] is T t)
                return t;
            else
                return null;
        }
        Dictionary<int, CardDefine> cardDefineDic { get; } = new Dictionary<int, CardDefine>();
        #endregion
        public Card createCardById(int id)
        {
            CardDefine define = getCardDefine(id);
            if (define == null)
                throw new NoCardDefineException(id);
            return createCard(define);
        }
        #region Card
        public virtual Card createCard(CardDefine define)
        {
            int id = cardDic.Count + 1;
            while (cardDic.ContainsKey(id))
                id++;
            Card card = new Card(id, define);
            cardDic.Add(id, card);
            return card;
        }
        public Card getCard(int id)
        {
            if (cardDic.TryGetValue(id, out var card))
                return card;
            else
                return null;
        }
        public Card[] getCards(int[] ids)
        {
            return ids.Select(id => getCard(id)).ToArray();
        }
        Dictionary<int, Card> cardDic { get; } = new Dictionary<int, Card>();
        #endregion
        public T runFunc<T>(string script, EffectGlobals globals)
        {
            return env.runFunc<T>(script, globals);
        }
        public void runAction(string script, EffectGlobals globals)
        {
            env.runAction(script, globals);
        }
        public T getProp<T>(string varName)
        {
            if (dicVar.ContainsKey(varName) && dicVar[varName] is T)
                return (T)dicVar[varName];
            return default;
        }
        public void setProp<T>(string propName, T value)
        {
            dicVar[propName] = value;
        }
        public void setProp(string propName, PropertyChangeType changeType, int value)
        {
            if (changeType == PropertyChangeType.set)
                dicVar[propName] = value;
            else if (changeType == PropertyChangeType.add)
                dicVar[propName] = getProp<int>(propName) + propName;
        }
        public void setProp(string propName, PropertyChangeType changeType, float value)
        {
            if (changeType == PropertyChangeType.set)
                dicVar[propName] = value;
            else if (changeType == PropertyChangeType.add)
                dicVar[propName] = getProp<float>(propName) + propName;
        }
        public void setProp(string propName, PropertyChangeType changeType, string value)
        {
            if (changeType == PropertyChangeType.set)
                dicVar[propName] = value;
            else if (changeType == PropertyChangeType.add)
                dicVar[propName] = getProp<string>(propName) + propName;
        }
        internal Dictionary<string, object> dicVar { get; } = new Dictionary<string, object>();
        public int registerCard(Card card)
        {
            dicCard.Add(dicCard.Count + 1, card);
            card.id = dicCard.Count;
            return card.id;
        }
        public int[] registerCards(Card[] cards)
        {
            return cards.Select(c => { return registerCard(c); }).ToArray();
        }
        Dictionary<int, Card> dicCard { get; } = new Dictionary<int, Card>();
        public Player getPlayerAt(int playerIndex)
        {
            return (0 <= playerIndex && playerIndex < playerList.Count) ? playerList[playerIndex] : null;
        }
        public int getPlayerIndex(Player player)
        {
            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i] == player)
                    return i;
            }
            return -1;
        }
        /// <summary>
        /// 获取所有玩家，玩家在数组中的顺序与玩家被添加的顺序相同。
        /// </summary>
        /// <remarks>为什么不用属性是因为每次都会生成一个数组。</remarks>
        public Player[] getPlayers()
        {
            return playerList.ToArray();
        }
        public int playerCount
        {
            get { return playerList.Count; }
        }
        public void addPlayer(Player player)
        {
            playerList.Add(player);
        }
        protected int getNewPlayerId()
        {
            int id = playerList.Count;
            if (playerList.Any(p => p.id == id))
                id++;
            return id;
        }
        private List<Player> playerList { get; } = new List<Player>();
        public void doEvent(Event e)
        {
            e.phase = EventPhase.before;
            beginEvent(e);
            e.phase = EventPhase.logic;
            e.execute(this);
            e.phase = EventPhase.after;
            endEvent();
        }
        public void beginEvent(Event e)
        {
            e.parent = currentEvent;
            currentEvent = e;
            rule.beforeEvent(this, e);
            beforeEvent?.Invoke(currentEvent);
        }
        public void endEvent()
        {
            //进行游戏规则内容中事件结束之后的处理，比如在事件之后发生的效果。
            rule.afterEvent(this, currentEvent);
            afterEvent?.Invoke(currentEvent);
            if (currentEvent.parent != null)
                currentEvent = currentEvent.parent;
            else
            {
                eventList.Add(currentEvent);
                currentEvent = null;
            }
        }
        public delegate void EventAction(Event @event);
        public event EventAction beforeEvent;
        public event EventAction afterEvent;
        Event currentEvent { get; set; } = null;
        List<Event> eventList { get; } = new List<Event>();
        /// <summary>
        /// 随机整数，注意该函数返回的值可能包括最大值与最小值。
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>介于最大值与最小值之间，可能为最大值也可能为最小值</returns>
        public int randomInt(int min, int max)
        {
            return random.Next(min, max + 1);
        }
        /// <summary>
        /// 随机实数，注意该函数返回的值可能包括最小值，但是不包括最大值。
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>介于最大值与最小值之间，不包括最大值</returns>
        public float randomFloat(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }
        Random random { get; set; }
    }
    public delegate void PlayerIndexWitnessEvent(int playerIndex, EventWitness witness);
    public enum EventPhase
    {
        logic = 0,
        before,
        after
    }

    [Serializable]
    public class ConflictDefineException : Exception
    {
        public ConflictDefineException() { }
        public ConflictDefineException(CardDefine a, CardDefine b) : base(a + "和" + b + "具有相同的ID:" + a.id)
        {
        }
        public ConflictDefineException(string message) : base(message) { }
        public ConflictDefineException(string message, Exception inner) : base(message, inner) { }
        protected ConflictDefineException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}