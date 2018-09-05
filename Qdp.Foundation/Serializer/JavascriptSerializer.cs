#if !NETCOREAPP2_1
using System.Web.Script.Serialization;
#else
using Newtonsoft.Json;
#endif

namespace Qdp.Foundation.Serializer
{
	public static class JavascriptSerializer
	{
		public static string Serialize<T>(T value)
		{
#if !NETCOREAPP2_1
            var jsonSerializer = new JavaScriptSerializer();
			return jsonSerializer.Serialize(value);
#else
            return JsonConvert.SerializeObject(value);
#endif
        }

		public static T Deserialize<T>(string json)
		{
#if !NETCOREAPP2_1
			var jsonSerializer = new JavaScriptSerializer();
			return jsonSerializer.Deserialize<T>(json);
#else
            return JsonConvert.DeserializeObject<T>(json);
#endif
        }
	}
}
