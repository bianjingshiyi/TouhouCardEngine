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
        public DefineReference(CardDefine cardDefine) :
            this(cardDefine.cardPoolId, cardDefine.id)
        {
        }
        public DefineReference(BuffDefine buffDefine) :
            this(buffDefine.cardPoolId, buffDefine.id)
        {
        }
        public DefineReference() : this(0, 0)
        {
        }
        public override bool Equals(object obj)
        {
            if (obj is DefineReference other)
            {
                return cardPoolId == other.cardPoolId && defineId == other.defineId;
            }
            return false;
        }
        public override int GetHashCode()
        {
            //不知道从哪里抄的异或哈希算法，使用了质数。
            int hashCode = 17;
            hashCode = hashCode * 31 + cardPoolId.GetHashCode();
            hashCode = hashCode * 31 + defineId.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            return string.Format("({0}, {1})", cardPoolId, defineId);
        }
        public static bool operator==(DefineReference r1, DefineReference r2)
        {
            return r1.Equals(r2);
        }
        public static bool operator!=(DefineReference r1, DefineReference r2)
        {
            return !r1.Equals(r2);
        }

        public long cardPoolId;
        public int defineId;

        public static DefineReference Empty = new DefineReference(0, 0);
    }
    [Obsolete("use DefineReference instead")]
    [Serializable]
    public class CardReference
    {
        public long cardPoolId;
        public int cardId;
    }
}