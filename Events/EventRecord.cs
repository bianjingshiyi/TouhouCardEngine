using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EventRecord
    {
        #region 公有方法

        #region 构造器
        public EventRecord(CardEngine game, IEventArg arg)
        {
            this.game = game;
            eventArg = arg;
            isCanceled = arg.isCanceled;
        }
        #endregion

        #region 变量
        public object getVar(string varName)
        {
            if (varDict.TryGetValue(varName, out object value))
                return value;
            else
                return null;
        }
        public T getVar<T>(string varName)
        {
            if (getVar(varName) is T t)
                return t;
            else
                return default;
        }
        public void setCardState(string varName, Card card)
        {
            if (card == null)
            {
                setVar(varName, new CardState(null, -1));
                return;
            }
            int stateIdx = game.triggers.getCurrentEventIndex();
            CardState state = new CardState(card, stateIdx);
            setVar(varName, state);
        }
        public void setCardStates(string varName, IEnumerable<Card> cards)
        {
            if (cards == null)
                return;
            CardState[] states = new CardState[cards.Count()];
            for (int i = 0; i < states.Length; i++)
            {
                var card = cards.ElementAt(i);
                if (card == null)
                {
                    states[i] = new CardState(null, -1);
                    continue;
                }
                int stateIdx = game.triggers.getCurrentEventIndex();
                CardState state = new CardState(card, stateIdx);
                states[i] = state;
            }
            setVar(varName, states);
        }
        public void setVar(string varName, object value)
        {
            if (varDict.ContainsKey(varName))
                varDict[varName] = value;
            else
                varDict.Add(varName, value);
        }
        #endregion

        #endregion

        #region 属性字段
        public IEventArg eventArg { get; internal set; }
        public bool isCanceled { get; internal set; }
        public bool isCompleted { get; set; }
        private Dictionary<string, object> varDict { get; } = new Dictionary<string, object>();
        private CardEngine game;
        #endregion
    }
}