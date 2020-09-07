using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NitoriNetwork.Common
{
    [Serializable]
    public class RoomInfo
    {
        /// <summary>
        /// 房间的IP（加入用）
        /// </summary>
        public string ip;
        /// <summary>
        /// 房间的端口（加入用）
        /// </summary>
        public int port;
        /// <summary>
        /// 房间的ID
        /// </summary>
        public Guid id = Guid.Empty;

        /// <summary>
        /// 房主玩家ID (Player ID)
        /// </summary>
        public int OwnerID;

        /// <summary>
        /// 房间用户列表
        /// </summary>
        public List<RoomPlayerInfo> playerList = new List<RoomPlayerInfo>();

        /// <summary>
        /// 内部参数列表
        /// </summary>
        [BsonRequired]
        public List<string> _persistDataList = new List<string>();

        /// <summary>
        /// 是否是同一个房间
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool isSameRoom(RoomInfo other)
        {
            if (other == null)
                return false;
            return id == other.id;
        }
        public void setProp(string name, object value)
        {
            runtimeDic[name] = value;
        }
        [BsonIgnore]
        public Dictionary<string, object> runtimeDic = new Dictionary<string, object>();
        public T getProp<T>(string name)
        {
            if (runtimeDic.ContainsKey(name))
            {
                if (runtimeDic[name] is T t)
                    return t;
                else
                    throw new InvalidCastException(runtimeDic[name] + "无法被转化为类型" + typeof(T).Name);
            }
            else
                throw new KeyNotFoundException(this + "中不存在属性" + name);
        }
        public object getProp(string name)
        {
            return runtimeDic[name];
        }
        public bool tryGetProp<T>(string name, out T value)
        {
            if (runtimeDic.ContainsKey(name) && runtimeDic[name] is T t1)
            {
                value = t1;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        public static Type getType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
                return type;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            return type;
        }
        public RoomInfo serialize()
        {
            _persistDataList.Clear();
            foreach (var pair in runtimeDic)
            {
                _persistDataList.Add(pair.Key + ":(" + pair.Value.GetType().FullName + ")" + pair.Value.ToJson());
            }
            return this;
        }
        public RoomInfo deserialize()
        {
            runtimeDic.Clear();
            foreach (string data in _persistDataList)
            {
                if (Regex.Match(data, @"(?<name>.+):\((?<type>.+)\)(?<json>.+)") is var m && m.Success)
                {
                    runtimeDic.Add(m.Groups["name"].Value, BsonSerializer.Deserialize(m.Groups["json"].Value, getType(m.Groups["type"].Value)));
                }
                else
                    throw new FormatException("错误的序列化格式：" + data);
            }
            return this;
        }

        public RoomInfo()
        {
            // 自动生成一个id
            id = Guid.NewGuid();
        }

        /// <summary>
        /// 房主ID
        /// </summary>
        /// <param name="ownerID"></param>
        public RoomInfo(int ownerID) : this()
        {
            OwnerID = ownerID;
        }
    }
}
