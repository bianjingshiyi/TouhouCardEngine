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
}
