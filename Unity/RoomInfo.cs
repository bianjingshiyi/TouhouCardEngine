using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;

namespace TouhouCardEngine
{
    [Serializable]
    public class RoomInfo
    {
        public string ip;
        public int port;
        public Guid id = Guid.Empty;

        public List<RoomPlayerInfo> playerList = new List<RoomPlayerInfo>();
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
            return (T)runtimeDic[name];
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
                _persistDataList.Add(pair.Key + ":(" + pair.Value.GetType().FullName + ")" + JsonConvert.SerializeObject(pair.Value));
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
                    runtimeDic.Add(m.Groups["name"].Value, JsonConvert.DeserializeObject(m.Groups["json"].Value, getType(m.Groups["type"].Value)));
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
    }
}
