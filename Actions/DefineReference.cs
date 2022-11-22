using System;
namespace TouhouCardEngine
{
    [Serializable]
    public class DefineReference
    {
        public DefineReference(long cardPoolId, int cardId)
        {
            this.cardPoolId = cardPoolId;
            this.id = cardId;
        }
        public DefineReference() : this(0, 0)
        {
        }
        public long cardPoolId;
        public int id;
    }
}