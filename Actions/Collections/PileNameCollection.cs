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
        public object Clone()
        {
            return new PileNameCollection(this);
        }
    }
}