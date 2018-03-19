using System;
using System.Configuration;
using System.IO;
using System.Windows;

namespace dnkTransactionsUI
{
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static int? LimitToYear { get; set; }
		public static FileInfo PathToDataFile { get; set; } = new FileInfo(ConfigurationManager.AppSettings["fileLocation"]);

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			foreach (var arg in e.Args)
			{
				int year;
				if (int.TryParse(arg, out year))
				{
					LimitToYear = year;
					continue;
				}

				try
				{
					var dataFile = new FileInfo(arg);
					if (dataFile.Exists)
						PathToDataFile = dataFile;
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}
	}
}