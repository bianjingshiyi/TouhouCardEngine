using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NitoriNetwork.Common;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    public abstract class GameOption
    {
        public int randomSeed = 0;
    }
    [Serializable]
    public partial class CardEngine : IGame, IChangeableGame
    {
        #region 公共方法

        #region 构造器
        public CardEngine(Rule rule)
        {
            random = new RNG(0);
            responseRNG = new RNG(0);
            this.rule = rule;
        }
        public CardEngine() : this(null)
        {

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
            random = new RNG(options.randomSeed);
            responseRNG = new RNG(options.randomSeed);
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
            addCard(card);
            triggers.addChange(new CreateCardChange(this, card));
            return card;
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
        /// 获取某张卡牌在某事件发生前的快照。
        /// </summary>
        /// <param name="card">目标卡牌。</param>
        /// <param name="arg">事件。</param>
        /// <returns>卡牌快照。</returns>
        public CardSnapshot getCardSnapshotBeforeEvent(Card card, IEventArg arg)
        {
            var eventIndex = triggers.getEventIndexBefore(arg);
            var snapshot = snapshotCard(card);
            triggers.revertChanges(snapshot, eventIndex);
            return snapshot;
        }
        /// <summary>
        /// 获取某张卡牌在某事件发生后的快照。
        /// </summary>
        /// <param name="card">目标卡牌。</param>
        /// <param name="arg">事件。</param>
        /// <returns>卡牌快照。</returns>
        public CardSnapshot getCardSnapshotAfterEvent(Card card, IEventArg arg)
        {
            var eventIndex = triggers.getEventIndexAfter(arg);
            var snapshot = snapshotCard(card);
            triggers.revertChanges(snapshot, eventIndex);
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
        public object getProp(string varName)
        {
            return getProp<object>(varName);
        }
        public void setProp(string propName, object value)
        {
            var beforeValue = getProp(propName);
            setPropRaw(propName, value);
            triggers.addChange(new GamePropChange(this, propName, beforeValue, value));
        }
        public void setProp<T>(string propName, T value)
        {
            setProp(propName, (object)value);
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
                var beforeRandomInts = nextRandomIntList.ToArray();
                nextRandomIntList.RemoveAt(0);
                var afterRandomInts = nextRandomIntList.ToArray();
                triggers.addChange(new SetNextRandomChange(this, beforeRandomInts, afterRandomInts));
                return result;
            }
            var beforeState = random.state;
            var randomResult = random.next(min, max + 1);
            var afterState = random.state;
            triggers.addChange(new RandomChange(this, beforeState, afterState));
            return randomResult;
        }
        /// <summary>
        /// 随机实数，注意该函数返回的值可能包括最小值，但是不包括最大值。
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>介于最大值与最小值之间，不包括最大值</returns>
        public float randomFloat(float min, float max)
        {
            var beforeState = random.state;
            var result = (float)(random.nextDouble() * (max - min) + min);
            var afterState = random.state;
            triggers.addChange(new RandomChange(this, beforeState, afterState));
            return result;
        }
        public void setNextRandomInt(params int[] results)
        {
            var beforeRandomInts = nextRandomIntList.ToArray();
            setNextRandomIntRaw(results);
            var afterRandomInts = nextRandomIntList.ToArray();
            triggers.addChange(new SetNextRandomChange(this, beforeRandomInts, afterRandomInts));
        }
        /// <summary>
        /// 获取下一次的请求随机数。用于解决回放中，AI选择选项，或者卡牌选择超时后，随机选择的选项不会过随机数，导致炸rep的问题。
        /// </summary>
        /// <param name="min">随机最小值，包括这个数字。</param>
        /// <param name="max">随机最大值，不包括这个数字。</param>
        /// <returns>获取到的随机数。</returns>
        public int responseRandomInt(int min, int max)
        {
            var beforeState = responseRNG.state;
            var result = responseRNG.next(min, max);
            var afterState = responseRNG.state;
            triggers.addChange(new ResponseRNGChange(this, beforeState, afterState));
            return result;
        }
        #endregion
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

        #endregion

        #region 私有方法

        #region 接口实现
        void IChangeableGame.setRandomState(uint state)
        {
            random.setState(state);
        }
        void IChangeableGame.setResponseRNGState(uint state)
        {
            responseRNG.setState(state);
        }
        void IChangeableGame.setNextRandomInt(int[] results)
        {
            setNextRandomIntRaw(results);
        }
        void IChangeableGame.setProp(string name, object value)
        {
            setPropRaw(name, value);
        }
        void IChangeableGame.addCard(Card card)
        {
            addCard(card);
        }
        void IChangeableGame.removeCard(int cardId)
        {
            removeCard(cardId);
        }
        #endregion
        private void addCard(Card card)
        {
            cardIdDic.Add(card.id, card);
        }
        private void removeCard(int cardId)
        {
            cardIdDic.Remove(cardId);
        }
        private void setPropRaw(string propName, object value)
        {
            dicVar[propName] = value;
        }
        private void setNextRandomIntRaw(int[] results)
        {
            nextRandomIntList.Clear();
            nextRandomIntList.AddRange(results);
        }
        #endregion

        #region 属性字段
        public Rule rule { get; set; }

        #region 管理器
        public ITimeManager time { get; set; } = null;
        public ITriggerManager triggers { get; set; } = null;
        public ISnapshoter snapshoter { get; set; } = null;
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
        private IAnswerManager _answers;
        #endregion

        #region 游戏流程
        public bool isRunning { get; set; } = false;
        public bool isInited { get; set; } = false;
        public GameOption option;
        #endregion

        #region 玩家
        public Player[] players
        {
            get { return playerList.ToArray(); }
        }
        public int playerCount
        {
            get { return playerList.Count; }
        }
        /// <summary>
        /// 获取所有玩家，玩家在数组中的顺序与玩家被添加的顺序相同。
        /// </summary>
        /// <remarks>为什么不用属性是因为每次都会生成一个数组。</remarks>
        private List<Player> playerList = new List<Player>();
        #endregion

        #region 随机数
        private List<int> nextRandomIntList { get; } = new List<int>();
        private RNG random { get; set; }
        private RNG responseRNG { get; set; }
        #endregion

        private Dictionary<string, object> dicVar { get; } = new Dictionary<string, object>();
        private Dictionary<int, Card> cardIdDic { get; } = new Dictionary<int, Card>();
        #endregion
    }
}