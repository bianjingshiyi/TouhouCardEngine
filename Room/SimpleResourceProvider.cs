using System.IO;

namespace TouhouCardEngine
{
    /// <summary>
    /// 资源服务器的资源提供者
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// 打开一个资源，用于写入
        /// </summary>
        /// <param name="resType"></param>
        /// <param name="resID"></param>
        /// <returns></returns>
        Stream OpenWriteResource(string resType, string resID);

        /// <summary>
        /// 打开一个资源，用于读出
        /// </summary>
        /// <param name="resType"></param>
        /// <param name="resID"></param>
        /// <returns></returns>
        Stream OpenReadResource(string resType, string resID);

        /// <summary>
        /// 获取给定资源的信息
        /// </summary>
        /// <param name="resType"></param>
        /// <param name="resID"></param>
        /// <param name="size">若存在，则资源长度为</param>
        /// <returns></returns>
        bool ResourceInfo(string resType, string resID, out long size);
    }

    /// <summary>
    /// 资源服务器的底层逻辑
    /// </summary>
    public class SimpleResourceProvider : IResourceProvider
    {
        const string RES_DEFINE = "card";
        const string RES_PICTURE = "pic";

        /// <summary>
        /// 文件存储位置
        /// </summary>
        string StorageDir { get; }

        public SimpleResourceProvider(string storageDir)
        {
            StorageDir = storageDir;
        }

        /// <summary>
        /// 保存给定的资源
        /// </summary>
        /// <param name="uid">玩家ID</param>
        /// <param name="resType">资源类型</param>
        /// <param name="resID">资源ID</param>
        /// <param name="file">文件流</param>
        /// <exception cref="FileNotFoundException"></exception>
        public virtual Stream OpenWriteResource(string resType, string resID)
        {
            // 检查资源类型
            if (!ResourceIsValid(resType))
                throw new FileNotFoundException();

            var folder = Path.Combine(StorageDir, resType);
            var path = Path.Combine(folder, resID);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        }

        /// <summary>
        /// 读取给定的资源
        /// </summary>
        /// <param name="uid">玩家ID</param>
        /// <param name="resType">资源类型</param>
        /// <param name="resID">资源ID</param>
        /// <returns>文件流</returns>
        /// <exception cref="FileNotFoundException">资源不存在</exception>
        public virtual Stream OpenReadResource(string resType, string resID)
        {
            // 资源类型
            if (!ResourceIsValid(resType))
                throw new FileNotFoundException();

            var path = resourcePath(resType, resID);
            if (!File.Exists(path))
                throw new FileNotFoundException();

            return File.OpenRead(path);
        }

        /// <summary>
        /// 检查给定的资源类型是否正确
        /// </summary>
        /// <param name="resType"></param>
        /// <returns></returns>
        protected bool ResourceIsValid(string resType)
        {
            switch (resType)
            {
                case RES_DEFINE:
                case RES_PICTURE:
                    return true;
                default:
                    return false;
            }
        }

        protected string resourcePath(string resType, string resID)
        {
            return Path.Combine(StorageDir, resType, resID);
        }

        /// <summary>
        /// 检查给定资源是否存在
        /// </summary>
        /// <param name="resType"></param>
        /// <param name="resID"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual bool ResourceInfo(string resType, string resID, out long size)
        {
            size = 0;
            var path = resourcePath(resType, resID);

            if (!ResourceIsValid(resType) || !File.Exists(path))
                return false;

            var info = new FileInfo(path);
            size = info.Length;
            return true;
        }
    }
}