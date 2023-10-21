using System;


namespace TouhouCardEngine
{
    [Serializable]
    public class NoCardDefineException : Exception
    {
        public NoCardDefineException() { }
        public NoCardDefineException(int id) : base($"There is no card with id {id}") { }
        public NoCardDefineException(string message) : base(message) { }
        public NoCardDefineException(string message, Exception inner) : base(message, inner) { }
        protected NoCardDefineException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}