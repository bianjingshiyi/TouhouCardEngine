using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class KeywordCollection : List<string>
    {
        public KeywordCollection() : this(null)
        {
        }
        public KeywordCollection(IEnumerable<string> keywords)
        {
            if (keywords != null)
                AddRange(keywords);
        }
    }
}