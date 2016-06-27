using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TSUImageCollectSystem
{
	using Mono.Options;
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			this.Startup += (s, e) => 
			{
				System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory, "Log.txt"));
				Helpers.Log.UseSensibleDefaults("Log.txt", Environment.CurrentDirectory, Helpers.eloglevel.info);
				Helpers.Log.LogWhere = Helpers.elogwhere.file_and_console;
				Helpers.Log.LogThisInfo("Read Arguments...{0}", Helpers.Args.DefaultArgs.IPAddress);
			};
		}
	}
}
