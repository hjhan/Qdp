using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Qdp.Foundation.Serializer
{
	public static class DataContractJsonObjectSerializer
	{
		public static string Serialize<T>(T value)
		{
			var serializer = new DataContractJsonSerializer(typeof(T));
			string result;

			using(var memoryStream = new MemoryStream())
			using (var xmlWriter = JsonReaderWriterFactory.CreateJsonWriter(memoryStream))
			{
				serializer.WriteObject(xmlWriter, value);
				xmlWriter.Flush();
				result = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int) memoryStream.Length);
			}

			return result;
		}

		public static T Deserialize<T>(string json)
		{
			T result;

			using (var memoryStream = new MemoryStream())
			{
				byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
				memoryStream.Write(jsonBytes, 0, jsonBytes.Length);
				memoryStream.Seek(0, SeekOrigin.Begin);
				using (var reader = JsonReaderWriterFactory.CreateJsonReader(memoryStream, Encoding.UTF8, XmlDictionaryReaderQuotas.Max, null))
				{
					var serializer = new DataContractJsonSerializer(typeof(T));
					result = (T)serializer.ReadObject(reader);
				}
			}

			return result;
		}

		public static T Deserialize<T>(Stream json)
		{
			T result;

			using (var reader = JsonReaderWriterFactory.CreateJsonReader(json, Encoding.UTF8, XmlDictionaryReaderQuotas.Max, null))
			{
				var serializer = new DataContractJsonSerializer(typeof(T));
				result = (T) serializer.ReadObject(reader);
			}

			return result;
		}


	}
}
