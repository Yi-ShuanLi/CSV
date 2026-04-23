using CSV.Writers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSV
{
    public class CSVHelper
    {
        static PropertyInfo[] propsWrite = null;
        //給方法取用的參數入口，目標物件，值
        delegate object GetterDelegte(object sourceItem);
        //propInfo從這帶入，最後包成SetterDelegte結果
        static GetterDelegte[] getter = null;
        static GetterDelegte CreateGetter(PropertyInfo propertyInfo)
        {
            var sourceItemParm = Expression.Parameter(typeof(object));
            //目標物件的欄位，與物件mapping轉型 => SetterDelegte.id，Func包成Expression
            Expression sourcePropInfoExpression = Expression.Convert(sourceItemParm, propertyInfo.DeclaringType);
            //建立MethodCallExpression
            MethodCallExpression methodCall = Expression.Call(sourcePropInfoExpression, propertyInfo.GetGetMethod());
            //把MethodCallExpression，包起來Expression，最後return SetterDelegte 回去變成SetterDelegte array的方法陣列
            GetterDelegte delegte = Expression.Lambda<GetterDelegte>(methodCall, sourceItemParm).Compile();
            return delegte;
        }

        static PropertyInfo[] propsRead = null;
        //給方法取用的參數入口，目標物件，值
        delegate void SetterDelegte(object data, object value);
        //propInfo從這帶入，最後包成SetterDelegte結果
        static SetterDelegte[] setters = null;
        static SetterDelegte CreateSetter(PropertyInfo propertyInfo)
        {
            var targetParm = Expression.Parameter(typeof(object));
            var valueParm = Expression.Parameter(typeof(object));
            //目標物件的欄位，與物件mapping轉型 => SetterDelegte.id，Func包成Expression
            Expression castTarget = Expression.Convert(targetParm, propertyInfo.DeclaringType);
            //欄位的資料型態，string => 
            Expression castValue = Expression.Convert(valueParm, propertyInfo.PropertyType);
            //建立MethodCallExpression
            MethodCallExpression methodCall = Expression.Call(castTarget, propertyInfo.GetSetMethod(), castValue);
            //把MethodCallExpression，包起來Expression，最後return SetterDelegte 回去變成SetterDelegte array的方法陣列
            SetterDelegte delegte = Expression.Lambda<SetterDelegte>(methodCall, targetParm, valueParm).Compile();
            return delegte;
        }

        public static void OptimizeWrite<T>(string filePath, List<T> datas)
        {

            StringBuilder sb = new StringBuilder(90);
            char[] buffer = new char[90];
            if (propsWrite == null || getter == null)
            {
                propsWrite = typeof(T).GetProperties();
                getter = propsWrite.Select(x => CreateGetter(x)).ToArray();
            }
            StreamWriter writer = new StreamWriter(filePath, true);
            for (int j = 0; j < datas.Count; j++)
            {
                for (int i = 0; i < propsWrite.Length; i++)
                {
                    sb.Append(getter[i](datas[j]).ToString());
                    if (i < propsWrite.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.CopyTo(0, buffer, 0, sb.Length);
                writer.WriteLine(buffer, 0, sb.Length);
                sb.Clear();
            }
            writer.Flush();
            writer.Close();
        }

        public static List<T> OptimizeRead<T>(string filePath, int startLine, int count) where T : class, new()
        {
            List<T> datas = new List<T>();
            if (propsRead == null || setters == null)
            {
                propsRead = typeof(T).GetProperties();
                setters = propsRead.Select(x => CreateSetter(x)).ToArray();
            }
            StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
            int currentIndex = 0;
            while (!reader.EndOfStream)
            {
                currentIndex++;
                if (currentIndex >= startLine + count)
                    break;
                string line = reader.ReadLine();
                if (currentIndex < startLine)
                    continue;

                ReadOnlySpan<char> strings = line.AsSpan();

                int current = 0;
                int field = 0;
                T dest = new T();
                while (true)
                {
                    int num = strings.Slice(current).IndexOf(',');
                    if (num == -1)
                    {
                        setters[field++](dest, strings.Slice(current).ToString());
                        break;
                    }
                    else
                    {
                        setters[field++](dest, strings.Slice(current, num).ToString());
                        current += num + 1;
                    }
                }
                datas.Add(dest);
            }
            return datas;
        }


        public static void Write<T>(string filePath, List<T> objs)
        {
            string[] fileExtension = filePath.Split('.');
            if (fileExtension[1] != "csv")
                throw new Exception("必須是csv檔案才能進行讀取");
            string[] fileExtension2 = fileExtension[0].Split('\\');
            string newFilePath = String.Join("\\", fileExtension2.Take(fileExtension2.Length - 1)).ToString();
            if (!Directory.Exists(newFilePath))
                Directory.CreateDirectory(newFilePath);
            //Header寫入的時機:
            //1.檔案不存在的時候或者檔案存在但沒任何一筆資料的時候，寫入header+資料
            //2.有Header的情況不寫入，直接寫入資料
            //3.沒有Header但是有檔案的時候，先把資料全部讀出來(舊資料)，啟動複寫模式，寫入header+舊資料 最後在寫入新資料
            HeaderType headerType = HeaderManager.CheckHeader<T>(filePath);
            Type type = Type.GetType($"CSV.Writers.{headerType}");
            CSVWriter writer2 = (CSVWriter)Activator.CreateInstance(type);
            writer2.WriteData<T>(filePath, objs);
        }
        public static async Task WriteAsync<T>(string filePath, List<T> objs, CancellationToken token)
        {
            string[] fileExtension = filePath.Split('.');
            if (fileExtension[1] != "csv")
                throw new Exception("必須是csv檔案才能進行讀取");
            string[] fileExtension2 = fileExtension[0].Split('\\');
            string newFilePath = String.Join("\\", fileExtension2.Take(fileExtension2.Length - 1)).ToString();
            if (!Directory.Exists(newFilePath))
                Directory.CreateDirectory(newFilePath);
            //Header寫入的時機:
            //1.檔案不存在的時候或者檔案存在但沒任何一筆資料的時候，寫入header+資料
            //2.有Header的情況不寫入，直接寫入資料
            //3.沒有Header但是有檔案的時候，先把資料全部讀出來(舊資料)，啟動複寫模式，寫入header+舊資料 最後在寫入新資料
            await Task.Run(async () =>
            {
                //HeaderType headerType = await HeaderManager.CheckHeaderAsync<T>(filePath, token);
                Type type = Type.GetType($"CSV.Writers.AppendData");
                CSVWriter writer2 = (CSVWriter)Activator.CreateInstance(type);
                // 4. 機器人每做一步，就檢查一下對講機有沒有收到取消訊號
                if (token.IsCancellationRequested)
                {
                    // 發現要取消了，趕快收工
                    Debug.WriteLine("收到指令，我不做了！ (Task Cancelled!)");
                    return;
                }
                await writer2.WriteDataAsync<T>(filePath, objs, token);
            });
        }
        public static void Write<T>(string filePath, T obj)
        {
            List<T> objs = new List<T>
            {
                obj
            };
            Write(filePath, objs);
        }
        public static async Task WriteAsync<T>(string filePath, T obj, CancellationToken token)
        {
            List<T> objs = new List<T>
            {
                obj
            };
            await Task.Run(async () =>
            {
                await WriteAsync(filePath, objs, token);
            });
            return;
        }



        //0~100
        //101~200(101+100)
        //201~300(201+100)
        public static List<T> Read<T>(string filePath, int startLine, int count) where T : class, new()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException();
            string[] fileExtension = filePath.Split('.');
            if (fileExtension[1] != "csv")
                throw new Exception("必須是csv檔案才能進行讀取");


            StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
            List<T> datas = new List<T>();
            var props = typeof(T).GetProperties();
            var headers = reader.ReadLine().Split(',').ToList();
            Dictionary<string, int> keyValuePairs = props.Select(x =>
            {
                int index = headers.FindIndex(y => y == x.Name);
                if (index != -1)
                {
                    return new
                    {
                        Name = x.Name,
                        Index = index
                    };
                }
                return null;
            }).Where(x => x != null).ToDictionary(x => x.Name, x => x.Index);

            //for (int i = 0; i < column.Length; i++)
            //{
            //    var propInfo = typeof(T).GetProperty(column[i]);
            //    if (propInfo != null)
            //    {
            //        keyValuePairs.Add(propInfo.Name, i);
            //    }
            //    //for (int j = 0; j < column.Length; j++)
            //    //{
            //    //    if (props[i].Name.Equals(column[j]))
            //    //    {
            //    //        keyValuePairs.Add(props[i].Name, j);
            //    //        break;
            //    //    }
            //    //}
            //}
            int currentIndex = 0;
            while (!reader.EndOfStream)
            {
                currentIndex++;
                if (currentIndex >= startLine + count)
                    break;
                string line = reader.ReadLine();
                if (currentIndex < startLine)
                    continue;
                string[] fields = line.Split(','); // 5
                T t = new T(); // 3
                for (int i = 0; i < props.Length; i++)
                {
                    if (keyValuePairs.TryGetValue(props[i].Name, out int index))
                        props[i].SetValue(t, fields[index]);
                }
                datas.Add(t);
            }

            reader.Close();
            return datas;
        }

        public static Task<List<T>> ReadAsync<T>(string filePath, int startLine, int count, CancellationToken token) where T : class, new()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException();
            string[] fileExtension = filePath.Split('.');
            if (fileExtension[1] != "csv")
                throw new Exception("必須是csv檔案才能進行讀取");

            //StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
            //var props = typeof(T).GetProperties();
            //var headers = reader.ReadLine().Split(',').ToList();
            //Dictionary<string, int> keyValuePairs = props.Select(x =>
            //{
            //    int index = headers.FindIndex(y => y == x.Name);
            //    if (index != -1)
            //    {
            //        return new
            //        {
            //            Name = x.Name,
            //            Index = index
            //        };
            //    }
            //    return null;
            //}).Where(x => x != null).ToDictionary(x => x.Name, x => x.Index);
            Task<List<T>> datas = Task.Run(() =>
            {
                int currentIndex = 0;
                List<T> datasList = new List<T>();
                StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
                var props = typeof(T).GetProperties();
                var headers = reader.ReadLine().Split(',').ToList();
                Dictionary<string, int> keyValuePairs = props.Select(x =>
                {
                    int index = headers.FindIndex(y => y == x.Name);
                    if (index != -1)
                    {
                        return new
                        {
                            Name = x.Name,
                            Index = index
                        };
                    }
                    return null;
                }).Where(x => x != null).ToDictionary(x => x.Name, x => x.Index);
                while (!reader.EndOfStream)
                {
                    // 4. 機器人每做一步，就檢查一下對講機有沒有收到取消訊號
                    if (token.IsCancellationRequested)
                    {
                        // 發現要取消了，趕快收工
                        Debug.WriteLine("收到指令，我不做了！ (Task Cancelled!)");
                        break;
                    }
                    currentIndex++;
                    if (currentIndex >= startLine + count)
                        break;
                    string line = reader.ReadLine();
                    if (currentIndex < startLine)
                        continue;
                    string[] fields = line.Split(','); // 5
                    T t = new T(); // 3
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (keyValuePairs.TryGetValue(props[i].Name, out int index))
                            props[i].SetValue(t, fields[index]);
                    }
                    datasList.Add(t);
                }
                reader.Close();
                return datasList;
            });
            //reader.Close();
            return datas;
        }
    }
}
