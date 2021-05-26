using System;

namespace TouhouCardEngine
{
    [Serializable]
    public class ConflictDefineException : Exception
    {
        public ConflictDefineException() { }
        public ConflictDefineException(CardDefine a, CardDefine b) : base(a + "和" + b + "具有相同的ID:" + a.defineNumber)
        {
        }
        public ConflictDefineException(string message) : base(message) { }
        public ConflictDefineException(string message, Exception inner) : base(message, inner) { }
        protected ConflictDefineException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}