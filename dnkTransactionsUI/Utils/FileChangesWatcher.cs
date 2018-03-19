using System;
using System.IO;
using System.Windows;

namespace dnkTransactionsUI.Utils
{
	public class FileChangesWatcher
	{
		public FileChangesWatcher(Action fileChangedAction)
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			var dataFileWatcher = new FileSystemWatcher(App.PathToDataFile.DirectoryName, App.PathToDataFile.Name.Replace(".csv", "*.csv"));
			FileSystemEventHandler dataFileWatcherOnCreated = (sender, args) => Application.Current.Dispatcher.Invoke(fileChangedAction);
			RenamedEventHandler dataFileWatcherOnRenamed = (sender, args) => Application.Current.Dispatcher.Invoke(fileChangedAction);
			dataFileWatcher.Created += dataFileWatcherOnCreated;
			dataFileWatcher.Changed += dataFileWatcherOnCreated;
			dataFileWatcher.Deleted += dataFileWatcherOnCreated;
			dataFileWatcher.Renamed += dataFileWatcherOnRenamed;
			dataFileWatcher.EnableRaisingEvents = true;
		}
	}
}