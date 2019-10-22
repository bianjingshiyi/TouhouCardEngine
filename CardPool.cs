using System;
using System.Collections.Generic;
using System.Collections;

namespace TouhouCardEngine
{
    [Serializable]
    public class CardPool : IEnumerable<CardDefine>
    {
        int lastAllocateID { get; set; } = 0;
        public CardPool()
        {
            dicCardDefine = new Dictionary<int, CardDefine>();
        }
        public CardPool(IEnumerable<CardDefine> cardDefines)
        {
            dicCardDefine = new Dictionary<int, CardDefine>();
            foreach (CardDefine define in cardDefines)
            {
                dicCardDefine.Add(define.id, define);
                if (define.id > lastAllocateID)
                    lastAllocateID = define.id;
            }
        }
        public int register(CardDefine define)
        {
            lastAllocateID += 1;
            dicCardDefine.Add(lastAllocateID, define);
            define.id = lastAllocateID;
            return lastAllocateID;
        }
        public CardDefine this[int id]
        {
            get { return dicCardDefine.ContainsKey(id) ? dicCardDefine[id] : null; }
        }
        Dictionary<int, CardDefine> dicCardDefine = null;
        public IEnumerator<CardDefine> GetEnumerator()
        {
            return dicCardDefine.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return dicCardDefine.Values.GetEnumerator();
        }
    }
}