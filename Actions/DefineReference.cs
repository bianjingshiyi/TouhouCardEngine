using System;
namespace TouhouCardEngine
{
    [Serializable]
    public class DefineReference
    {
        public DefineReference(long cardPoolId, int defineId)
        {
            this.cardPoolId = cardPoolId;
            this.defineId = defineId;
        }
        public DefineReference() : this(0, 0)
        {
        }
        public long cardPoolId;
        public int defineId;
    }
    [Obsolete("use DefineReference instead")]
    [Serializable]
    public class CardReference
    {
        public long cardPoolId;
        public int cardId;
    }
}