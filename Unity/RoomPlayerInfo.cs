using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
namespace TouhouCardEngine
{
    [Serializable]
    public class RoomPlayerInfo
    {
        /// <summary>
        /// 游戏房间中的ID，由房间自行分配
        /// </summary>
        public int RoomID { get => id; set => id = value; }
        /// <summary>
        /// 玩家自身的ID，唯一不重复
        /// </summary>
        public int PlayerID { get => playerID; set => playerID = value; }

        private int id = 0;
        private int playerID = 0;
        public string name = null;
        public bool ready = false;

        public RoomPlayerInfo()
        {
            playerID = new Random().Next();
        }

        public Dictionary<string, KeyValuePair<string, string>> propJsonDic = new Dictionary<string, KeyValuePair<string, string>>();
        public void setProp(string name, object value)
        {
            if (propJsonDic.ContainsKey(name))
            {
                propJsonDic[name] = new KeyValuePair<string, string>(value.GetType().FullName, value.ToJson());
            }
            else
            {
                propJsonDic.Add(name, new KeyValuePair<string, string>(value.GetType().FullName, value.ToJson()));
            }
        }
        [NonSerialized]
        Dictionary<string, object> cacheDic = new Dictionary<string, object>();

        public T getProp<T>(string name)
        {
            if (cacheDic.ContainsKey(propJsonDic[name].Value) && cacheDic[propJsonDic[name].Value] is T t1)
                return t1;
            if (BsonSerializer.Deserialize(propJsonDic[name].Value, RoomInfo.getType(propJsonDic[name].Key)) is T t2)
            {
                cacheDic.Add(propJsonDic[name].Value, t2);
                return t2;
            }
            else
                throw new InvalidTypeException(name + "的类型" + propJsonDic[name].Key + "与返回类型" + typeof(T).FullName + "不一致");
        }
        public bool tryGetProp<T>(string name, out T value)
        {
            if (!propJsonDic.ContainsKey(name))
            {
                value = default;
                return false;
            }
            if (cacheDic.ContainsKey(propJsonDic[name].Value) && cacheDic[propJsonDic[name].Value] is T t1)
            {
                value = t1;
                return true;
            }
            if (RoomInfo.getType(propJsonDic[name].Key) is Type type && BsonSerializer.Deserialize(propJsonDic[name].Value, type) is T t2)
            {
                cacheDic.Add(propJsonDic[name].Value, t2);
                value = t2;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
