using System.IO;

namespace Qdp.Foundation.ConfigFileReaders
{
	public class ConfigFileTextReader
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

		public ConfigFileTextReader(ConfigFileLocationType locationType, params string[] configPaths)
		{
			_path = ConfigFilePathHelper.GetPath(locationType, configPaths);
		}

		public ConfigFileTextReader(params string[] configPaths)
			: this(ConfigFileLocationType.Default, configPaths)
		{
		}

		public string ReadAllText()
		{
			return File.ReadAllText(_path);
		}
	}
}
