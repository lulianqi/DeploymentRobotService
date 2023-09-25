using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.MyHelper
{
    public class JsonHelper
    {
        public static string SerializeContractData(object obj)
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

        public static T DeserializeContractData<T>(FileStream fs)
        {
            T serializeClass = default(T);
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
            return serializeClass;
        }

        public static T DeserializeContractData<T>(string str)
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


        public static string SerializeJson<T>(T data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public static T DeserializeJson<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public static long ConvertToTimeStamp(DateTime time)
        {
            DateTime dateTime = new DateTime(1993, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            return (long)(time.AddHours(-8) - dateTime).TotalMilliseconds;
        }
        
    }
}
