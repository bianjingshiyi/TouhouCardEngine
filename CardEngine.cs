using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
using NitoriNetwork.Common;
using TouhouCardEngine.Shared;
using System.Reflection;

namespace TouhouCardEngine
{
    public abstract class GameOption
    {
        public int randomSeed = 0;
    }
    [Serializable]
    public partial class CardEngine : IGame
    {
        #region 公共方法
        #region 构造器
        public CardEngine(Rule rule)
        {
            //trigger = new SyncTriggerSystem(this);
            random = new Random(0);
            this.rule = rule;
        }
        public CardEngine() : this(null)
        {

        }
        #endregion
        public Task command(CommandEventArg command)
        {
            return rule.onPlayerCommand(this, getPlayer(command.playerId), command);
        }
        public virtual void Dispose()
        {
            close();
            if (answers != null)
                answers.Dispose();
            if (triggers != null)
                triggers.Dispose();
            if (time != null)
                time.Dispose();
        }
        public virtual void onAnswer(IResponse response)
        {
        }
        #region 牌堆
        public Pile this[string pileName]
        {
            get { return getPile(pileName); }
        }
        public void addPile(Pile pile)
        {
            pileList.Add(pile);
            pile.owner = null;
            foreach (Card card in pile)
            {
                card.owner = null;
            }
        }
        public Pile getPile(string name)
        {
            return pileList.FirstOrDefault(e => { return e.name == name; });
        }
        public Pile[] getPiles()
        {
            return pileList.ToArray();
        }
        #endregion
        #region 游戏流程
        public Task init(Assembly[] assemblies, Rule rule, GameOption options, IRoomPlayer[] players)
        {
            this.rule = rule;
            if (isInited)
            {
                logger.logError("游戏已经初始化");
                return Task.CompletedTask;
            }
            isInited = true;
            //初始化随机
            random = new Random(options.randomSeed);
            //初始化动作定义
            foreach (var pair in ActionDefine.loadDefinesFromAssemblies(assemblies))
            {
                addActionDefine(new ActionReference(0, pair.Key), pair.Value);
            }
            return rule.onGameInit(this, options, players);
        }
        public Task run()
        {
            if (!isInited)
            {
                logger.logError("游戏未初始化");
                return Task.CompletedTask;
            }
            if (isRunning)
            {
                logger.logError("游戏已开始");
                return Task.CompletedTask;
            }
            isRunning = true;
            return rule.onGameRun(this);
        }

        public void initAndRun(Rule rule, GameOption options, IRoomPlayer[] players)
        {
            this.rule = rule;
            rule.onGameInit(this, options, players);
            isInited = true;
            isRunning = true;
            rule.onGameRun(this);
        }
        /// <summary>
        /// 开始游戏
        /// </summary>
        /// <returns></returns>
        public async Task startGame()
        {
            await rule.onGameStart(this);
        }

        public void close()
        {
            rule.onGameClose(this);
        }
        #endregion
        #region CardDefine
        public T getDefine<T>() where T : CardDefine
        {
            foreach (var cardPool in rule.cardDict)
            {
                foreach (var card in cardPool.Value)
                {
                    if (card.Value is T t)
                        return t;
                }
            }
            return null;
        }
        public T getDefine<T>(long cardPoolId, int cardId) where T : CardDefine
        {
            if (rule.cardDict.TryGetValue(cardPoolId, out var cardPool))
            {
                if (cardPool.TryGetValue(cardId, out CardDefine cardDefine) && cardDefine is T t)
                    return t;
            }
            return default;
        }
        public CardDefine getDefine(long cardPoolId, int id)
        {
            if (rule.cardDict.TryGetValue(cardPoolId, out var cardDefineDict))
            {
                if (cardDefineDict.TryGetValue(id, out var cardDefine))
                    return cardDefine;
            }
            throw new UnknowDefineException(id);
        }
        public CardDefine getDefine(DefineReference defRef)
        {
            return getDefine(defRef.cardPoolId, defRef.defineId);
        }
        public CardDefine[] getDefines()
        {
            IEnumerable<CardDefine> cardDefines = Enumerable.Empty<CardDefine>();
            foreach (var cardPool in rule.cardDict.Values)
            {
                cardDefines = cardDefines.Concat(cardPool.Values);
            }
            return cardDefines.ToArray();
        }
        public CardDefine[] getDefines(IEnumerable<DefineReference> cardRefs)
        {
            return cardRefs.Select(cardRef => getDefine(cardRef.cardPoolId, cardRef.defineId)).ToArray();
        }
        public long getCardPoolIdOfDefine(CardDefine cardDefine)
        {
            foreach (var cardPool in rule.cardDict)
            {
                if (cardPool.Value.ContainsKey(cardDefine.id))
                    return cardPool.Key;
            }
            return 0;
        }
        #endregion
        #region Card
        /// <summary>
        /// Create one uninitialized card with only card define and record the card in the engine's bank
        /// </summary>
        /// <param name="define"></param>
        /// <returns></returns>
        public Card createCard(CardDefine define)
        {
            int id = cardIdDic.Count + 1;
            while (cardIdDic.ContainsKey(id))
                id++;
            Card card = new Card(id, define);
            cardIdDic.Add(id, card);
            return card;
        }
        public Card createCard(long cardPoolId, int cardDefineId)
        {
            return createCard(getDefine(cardPoolId, cardDefineId));
        }
        public Card getCard(int id)
        {
            if (cardIdDic.TryGetValue(id, out var card))
                return card;
            else
                return null;
        }
        public Card[] getCards(int[] ids)
        {
            return ids.Select(id => getCard(id)).ToArray();
        }
        public CardSnapshot snapshotCard(Card card)
        {
            return snapshoter?.snapshot(this, card);
        }
        /// <summary>
        /// 获取某张卡牌在某事件发生后的快照。
        /// </summary>
        /// <param name="card">目标卡牌。</param>
        /// <param name="record">事件记录。</param>
        /// <returns>卡牌快照。</returns>
        public CardSnapshot getCardSnapshotOfEvent(Card card, EventRecord record)
        {
            var records = triggers.getEventRecords();
            var recordIndex = Array.IndexOf(records, record);
            var histories = card.getHistories();
            int historyIndex = -1;
            for (int i = 0; i < histories.Length; i++)
            {
                var history = histories[i];
                var rec = triggers.getEventRecord(history.eventArg);
                if (rec == null)
                    continue;
                var historyRecIndex = Array.IndexOf(records, rec);
                if (historyRecIndex <= recordIndex) // 还没到，或者刚好是指定事件
                {
                    historyIndex = i;
                }
                if (historyRecIndex >= recordIndex) // 超过了指定事件
                {
                    break;
                }
            }
            if (historyIndex < 0)
            {
                return snapshotCard(card);
            }
            var snapshot = snapshotCard(card);
            card.revertToHistory(snapshot, historyIndex);
            return snapshot;
        }
        #endregion
        #region Props
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
        #endregion
        #region 玩家
        public Player getPlayer(int playerId)
        {
            return playerList.FirstOrDefault(p => p.id == playerId);
        }
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
        public void addPlayer(Player player)
        {
            playerList.Add(player);
        }
        public async Task initPlayer(Player player)
        {
            await rule.onPlayerInit(this, player);
        }
        #endregion
        #region 随机数
        //public delegate void EventAction(Event @event);
        //public event EventAction beforeEvent;
        //public event EventAction afterEvent;
        //Event currentEvent { get; set; } = null;
        //List<Event> eventList { get; } = new List<Event>();
        /// <summary>
        /// 随机整数1~max
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        public int dice(int max)
        {
            return randomInt(1, max);
        }
        /// <summary>
        /// 随机整数，注意该函数返回的值可能包括最大值与最小值。
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>介于最大值与最小值之间，可能为最大值也可能为最小值</returns>
        public int randomInt(int min, int max)
        {
            if (nextRandomIntList.Count > 0)
            {
                int result = nextRandomIntList[0];
                nextRandomIntList.RemoveAt(0);
                return result;
            }
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
        public void setNextRandomInt(params int[] results)
        {
            nextRandomIntList.Clear();
            nextRandomIntList.AddRange(results);
        }
        #endregion
        #endregion
        #region 私有方法
        protected int getNewPlayerId()
        {
            int id = playerList.Count;
            if (playerList.Any(p => p.id == id))
                id++;
            return id;
        }
        #endregion
        #region 属性字段
        public Rule rule { get; set; }
        #region 管理器
        public ITimeManager time { get; set; } = null;
        public ITriggerManager triggers { get; set; } = null;
        public ISnapshoter snapshoter { get; set; } = null;
        //public SyncTriggerSystem trigger { get; }
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
        #endregion
        #region 游戏流程
        public bool isRunning { get; set; } = false;
        public bool isInited { get; set; } = false;
        public GameOption option;
        #endregion
        #region 玩家

        /// <summary>
        /// 获取所有玩家，玩家在数组中的顺序与玩家被添加的顺序相同。
        /// </summary>
        /// <remarks>为什么不用属性是因为每次都会生成一个数组。</remarks>
        private List<Player> playerList = new List<Player>();
        public Player[] players
        {
            get { return playerList.ToArray(); }
        }
        public int playerCount
        {
            get { return playerList.Count; }
        }
        #endregion

        internal Dictionary<string, object> dicVar { get; } = new Dictionary<string, object>();
        Dictionary<int, Card> cardIdDic { get; } = new Dictionary<int, Card>();
        private List<Pile> pileList { get; } = new List<Pile>();
        List<int> nextRandomIntList { get; } = new List<int>();
        Random random { get; set; }
        #endregion
        #region 内部类
        public class CommandEventArg : EventArg
        {
            public int playerId;
            public string commandName;
            public object[] commandArgs;
            public override void Record(IGame game, EventRecord record)
            {
            }
        }
        #endregion
    }
    public enum EventPhase
    {
        logic = 0,
        before,
        after
    }
}