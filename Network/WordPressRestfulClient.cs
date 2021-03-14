using System.Net;
using RestSharp;
using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NitoriNetwork.Common
{
    /// <summary>
    /// Wordpress 内容服务
    /// </summary>
    public class WordPressRestfulClient
    {
        RestClient client { get; }

        /// <summary>
        /// Wordpress 内容服务
        /// </summary>
        /// <param name="baseURL"></param>
        public WordPressRestfulClient(string baseURL)
        {
            client = new RestClient(baseURL);
            client.ThrowOnDeserializationError = true;
            client.UseSerializer(
                () => new MongoDBJsonSerializer()
            );
        }

        /// <summary>
        /// 获取指定分类的文章
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public WordPressPost[] GetPosts(int[] categories = null, int count = 10, int offset = 0)
        {
            RestRequest request = new RestRequest("/wp-json/wp/v2/posts", Method.GET);
            if (categories != null)
            {
                request.AddParameter("categories", String.Join(",", categories.Select(p => p.ToString()).ToArray()));
            }
            request.AddParameter("offset", offset);
            request.AddParameter("count", count);

            var result = client.Execute<WordPressPost[]>(request);
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(result.StatusDescription);
            }
            return result.Data;
        }

        /// <summary>
        /// 获取指定分类的文章
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public async Task<WordPressPost[]> GetPostsAsync(int[] categories = null, int count = 10, int offset = 0)
        {
            RestRequest request = new RestRequest("/wp-json/wp/v2/posts", Method.GET);
            if (categories != null)
            {
                request.AddParameter("categories", String.Join(",", categories.Select(p => p.ToString()).ToArray()));
            }
            request.AddParameter("offset", offset);
            request.AddParameter("count", count);

            var result = await client.ExecuteAsync<WordPressPost[]>(request);
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(result.StatusDescription);
            }
            return result.Data;
        }

        /// <summary>
        /// 获取指定ID的媒体信息
        /// </summary>
        /// <param name="mediaID"></param>
        /// <returns></returns>
        public WordPressMedia GetMediaInfo(int mediaID)
        {
            RestRequest request = new RestRequest("/wp-json/wp/v2/media/" + mediaID, Method.GET);
            var result = client.Execute<WordPressMedia>(request);
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(result.StatusDescription);
            }
            return result.Data;
        }

        /// <summary>
        /// 获取指定ID的媒体信息
        /// </summary>
        /// <param name="mediaID"></param>
        /// <returns></returns>
        public async Task<WordPressMedia> GetMediaInfoAsync(int mediaID)
        {
            RestRequest request = new RestRequest("/wp-json/wp/v2/media/" + mediaID, Method.GET);
            var result = await client.ExecuteAsync<WordPressMedia>(request);
            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(result.StatusDescription);
            }
            return result.Data;
        }

        /// <summary>
        /// WordPress 通用的一些信息
        /// </summary>
        [BsonIgnoreExtraElements]
        [Serializable]
        public class WordPressCommonInfo
        {
            /// <summary>
            /// ID
            /// </summary>
            [BsonElement("id")]
            public int id;

            /// <summary>
            /// 无用，防止冲突
            /// </summary>
            [BsonId]
            public string bsonID;

            /// <summary>
            /// 发布日期
            /// </summary>
            public DateTime? date;
            /// <summary>
            /// 发布日期（GMT）
            /// </summary>
            public DateTime? date_gmt;
            /// <summary>
            /// 修改日期
            /// </summary>
            public DateTime? modified;
            /// <summary>
            /// 修改日期（GMT）
            /// </summary>
            public DateTime? modified_gmt;
            /// <summary>
            /// 自定义后缀
            /// </summary>
            public string slug;
            /// <summary>
            /// 状态
            /// </summary>
            /// <remarks>
            /// 文章可以是 publish, future, draft, pending, private
            /// </remarks>
            public string status;
            /// <summary>
            /// 类型
            /// </summary>
            public string type;
            /// <summary>
            /// 标题
            /// </summary>
            public WordPressHTMLContent title;
            /// <summary>
            /// 页面链接
            /// </summary>
            public string link;
            /// <summary>
            /// 作者ID
            /// </summary>
            public int author;
        }

        /// <summary>
        /// WordPress文章信息
        /// </summary>
        [BsonIgnoreExtraElements]
        public class WordPressPost: WordPressCommonInfo
        {
            /// <summary>
            /// 文章内容
            /// </summary>
            public WordPressHTMLContent content;
            /// <summary>
            /// 文章摘要
            /// </summary>
            public WordPressHTMLContent excerpt;
            /// <summary>
            /// 文章分类ID
            /// </summary>
            public int[] categories;
            /// <summary>
            /// 文章标签ID
            /// </summary>
            public int[] tags;
            /// <summary>
            /// 文章特色图像
            /// </summary>
            public int featured_media;
        }

        /// <summary>
        /// WordPress 媒体信息
        /// </summary>
        [BsonIgnoreExtraElements]
        public class WordPressMedia
        {
            /// <summary>
            /// 图像描述
            /// </summary>
            public WordPressHTMLContent description;
            /// <summary>
            /// 图像代替文字
            /// </summary>
            public WordPressHTMLContent caption;
            /// <summary>
            /// 代替文字
            /// </summary>
            public string alt_text;
            /// <summary>
            /// 媒体类型
            /// </summary>
            public string media_type;
            /// <summary>
            /// MIME类型
            /// </summary>
            public string mime_type;
            /// <summary>
            /// 详细信息
            /// </summary>
            public WordPressMediaDetails media_details;
            /// <summary>
            /// 源地址
            /// </summary>
            public string source_url;
        }

        /// <summary>
        /// WordPress 内容
        /// </summary>
        [BsonIgnoreExtraElements]
        public class WordPressHTMLContent
        {
            /// <summary>
            /// 渲染后的HTML内容
            /// </summary>
            public string rendered;
        }

        /// <summary>
        /// WordPress媒体文件信息
        /// </summary>
        [BsonIgnoreExtraElements]
        public class WordPressMediaDetails
        {
            public int width;
            public int height;
            /// <summary>
            /// 文件名
            /// </summary>
            public string file;
            /// <summary>
            /// 各个大小的版本
            /// </summary>
            public Dictionary<string, WordPressMediaSize> sizes;
        }

        /// <summary>
        /// 媒体文件大小
        /// </summary>
        [BsonIgnoreExtraElements]
        public class WordPressMediaSize
        {
            public int width;
            public int height;
            public string file;
            public string mime_type;
            public string source_url;
        }
    }
}
