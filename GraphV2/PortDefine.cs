using System;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class PortDefine
    {
        #region 公有方法
        public PortDefine(Type type, string name, string displayName)
        {
            this.type = type;
            this.name = name;
            this.displayName = displayName;
        }
        public PortDefine(Type type, string name) : this(type, name, null)
        {
        }
        public PortDefine(Type type) : this(type, string.Empty)
        {
        }
        public PortType GetPortType()
        {
            if (typeof(Node).IsAssignableFrom(type))
            {
                return PortType.Control;
            }
            return PortType.Value;
        }
        public string getDisplayName()
        {
            return displayName ?? name ?? string.Empty;
        }
        public override bool Equals(object obj)
        {
            if (obj is PortDefine def)
            {
                return def.type == type && def.name == name && def.displayName == displayName;
            }
            return false;
        }
        public override int GetHashCode()
        {
            var hashCode = type.GetHashCode();
            hashCode = (hashCode * 33) ^ name.GetHashCode();
            hashCode = (hashCode * 33) ^ displayName.GetHashCode();
            return hashCode;
        }

        public static PortDefine Control(string name)
        {
            return Control(name, name);
        }
        public static PortDefine Control(string name, string displayName)
        {
            return new PortDefine(typeof(ActionNode), name, displayName);
        }
        public static PortDefine Value(Type type, string name)
        {
            return Value(type, name, name);
        }
        public static PortDefine Value(Type type, string name, string displayName)
        {
            return new PortDefine(type, name, displayName);
        }
        public static PortDefine Const(Type type, string name)
        {
            return Value(type, name);
        }
        public static PortDefine Const(Type type, string name, string displayName)
        {
            return Value(type, name, displayName);
        }
        #endregion
        #region 属性字段
        public Type type { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        #endregion
    }
    [Serializable]
    public class SerializablePortDefine
    {
        public SerializablePortDefine(PortDefine portDefine)
        {
            typeName = portDefine.type.Name;
            name = portDefine.name;
            displayName = portDefine.displayName;
        }
        public PortDefine ToPortDefine(TypeFinder typeFinder = null)
        {
            var type = !string.IsNullOrEmpty(typeName) && typeFinder != null ? typeFinder(typeName) : null;
            return new PortDefine(type, name, displayName);
        }
        public string typeName;
        public string name;
        public string displayName;
    }
}