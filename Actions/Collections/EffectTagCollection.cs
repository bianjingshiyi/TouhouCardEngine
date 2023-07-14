using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class EffectTagCollection : List<string>, ICloneable
    {
        public EffectTagCollection() : base()
        {
        }
        public EffectTagCollection(IEnumerable<string> keywords) : base(keywords)
        {
        }
        public object Clone()
        {
            return new EffectTagCollection(this);
        }
    }
}