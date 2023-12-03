using System;
using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    public partial class CardEngine
    {
        #region 公有方法
        public void addEventDefine(EventDefine actionDefine)
        {
            eventDefines.Add(actionDefine);
        }
        public EventDefine getEventDefine(EventReference reference)
        {
            return eventDefines.FirstOrDefault(e => e.cardPoolId == reference.cardPoolId && e.eventName == reference.eventName);
        }
        public T getEventDefine<T>() where T : EventDefine
        {
            return eventDefines.OfType<T>().FirstOrDefault();
        }
        public GeneratedEventDefine getGeneratedEventDefine(ActionReference actionRef)
        {
            var action = getActionDefine(actionRef);
            if (action is not GeneratedActionDefine genAction)
                return null;
            return eventDefines.OfType<GeneratedEventDefine>().FirstOrDefault(e => e.cardPoolId == genAction.cardPoolId && e.eventName == genAction.getEventName());
        }
        
        #endregion

        public List<EventDefine> eventDefines = new List<EventDefine>();
    }
}