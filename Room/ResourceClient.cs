using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine.Networking;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace TouhouCardEngine
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// 卡池定义
        /// </summary>
        CardDefine,
        /// <summary>
        /// 图像资源
        /// </summary>
        Picture,
        /// <summary>
        /// 音频资源
        /// </summary>
        Sound,
    }

    public static class ResourceTypeHelper
    {
        /// <summary>
        /// 将资源类型枚举转换为url文本
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetString(this ResourceType type)
        {
            switch (type)
            {
                case ResourceType.CardDefine:
                    return "card";
                case ResourceType.Picture:
                    return "pic";
                case ResourceType.Sound:
                    return "sound";
                default:
                    return "unknown";
            }
        }
    }

    /// <summary>
    /// 资源服务器配套客户端
    /// </summary>
    public class ResourceClient
    {
        /// <summary>
        /// 资源服务器的基URL
        /// </summary>
        Uri BaseUrl { get; }
        /// <summary>
        /// 验证用Session
        /// </summary>
        string Session { get; } = "";

        /// <summary>
        /// 新建资源客户端
        /// </summary>
        /// <param name="baseUri">资源服务器的基URL</param>
        /// <param name="session">验证用Session。仅适用于服务器模式，在局域网模式下必须设置为空，否则会导致Session泄露。</param>
        public ResourceClient(string baseUri, string session = "")
        {
            BaseUrl = new Uri(baseUri);
            Session = session;
        }

        /// <summary>
        /// 新建获取资源的请求
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private UnityWebRequestAsyncOperation requestGet(ResourceType type, string id)
        {
            var req = UnityWebRequest.Get(resourceUri(type, id));
            req.SetRequestHeader("Cookie", $"Session={Session}");
            return req.SendWebRequest();
        }

        /// <summary>
        /// 从服务器获取资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public byte[] GetResource(ResourceType type, string id)
        {
            UnityWebRequestAsyncOperation op = requestGet(type, id);

            while (!op.isDone) ;
            return parseFileResponse(op);
        }

        /// <summary>
        /// 从服务器获取资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<byte[]> GetResourceAsync(ResourceType type, string id)
        {
            var completeSource = new TaskCompletionSource<byte[]>();

            var op = requestGet(type, id);
            op.completed += (_) =>
            {
                try
                {
                    completeSource.SetResult(parseFileResponse(op));
                }
                catch (Exception e)
                {
                    completeSource.SetException(e);
                }
            };

            return completeSource.Task;
        }

        /// <summary>
        /// 新建上传资源的请求
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private UnityWebRequestAsyncOperation requestPost(ResourceType type, string id, byte[] data)
        {
            // 压缩上传资源
            if (type == ResourceType.CardDefine)
                data = Compression.compress(data);

            var form = new MultipartFormFileSection("file", data, id, "application/octet-stream");
            var list = new List<IMultipartFormSection>() { form };
            var req = UnityWebRequest.Post(resourceUri(type, id), list);
            req.SetRequestHeader("Cookie", $"Session={Session}");

            return req.SendWebRequest();
        }

        /// <summary>
        /// Post 一个 Json
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private UnityWebRequestAsyncOperation postJson(Uri uri, string data)
        {
            var request = new UnityWebRequest(uri, "POST");

            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(data));
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Cookie", $"Session={Session}");
            return request.SendWebRequest();
        }

        /// <summary>
        /// 上传资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void UploadResource(ResourceType type, string id, byte[] data)
        {
            UnityWebRequestAsyncOperation op = requestPost(type, id, data);
            while (!op.isDone) ;

            parseResponse(op);
        }

        /// <summary>
        /// 上传资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task UploadResourceAsync(ResourceType type, string id, byte[] data)
        {
            var completeSource = new TaskCompletionSource<object>();

            UnityWebRequestAsyncOperation op = requestPost(type, id, data);
            op.completed += (_) =>
            {
                try
                {
                    completeSource.SetResult(parseResponse(op));
                }
                catch (Exception e)
                {
                    completeSource.SetException(e);
                }
            };

            return completeSource.Task;
        }

        /// <summary>
        /// 新建检查资源的请求
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private UnityWebRequestAsyncOperation requestHead(ResourceType type, string id)
        {
            var req = UnityWebRequest.Head(resourceUri(type, id));
            req.SetRequestHeader("Cookie", $"Session={Session}");
            return req.SendWebRequest();
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ResourceExists(ResourceType type, string id)
        {
            var op = requestHead(type, id);
            while (!op.isDone) ;

            return parseHeadResponse(op);
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> ResourceExistsAsync(ResourceType type, string id)
        {
            var completeSource = new TaskCompletionSource<bool>();

            UnityWebRequestAsyncOperation op = requestHead(type, id);
            op.completed += (_) =>
            {
                try
                {
                    completeSource.SetResult(parseHeadResponse(op));
                }
                catch (Exception e)
                {
                    completeSource.SetException(e);
                }
            };

            return completeSource.Task;
        }

        /// <summary>
        /// 批量检查资源是否存在
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public bool[] ResourceExistsBatch(Tuple<ResourceType, string>[] res)
        {
            var obj = new object[res.Length];
            for (int i = 0; i < res.Length; i++)
            {
                obj[i] = new
                {
                    Type = res[i].Item1.GetString(),
                    ID = res[i].Item2
                };
            }
            var op = postJson(getUri("exists"), obj.ToJson());
            while (!op.isDone) ;

            var bytes = parseResponse(op);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return BsonSerializer.Deserialize<bool[]>(json);
        }

        /// <summary>
        /// 批量检查资源是否存在
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public Task<bool[]> ResourceExistsBatchAsync(Tuple<ResourceType, string>[] res)
        {
            var completeSource = new TaskCompletionSource<bool[]>();

            var obj = new object[res.Length];
            for (int i = 0; i < res.Length; i++)
            {
                obj[i] = new
                {
                    Type = res[i].Item1.GetString(),
                    ID = res[i].Item2
                };
            }
            var op = postJson(getUri("exists"), obj.ToJson());
            op.completed += (_) =>
            {
                try
                {
                    var bytes = parseResponse(op);
                    var json = System.Text.Encoding.UTF8.GetString(bytes);
                    completeSource.SetResult(BsonSerializer.Deserialize<bool[]>(json));
                }
                catch (Exception e)
                {
                    completeSource.SetException(e);
                }
            };

            return completeSource.Task;
        }

        /// <summary>
        /// 解析资源是否存在
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static bool parseHeadResponse(UnityWebRequestAsyncOperation op)
        {
            if (op.webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                throw new Exception(op.webRequest.error);
            }
            if (op.webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                if (op.webRequest.responseCode == 404) return false;
                throw new Exception(op.webRequest.error);
            }
            return true;
        }

        Uri getUri(string uri)
        {
            if (BaseUrl == null) throw new Exception("Resource base URL is not set.");
            return new Uri(BaseUrl + "/" + uri);
        }

        Uri resourceUri(ResourceType type, string id)
        {
            return getUri($"{type.GetString()}/{id}");
        }

        /// <summary>
        /// 解析通用响应
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private static byte[] parseResponse(UnityWebRequestAsyncOperation op)
        {
            if (op.webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                throw new Exception(op.webRequest.error);
            }
            if (op.webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                if (op.webRequest.responseCode == 404)
                {
                    throw new FileNotFoundException();
                }
                else
                {
                    throw new Exception(op.webRequest.error);
                }
            }
            return op.webRequest.downloadHandler.data;
        }

        private static byte[] parseFileResponse(UnityWebRequestAsyncOperation op)
        {
            var data = parseResponse(op);
            return Compression.tryDecompress(data);
        }

    }
}