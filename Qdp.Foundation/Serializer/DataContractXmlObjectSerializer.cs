using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Qdp.Foundation.Serializer
{
	public static class DataContractXmlObjectSerializer
	{
		public static string Serialize<T>(T value)
		{
			if (value.Equals(default(T)))
			{
				return null;
			}

			var serializer = new DataContractSerializer(typeof(T));
			var settings = new XmlWriterSettings()
			{
				Indent = true,
				Encoding = new UTF8Encoding(false),
				NewLineChars = Environment.NewLine
			};

			using (var writer = new StringWriter())
			{
				using (var xmlWrite = XmlWriter.Create(writer, settings))
				{
					serializer.WriteObject(xmlWrite, value);
				}

				return writer.ToString();
			}
		}

		public static T Deserialize<T>(Stream xml)
		{
			if (xml == null)
			{
				return default(T);
			}

			var serializer = new DataContractSerializer(typeof(T));
			var settings = new XmlReaderSettings();
			using (var xmlReader = XmlReader.Create(xml, settings))
			{
				return (T) serializer.ReadObject(xmlReader);
			}
		}

		public static T Deserialize<T>(string xml)
		{
			if (string.IsNullOrEmpty(xml))
			{
				return default(T);
			}

			var serializer = new DataContractSerializer(typeof(T));
			var settings = new XmlReaderSettings();
			using(var reader = new StringReader(xml))
			using (var xmlReader = XmlReader.Create(reader, settings))
			{
				return (T)serializer.ReadObject(xmlReader);
			}
		}
	}
}
