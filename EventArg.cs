using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EventArg : IEventArg
    {
        public IEventArg[] getChildEvents()
        {
            return childEventList.ToArray();
        }
        public object getVar(string varName)
        {
            if (varDict.TryGetValue(varName, out object value))
                return value;
            else
                return null;
        }
        public void setVar(string varName, object value)
        {
            varDict[varName] = value;
        }
        public IGame game;
        public string[] beforeNames { get; set; }
        public string[] afterNames { get; set; }
        public object[] args { get; set; }
        public bool isCanceled { get; set; }
        public int repeatTime { get; set; }
        public Func<IEventArg, Task> action { get; set; }
        public List<IEventArg> childEventList { get; } = new List<IEventArg>();
        public IEventArg parent
        {
            get => _parent;
            set
            {
                _parent = value;
                if (value is EventArg ea)
                    ea.childEventList.Add(this);
            }
        }
        IEventArg _parent;
        Dictionary<string, object> varDict { get; } = new Dictionary<string, object>();
    }
}