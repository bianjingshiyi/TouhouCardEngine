using System;

namespace TouhouCardEngine
{
    public class InputDefaultValue
    {
        public InputDefaultValue(string name, int paramIndex, object value)
        {
            this.name = name;
            this.paramIndex = paramIndex;
            this.value = value;
        }
        public string name;
        public int paramIndex = -1;
        public object value;
    }
    [Serializable]
    public class SerializableInputDefaultValue
    {
        public SerializableInputDefaultValue(InputDefaultValue value)
        {
            name = value.name;
            paramIndex = value.paramIndex;
            this.value = value.value;
        }
        public InputDefaultValue deserialize()
        {
            return new InputDefaultValue(name, paramIndex, value);
        }
        public string name;
        public int paramIndex;
        public object value;
    }
}
