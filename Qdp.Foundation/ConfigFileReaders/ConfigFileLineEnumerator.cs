using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Qdp.Foundation.ConfigFileReaders
{
	public class ConfigFileLineEnumerator : IEnumerable<string>
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

		public ConfigFileLineEnumerator(ConfigFileLocationType locationType, params string[] configPaths)
		{
			_path = ConfigFilePathHelper.GetPath(locationType, configPaths);
		}

		public ConfigFileLineEnumerator(params string[] configPaths)
			: this(ConfigFileLocationType.Default, configPaths)
		{
		}

		public IEnumerator<string> GetEnumerator()
		{
			if (Exists)
			{
				return File.ReadLines(_path).GetEnumerator();
			}
			else
			{
				throw new FileNotFoundException(string.Format("File {0} is not found"), _path);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
