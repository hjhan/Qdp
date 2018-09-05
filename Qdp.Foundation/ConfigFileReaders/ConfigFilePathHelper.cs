using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Qdp.Foundation.ConfigFileReaders
{
	public static class ConfigFilePathHelper
	{
		internal static string GetPath(ConfigFileLocationType locationType, params string[] configPaths)
		{
			var dirs = new List<string>();
            if (locationType == ConfigFileLocationType.Default)
            {
                var directory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                dirs.Add(directory); 
            }
            else if (locationType == ConfigFileLocationType.Web) {
                dirs.Add(System.AppDomain.CurrentDomain.RelativeSearchPath);
            } 

            dirs.AddRange(configPaths); 
            var path = Path.Combine(dirs.ToArray());
 
			if (!(Directory.Exists(path) || File.Exists(path)))
			{
				throw new ArgumentException(string.Format("Directory '{0} does not exist.", path));
			}

			return path;
		}

		public static List<string> GetFiles(ConfigFileLocationType locationType, params string[] configPaths)
		{
			var path = GetPath(locationType, configPaths);

			var fileList = new List<string>();
			fileList.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories));
			return fileList;
		}

		public static List<string> GetFiles(params string[] configPaths)
		{
			return GetFiles(ConfigFileLocationType.Default, configPaths);
		} 
	}
}
