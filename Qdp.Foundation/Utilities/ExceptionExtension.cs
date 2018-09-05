using System;
using System.Text;

namespace Qdp.Foundation.Utilities
{
	public static class ExceptionExtension
	{
		public static string GetDetail(this Exception ex)
		{
			var strBuilder = new StringBuilder();
			strBuilder.AppendLine("==========================================");
			strBuilder.AppendLine(ex.Message);
			if (ex.InnerException != null)
			{
				strBuilder.AppendLine("-----------------Inner Exception--------------------");
				strBuilder.AppendLine(ex.InnerException.Message);
			}
			strBuilder.AppendLine("------------------------------------------------");
			strBuilder.AppendLine("StackTrace");
			strBuilder.AppendLine(ex.StackTrace);
			strBuilder.AppendLine("==========================================");
			return strBuilder.ToString();
		}
	}
}
