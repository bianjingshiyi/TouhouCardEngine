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
}