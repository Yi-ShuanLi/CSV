using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSV.Writers
{
    internal class AddHeader : CSVWriter
    {
        private static readonly object key = new object();
        public override void WriteData<T>(string filePath, List<T> objs)
        {
            var props = typeof(T).GetProperties();
            string columnName = String.Join(",", props.Select(x => x.Name)).ToString();
            StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8);
            writer.WriteLine(columnName);
            foreach (T obj in objs)
            {
                string dataLine = String.Join(",", props.Select(x => x.GetValue(obj).ToString()));
                writer.WriteLine(dataLine);
                writer.Flush();
            }
            writer.Close();
        }



        public override async Task WriteDataAsync<T>(string filePath, List<T> objs, CancellationToken token)
        {
            await Task.Run(() =>
            {

                lock (key)
                {
                    var props = typeof(T).GetProperties();
                    string columnName = String.Join(",", props.Select(x => x.Name)).ToString();
                    StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8);
                    if (token.IsCancellationRequested)
                    {
                        writer.Close();
                        // 發現要取消了，趕快收工
                        Debug.WriteLine("收到指令，我不做了！ (Task Cancelled!)");
                        return;
                    }
                    writer.WriteLine(columnName);
                    foreach (T obj in objs)
                    {
                        string dataLine = String.Join(",", props.Select(x => x.GetValue(obj).ToString()));
                        writer.WriteLine(dataLine);
                        writer.Flush();
                    }
                    writer.Close();
                }

            });

        }
    }
}
