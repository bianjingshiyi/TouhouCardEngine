using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class TriggerCollection : List<TriggerGraph>, ICloneable
    {
        public TriggerCollection() : base()
        {
        }
        public TriggerCollection(IEnumerable<TriggerGraph> triggers) : base(triggers)
        {
        }
        public object Clone()
        {
            return new TriggerCollection(this);
        }
    }
}