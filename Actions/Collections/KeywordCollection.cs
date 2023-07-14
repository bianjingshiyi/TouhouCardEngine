using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class KeywordCollection : List<string>, ICloneable
    {
        public KeywordCollection() : base()
        {
        }
        public KeywordCollection(IEnumerable<string> keywords) : base(keywords)
        {
        }
        public object Clone()
        {
            return new KeywordCollection(this);
        }
    }
}