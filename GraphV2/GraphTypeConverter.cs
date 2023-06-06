using System;
using System.Collections.Generic;

namespace TouhouCardEngine
{
    public abstract class ParamConversion
    {
        public abstract bool canConvert(Type from, Type to);
        public abstract object convert(Flow flow, object input, Type to);
    }
}
