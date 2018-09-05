using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Qdp.Foundation.ConfigFileReaders
{
	public class ConfigFileSettingReader
	{
		private readonly string _path;

		public bool Exists
		{
			get { return File.Exists(_path); }
		}

		public string Path
		{
			get { return _path; }
		}

		public ConfigFileSettingReader(ConfigFileLocationType locationType, params string[] configPaths)
		{
			_path = ConfigFilePathHelper.GetPath(locationType, configPaths);
		}

		public ConfigFileSettingReader(params string[] configPaths)
			: this(ConfigFileLocationType.Default, configPaths)
		{
		}

		public Dictionary<string, string> ReadSettings(char delimiter = ':')
		{
			return File.ReadAllLines(_path)
				.Where(x => !string.IsNullOrEmpty(x))
				.Select(x =>
				{
					var splits = x.Split(delimiter);
					return Tuple.Create(splits[0], splits[1]);
				})
				.ToDictionary(x => x.Item1, x => x.Item2);
		}
	}
}
