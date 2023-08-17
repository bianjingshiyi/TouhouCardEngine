using System;
using System.IO;
using System.IO.Compression;

namespace TouhouCardEngine
{
    public static class Compression
    {
        public static byte[] compress(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    gz.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 尝试解压内容。如果不是GZip压缩则不会解压
        /// </summary>
        /// <param name="data"></param>
        /// <param name="maxSize">最大大小</param>
        /// <returns></returns>
        /// <exception cref="OutOfMemoryException">超过最大大小</exception>
        public static byte[] tryDecompress(byte[] data, uint maxSize = 10 * 1024 * 1024)
        {
            // 不是GZip压缩文件，直接返回
            if (data.Length < 2 || data[0] != 0x1f || data[1] != 0x8b)
                return data;

            uint fileSize = BitConverter.ToUInt32(data, data.Length - 4);
            if (fileSize >= maxSize)
                throw new OutOfMemoryException("Data to be decompress is too long");

            using (var originalStream = new MemoryStream(data))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    using (var decompressionStream = new GZipStream(originalStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedStream);
                    }
                    return decompressedStream.ToArray();
                }
            }
        }
    }
}
