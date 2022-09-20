using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class PileNameCollection : List<string>
    {
        public PileNameCollection() : base()
        {
        }
        public PileNameCollection(IEnumerable<string> collection) : base(collection)
        {
        }
    }
}