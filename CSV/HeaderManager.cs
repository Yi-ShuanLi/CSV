using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSV
{
    internal class HeaderManager
    {
        /// <summary>
        /// 檢查是否需要添加Header。
        /// 情況如下:
        /// 1. 如果檔案本身不存在則自動返回 <c>CreateNewFile</c> 
        /// 2. 如果檔案存在，但沒有任何檔案，即第一行為空或是null 則返回 <c>AddHeader</c>
        /// 3. 如果第一行不為空，但與泛型欄位不相符則返回 <c>AddHeaderOverwrite</c>
        /// 4. 如果該檔案本身有Header也有內容，則返回 <c>AppendData</c>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static HeaderType CheckHeader<T>(string filePath)
        {

            var props = typeof(T).GetProperties();
            string columnName = String.Join(",", props.Select(x => x.Name)).ToString();

            if (!File.Exists(filePath))
                return HeaderType.CreateNewFile;

            StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
            string firstLine = reader.ReadLine();

            if (firstLine == null)
            {
                reader.Close();
                return HeaderType.AddHeader;
            }
            if (firstLine != columnName)
            {
                reader.Close();
                return HeaderType.AddHeaderOverwrite;
            }
            reader.Close();
            return HeaderType.AppendData;
        }
        private static readonly object key = new object();
        /// <summary>
        /// 檢查是否需要添加Header。
        /// 情況如下:
        /// 1. 如果檔案本身不存在則自動返回 <c>CreateNewFile</c> 
        /// 2. 如果檔案存在，但沒有任何檔案，即第一行為空或是null 則返回 <c>AddHeader</c>
        /// 3. 如果第一行不為空，但與泛型欄位不相符則返回 <c>AddHeaderOverwrite</c>
        /// 4. 如果該檔案本身有Header也有內容，則返回 <c>AppendData</c>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Task<HeaderType> CheckHeaderAsync<T>(string filePath, CancellationToken token)
        {
            return Task.Run(() =>
            {
                lock (key)
                {
                    var props = typeof(T).GetProperties();
                    string columnName = String.Join(",", props.Select(x => x.Name)).ToString();

                    if (!File.Exists(filePath))
                        return HeaderType.CreateNewFile;

                    StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
                    string firstLine = reader.ReadLine();
                    if (firstLine == null)
                    {
                        reader.Close();
                        return HeaderType.AddHeader;
                    }
                    if (firstLine != columnName)
                    {
                        reader.Close();
                        return HeaderType.AddHeaderOverwrite;
                    }
                    if (token.IsCancellationRequested)
                    {
                        // 發現要取消了，趕快收工
                        Debug.WriteLine("收到指令，我不做了！ (Task Cancelled!)");
                        reader.Close();
                        return HeaderType.AppendData;
                    }
                    reader.Close();
                    return HeaderType.AppendData;
                }

            });
            //var props = typeof(T).GetProperties();
            //string columnName = String.Join(",", props.Select(x => x.Name)).ToString();

            //if (!File.Exists(filePath))
            //    return HeaderType.CreateNewFile;

            //StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
            //string firstLine = reader.ReadLine();

            //if (firstLine == null)
            //{
            //    reader.Close();
            //    return HeaderType.AddHeader;
            //}
            //if (firstLine != columnName)
            //{
            //    reader.Close();
            //    return HeaderType.AddHeaderOverwrite;
            //}
            //reader.Close();
            //return HeaderType.AppendData;
        }
    }
}
