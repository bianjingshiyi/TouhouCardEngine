using System;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class PortDefine
    {
        #region 公有方法
        public PortDefine(Type type, string name, string displayName, bool isParams = false)
        {
            this.type = type;
            this.name = name;
            this.displayName = displayName;
            this.isParams = isParams;
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
                return def.type == type && def.name == name && def.displayName == displayName && def.isParams == isParams;
            }
            return false;
        }
        public override int GetHashCode()
        {
            var hashCode = type.GetHashCode();
            hashCode = (hashCode * 33) ^ name.GetHashCode();
            hashCode = (hashCode * 33) ^ displayName.GetHashCode();
            hashCode = (hashCode * 33) ^ isParams.GetHashCode();
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
        public static PortDefine Value(Type type, string name, bool isParams = false)
        {
            return Value(type, name, name, isParams);
        }
        public static PortDefine Value(Type type, string name, string displayName, bool isParams = false)
        {
            return new PortDefine(type, name, displayName, isParams);
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
        /// <summary>
        /// 是否是变长参数。
        /// 如果该端口定义是输入端口，则表示该输入端口会变长。如果是输出端口，表示这是GeneratedActionEntryNode的变长输入值组成的数组。
        /// </summary>
        public bool isParams { get; set; }
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
            isParams = portDefine.isParams;
        }
        public PortDefine ToPortDefine(TypeFinder typeFinder = null)
        {
            var type = !string.IsNullOrEmpty(typeName) && typeFinder != null ? typeFinder(typeName) : null;
            return new PortDefine(type, name, displayName, isParams);
        }
        public string typeName;
        public string name;
        public string displayName;
        public bool isParams;
    }
}