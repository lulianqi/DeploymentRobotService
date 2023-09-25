using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public static class MyExtendHelper
    {
        //extent func
        public static string ToStringDetail(this Dictionary<string, string> dc)
        {
            if (dc == null || dc.Count == 0)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            foreach(var kv in dc )
            {
                sb.Append("[");
                sb.Append(kv.Key);
                sb.Append(" ");
                sb.Append(kv.Value);
                sb.Append("] ");
            }
            return sb.ToString();
        }

        public static string ToStringDetail<T>(this List<T> list)
        {
            if(list==null)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            foreach (T item in list)
            {
                sb.Append(item?.ToString()??"NULL");
                sb.Append(',');
            }
            if(sb.Length>0 && sb[sb.Length-1]==',')
            {
                sb = sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        public static void MyAdd<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 value)
        {
            if (dictionary != null)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }
        }

        public static bool MyContains(this string sourceStr,string key,string spit=" ")
        {
            if(sourceStr!=null)
            {
                if(key.Contains(spit))
                {
                    string[] keyArr = key.Split(spit, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string tempKey in keyArr)
                    {
                        if(!sourceStr.Contains(tempKey))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return sourceStr.Contains(key);
                }
            }
            return false;
        }

        //static func

        public static string ObjectToJsonStr(object obj)
        {
            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                using (StreamReader sr = new StreamReader(stream))
                {
                    stream.Position = 0;
                    return sr.ReadToEnd();
                }
            }
        }

        public static T DeserializeContractDataFromFilePath<T>(string filePath)
        {
            T serializeClass = default(T);
            if (File.Exists(filePath))
            {
                FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate);
                System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
                try
                {
                    serializeClass = (T)ser.ReadObject(fs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    serializeClass = default(T);
                }
                finally
                {
                    fs.Close();
                }

            }
            return serializeClass;
        }

        public static T DeserializeContractDataFromJsonString<T>(string str)
        {
            T serializeClass = default(T);
            System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
            try
            {
                using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(str)))
                {
                    serializeClass = (T)ser.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                serializeClass = default(T);
            }
            finally
            {

            }
            return serializeClass;
        }
    }
}
