using System;
using System.Collections.Generic;

namespace TouhouCardEngine
{
    [Serializable]
    public class PileNameCollection : List<string>, ICloneable
    {
        public PileNameCollection() : base()
        {
        }
        public PileNameCollection(IEnumerable<string> collection) : base(collection)
        {
        }
        public virtual object Clone()
        {
            return new PileNameCollection(this);
        }
    }
    [Serializable]
    public class PileNameRange : PileNameCollection
    {
        public PileNameRange() : base()
        {
        }
        public PileNameRange(IEnumerable<string> collection) : base(collection)
        {
        }
        public override object Clone()
        {
            return new PileNameRange(this);
        }
    }
}