using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Xml.Serialization;

namespace dnkTransactionsUI.Utils
{
	public static class IsolatedStorageHelper
	{
		private static readonly Lazy<IsolatedStorageFile> _storage = new Lazy<IsolatedStorageFile>(() => AppDomain.CurrentDomain.ActivationContext != null
			? IsolatedStorageFile.GetUserStoreForApplication()
			: IsolatedStorageFile.GetUserStoreForDomain());

		public static IsolatedStorageFileStream GetStream(string filename, bool read)
		{
			if (read && !FileExists(filename)) return null;

			return read
				? new IsolatedStorageFileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, _storage.Value)
				: new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write, _storage.Value);
		}

		public static string[] LoadAndSplitLines(string filename)
		{
			var data = Load(filename);
			if (string.IsNullOrEmpty(data))
				return new string[0];
			return data.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
		}

		private static IsolatedStorageFileStream GetStream(string filename)
		{
			if (!FileExists(filename))
				return null;

			return GetStream(filename, true);
		}

		public static string Load(string filename)
		{
			string str = null;

			using (var isoStream = GetStream(filename))
			{
				if (isoStream == null) return null;

				using (var reader = new StreamReader(isoStream))
				{
					str = reader.ReadToEnd();
				}
			}

			return str;
		}

		public static byte[] LoadBytes(string fileName)
		{
			byte[] bytesResult;
			using (var isoStream = GetStream(fileName))
			{
				if (isoStream == null) return null;

				bytesResult = new byte[isoStream.Length];
				isoStream.Read(bytesResult, 0, (int) isoStream.Length);
			}

			return bytesResult;
		}

		public static T Load<T>(string filename)
		{
			using (var isoStream = GetStream(filename))
			{
				if (isoStream == null) return default(T);

				var serializer = new XmlSerializer(typeof(T));
				return (T) serializer.Deserialize(isoStream);
			}
		}

		public static bool FileExists(string filename)
		{
			return _storage.Value.GetFileNames(filename).Length > 0;
		}

		public static void SaveLines(string filename, params string[] content)
		{
			var sb = new StringBuilder();
			foreach (var line in content)
				sb.AppendLine(line);

			Save(filename, sb.ToString());
		}

		public static void Save(string filename, byte[] bytes)
		{
			using (var isoStream = GetStream(filename, false))
			{
				isoStream.Write(bytes, 0, bytes.Length);
			}
		}

		public static void Save(string filename, string content)
		{
			Save(filename, Encoding.UTF8.GetBytes(content));
		}

		public static void Save<T>(string filename, T content)
		{
			using (var isoStream = GetStream(filename, false))
			{
				var serializer = new XmlSerializer(content.GetType());
				serializer.Serialize(isoStream, content);
			}
		}

		public static void Delete(string filename)
		{
			if (!FileExists(filename))
				return;
			//_log.Debug("Delete: Filename: " + filename);
			_storage.Value.DeleteFile(filename);
		}
	}
}