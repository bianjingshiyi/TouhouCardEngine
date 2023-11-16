using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedEventArg : EventArg
    {
        public GeneratedEventArg()
        {
        }
        public GeneratedEventArg(GeneratedActionDefine actionDefine)
        {
            this.actionDefine = actionDefine;
            if (actionDefine != null)
            {
                beforeNames = new string[] { actionDefine.getBeforeEventName() };
                afterNames = new string[] { actionDefine.getAfterEventName() };
            }
        }

        public override void Record(IGame game, EventRecord record)
        {
            foreach (var varName in actionDefine.getAllEventArgVarNames())
            {
                var value = getVar(varName);
                if (value is Card card)
                {
                    record.setCardState(varName, card);
                }
                else if (value is IEnumerable<Card> cards)
                {
                    record.setCardStates(varName, cards);
                }
                else
                {
                    record.setVar(varName, value);
                }
            }
        }
        public override EventVariableInfo[] getBeforeEventVarInfos()
        {
            return actionDefine?.getBeforeEventArgVarInfos();
        }
        public override EventVariableInfo[] getAfterEventVarInfos()
        {
            return actionDefine?.getAfterEventArgVarInfos();
        }
        public override string ToString()
        {
            return actionDefine?.getEventName() ?? "null";
        }
        public GeneratedActionDefine actionDefine;
    }
}
