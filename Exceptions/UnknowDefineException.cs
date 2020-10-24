using System;

namespace TouhouCardEngine
{
    [Serializable]
    public class UnknowDefineException : Exception
    {
        public UnknowDefineException() { }
        public UnknowDefineException(int id) : base("未知的卡片定义：" + id) { }
        public UnknowDefineException(Type type) : base("未加载的卡片定义：" + type.Name) { }
        public UnknowDefineException(string message) : base(message) { }
        public UnknowDefineException(string message, Exception inner) : base(message, inner) { }
        protected UnknowDefineException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}