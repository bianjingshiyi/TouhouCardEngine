using System;
namespace TouhouCardEngine
{
    public class ValueDefine
    {
        #region 公有方法
        public ValueDefine(Type type, string name, bool isParams, bool isOut)
        {
            this.type = type;
            this.name = name;
            this.isParams = isParams;
            this.isOut = isOut;
        }
        #endregion
        #region 属性字段
        public Type type { get; set; }
        public string name { get; set; }
        public bool isParams { get; set; }
        public bool isOut { get; set; }
        #endregion
    }
    [Serializable]
    public class SerializableValueDefine
    {
        #region 公有方法
        #region 构造函数
        public SerializableValueDefine(ValueDefine valueDefine)
        {
            typeName = valueDefine.type.IsArray ? valueDefine.type.GetElementType().FullName : valueDefine.type.FullName;
            name = valueDefine.name;
            isParams = valueDefine.isParams;
            isArray = valueDefine.type.IsArray;
        }
        #endregion
        public ValueDefine toValueDefine(Func<string, Type> typeFinder = null)
        {
            Type type;
            if (typeFinder != null)
            {
                type = typeFinder(typeName.EndsWith("[]") ? typeName.Substring(0, typeName.Length - 2) : typeName);
            }
            else
            {
                type = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        break;
                }
            }
            if (isArray)
                type = type.MakeArrayType();
            return new ValueDefine(type, name, isParams, false);
        }
        #endregion
        #region 属性字段
        public string typeName;
        public string name;
        public bool isParams;
        public bool isArray;
        #endregion
    }
}