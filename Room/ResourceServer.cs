using System.Net;
using TouhouCardEngine.Shared;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.IO;
using System;
using System.Linq;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace TouhouCardEngine
{
    /// <summary>
    /// 小型资源服务器
    /// </summary>
    /// <remarks>
    /// 此服务器仅应当在局域网下使用。对于大型转发服务器，请使用ASP.NET
    /// </remarks>
    public class ResourceServerLite
    {
        ILogger logger { get; }

        HttpListener http { get; } = new HttpListener();

        IResourceProvider provider { get; }

        HashSet<IPAddress> allowedAddresses { get; } = new HashSet<IPAddress>();

        public ResourceServerLite(ILogger logger, IResourceProvider provider)
        {
            this.logger = logger;
            this.provider = provider;
        }

        /// <summary>
        /// 启动HTTP服务器
        /// </summary>
        /// <param name="port"></param>
        /// <param name="addrs">要监听的地址。若为空，则默认监听所有本地地址</param>
        /// <returns>是否启动成功</returns>
        public bool Start(int port, string[] addrs = null)
        {
            if (addrs == null || addrs.Length == 0)
            {
                addrs = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).Select(i => i.ToString()).ToArray();
            }

            http.Prefixes.Add($"http://localhost:{port}/");
            http.Prefixes.Add($"http://127.0.0.1:{port}/");
            foreach (var addr in addrs)
            {
                http.Prefixes.Add($"http://{addr}:{port}/");
            }

            logger.log($"Start HTTP server at {port}");
            try
            {
                http.Start();
            }
            catch (Exception e)
            {
                logger.logError($"HTTP start error: {e.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 停止 HTTP 服务器
        /// </summary>
        public void Stop()
        {
            http.Stop();
            http.Prefixes.Clear();
        }

        /// <summary>
        /// 执行 HTTP 事务。
        /// 会阻塞，需要在外面嵌套循环执行。
        /// </summary>
        public void Routine()
        {
            HttpListenerContext context = http.GetContext();
            requestHandler(context);
        }

        /// <summary>
        /// 设置允许连接的IP地址
        /// </summary>
        /// <param name="addresses"></param>
        public void SetAllowedIPs(IPAddress[] addresses)
        {
            allowedAddresses.Clear();
            foreach (var item in addresses)
                allowedAddresses.Add(item);
        }

        void requestHandler(HttpListenerContext ctx)
        {
            // 鉴权。
            // 对于转发服务器，鉴权信息使用Cookie；对于本地服务器，鉴权信息使用IP地址。应判断
            // 1. 当前玩家是否存在
            // 2. 玩家上传的资源是否在房间允许的卡组列表内

            //var ep = ctx.Request.RemoteEndPoint;
            //if (allowedAddresses.Count != 0)
            //{
            //    if (!allowedAddresses.Contains(ep.Address))
            //    {
            //        Response(ctx.Response, HttpStatusCode.Forbidden);
            //        logger.log($"{ep.Address} is not in allow list, connection reject.");
            //        return;
            //    }
            //}
            //else
            //{
            //    logger.logWarn("IP allow list is empty, filter disabled.");
            //}

            // 解析URL
            var regex = new Regex("/([0-9a-zA-Z-_]+)/([0-9a-zA-Z-_]+)");
            var uri = ctx.Request.Url.LocalPath;
            // 批量获取资源存在性
            if (uri.EndsWith("/exists") && ctx.Request.HttpMethod == "POST")
            {
                try
                {
                    var resItems = BsonSerializer.Deserialize<ResourceItem[]>(ctx.Request.InputStream);
                    var results = new bool[resItems.Length];

                    for (int i = 0; i < resItems.Length; i++)
                    {
                        results[i] = provider.ResourceInfo(resItems[i].Type, resItems[i].ID, out _);
                    }
                    Response(ctx.Response, results.ToJson(), "application/json");
                }
                catch (Exception e)
                {
                    logger.logError(e.ToString());
                    Response(ctx.Response, HttpStatusCode.InternalServerError);
                }
                return;
            }

            var matches = regex.Match(uri);
            if (!matches.Success)
            {
                Response(ctx.Response, HttpStatusCode.NotFound);
                return;
            }

            var resType = matches.Groups[1].Value;
            var resID = matches.Groups[2].Value;

            // 限制上传大小
            if (ctx.Request.ContentLength64 > 1024576)
            {
                Response(ctx.Response, HttpStatusCode.RequestEntityTooLarge);
                return;
            }

            logger.log($"Requesting {ctx.Request.HttpMethod} resource {resType} {resID}");

            try
            {
                switch (ctx.Request.HttpMethod)
                {
                    case "GET": // 获取玩家资源
                        using (var fs = provider.OpenReadResource(resType, resID))
                        {
                            Response(ctx.Response, fs, "application/binary");
                        }
                        break;

                    case "HEAD": // 获取玩家资源基本信息
                        var exists = provider.ResourceInfo(resType, resID, out long length);
                        if (!exists)
                        {
                            Response(ctx.Response, HttpStatusCode.NotFound);
                            return;
                        }
                        Response(ctx.Response, length);
                        break;

                    case "POST": // 上传玩家资源
                        if (!ctx.Request.ContentType.StartsWith("multipart/form-data"))
                        {
                            Response(ctx.Response, "Only multipart/form-data is supported!", code: HttpStatusCode.BadRequest);
                            return;
                        }

                        var boundary = GetBoundary(ctx.Request.ContentType);
                        using (var fs = provider.OpenWriteResource(resType, resID, ctx.Request.ContentLength64))
                        {
                            SaveFile(ctx.Request.ContentEncoding, boundary, ctx.Request.InputStream, fs);
                        }
                        Response(ctx.Response, HttpStatusCode.OK);
                        break;

                    default:
                        Response(ctx.Response, HttpStatusCode.MethodNotAllowed);
                        return;
                }
            }
            catch (FileNotFoundException)
            {
                Response(ctx.Response, HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                logger.logError(e.ToString());
                Response(ctx.Response, HttpStatusCode.InternalServerError);
            }
        }

        public static void Response(HttpListenerResponse resp, HttpStatusCode code)
        {
            Response(resp, $"{(int)code} {code}", code: code);
        }

        public static void Response(HttpListenerResponse resp, long contentLength, string contentType = "text/html")
        {
            resp.ContentLength64 = contentLength;
            resp.ContentType = contentType;
            resp.StatusCode = (int)HttpStatusCode.OK;
            resp.OutputStream.Close();
        }

        public static void Response(HttpListenerResponse resp, string text, string contentType = "text/html", HttpStatusCode code = HttpStatusCode.OK)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            resp.ContentLength64 = buffer.Length;
            resp.ContentType = contentType;
            resp.StatusCode = (int)code;
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            resp.OutputStream.Close();
        }

        public static void Response(HttpListenerResponse resp, Stream fs, string contentType = "text/html", HttpStatusCode code = HttpStatusCode.OK)
        {
            resp.ContentLength64 = fs.Length;
            resp.ContentType = contentType;
            resp.StatusCode = (int)code;
            fs.CopyTo(resp.OutputStream);
            resp.OutputStream.Close();
        }

        private static String GetBoundary(String ctype)
        {
            return "--" + ctype.Split(';')[1].Split('=')[1];
        }

        /// <summary>
        /// Parse form/multipart data
        /// </summary>
        /// <param name="enc"></param>
        /// <param name="boundary"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <exception cref="Exception"></exception>
        /// <see cref="https://stackoverflow.com/questions/8466703/httplistener-and-file-upload"/>
        private static void SaveFile(Encoding enc, String boundary, Stream input, Stream output)
        {
            Byte[] boundaryBytes = enc.GetBytes(boundary);
            Int32 boundaryLen = boundaryBytes.Length;

            Byte[] buffer = new Byte[1024];
            Int32 len = input.Read(buffer, 0, 1024);
            Int32 startPos = -1;

            // Find start boundary
            while (true)
            {
                if (len == 0)
                {
                    throw new Exception("Start Boundaray Not Found");
                }

                startPos = IndexOf(buffer, len, boundaryBytes);
                if (startPos >= 0)
                {
                    break;
                }
                else
                {
                    Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                    len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
                }
            }

            // Skip four lines (Boundary, Content-Disposition, Content-Type, and a blank)
            for (Int32 i = 0; i < 4; i++)
            {
                while (true)
                {
                    if (len == 0)
                    {
                        throw new Exception("Preamble not Found.");
                    }

                    startPos = Array.IndexOf(buffer, enc.GetBytes("\n")[0], startPos);
                    if (startPos >= 0)
                    {
                        startPos++;
                        break;
                    }
                    else
                    {
                        len = input.Read(buffer, 0, 1024);
                    }
                }
            }

            Array.Copy(buffer, startPos, buffer, 0, len - startPos);
            len = len - startPos;

            while (true)
            {
                Int32 endPos = IndexOf(buffer, len, boundaryBytes);
                if (endPos >= 0)
                {
                    if (endPos > 0) output.Write(buffer, 0, endPos - 2);
                    break;
                }
                else if (len <= boundaryLen)
                {
                    throw new Exception("End Boundaray Not Found");
                }
                else
                {
                    output.Write(buffer, 0, len - boundaryLen);
                    Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                    len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen;
                }
            }
        }

        private static Int32 IndexOf(Byte[] buffer, Int32 len, Byte[] boundaryBytes)
        {
            for (Int32 i = 0; i <= len - boundaryBytes.Length; i++)
            {
                Boolean match = true;
                for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
                {
                    match = buffer[i + j] == boundaryBytes[j];
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// 资源服务器的项目
    /// </summary>
    struct ResourceItem
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public string Type;
        /// <summary>
        /// 资源ID
        /// </summary>
        public string ID;
    }
}