using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EventArg : IEventArg
    {
        public IGame game;
        public string[] beforeNames { get; set; }
        public string[] afterNames { get; set; }
        public object[] args { get; set; }
        public bool isCanceled { get; set; }
        public int repeatTime { get; set; }
        public Func<IEventArg, Task> action { get; set; }
        public List<IEventArg> childEventList { get; } = new List<IEventArg>();
        public IEventArg[] getChildEvents()
        {
            return childEventList.ToArray();
        }
        IEventArg _parent;
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
    }
}