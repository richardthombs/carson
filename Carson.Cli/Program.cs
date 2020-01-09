using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1
{
	class Program
	{
		static CentralScene wallmote1;
		static CentralScene wallmote2;

		static Context context;
		static Cli cli;

		static void Main()
		{
			KeepAlive();

			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				Converters = { new StringEnumConverter() }
			};

			var zwave = new ZWaveController("COM3");
			//zwave.Channel.Log = new System.IO.StreamWriter(Console.OpenStandardOutput());
			zwave.Channel.MaxRetryCount = 1;
			zwave.Channel.ResponseTimeout = TimeSpan.FromSeconds(10);
			zwave.Channel.ReceiveTimeout = TimeSpan.FromSeconds(10);
			zwave.Open();

			var network = new ZWaveNetwork(zwave);
			Task.Run(() => network.Start()).Wait();

			context = new Context
			{
				Network = network,
				Quit = false,
				Areas = new List<Area>(),
				Tasks = new List<BackgroundTask>()
			};

			wallmote1 = network.nodes[18].GetCommandClass<CentralScene>();
			wallmote2 = network.nodes[23].GetCommandClass<CentralScene>();

			wallmote1.Changed += wallMote1Changed;
			wallmote2.Changed += wallMote2Changed;

			cli = new Cli(context);

			var tasks = LoadTasks();
			tasks?.ForEach(x => cli.Execute(x, nak: true));

			var autoexec = new List<string>
			{
/*
				"name node 1 as Controller",
				"name node 18 as Wallmote 1",
				"name node 23 as Wallmote 2",
				"name node 13 as Study light switch",
				"name node 10 as Study bulb",
				"name node 14 as Porch bulb",
				"name node 15 as Patio bulb",
				"name node 16 as Patio bulb",
				"name node 17 as Patio bulb",
				"name node 24 as Patio bulb",
				"name node 22 as Sensor 1",
				"name node 25 as Sensor 2",
				"name node 8 as Motion sensor 1",
				"name node 11 as Motion sensor 2",
				"name node 12 as Motion sensor 3",
				"name node 2 as Plug 1"
*/
/*
				"cancel all tasks",
				"wait for 4pm then turn porch lights on then repeat",
				"wait for 10pm then turn porch lights off then repeat",
				"wait for 5am then turn porch lights on then repeat",
				"wait for 8am then turn porch lights off then repeat",
				"wait for 5pm then turn patio lights on then repeat",
				"wait for 10pm then turn patio lights off then repeat",
*/
				"list tasks"
			};
			autoexec.ForEach(x => cli.Execute(x, nak: true));

			Console.WriteLine("\nSystem ready");

			cli.RunCommandLoop();

			network.Stop();
			zwave.Close();

			SaveTasks();
		}

		static List<string> LoadTasks()
		{
			try
			{
				var json = File.ReadAllText("tasks.json");
				return JsonConvert.DeserializeObject<List<string>>(json);
			}
			catch { }

			return null;
		}

		static void SaveTasks()
		{
			lock (context)
			{
				var tasks = context.Tasks.Where(x => !x.Completed && !x.Cancelled && x.Command.EndsWith("then repeat")).Select(x => x.Command).ToList();
				var json = JsonConvert.SerializeObject(tasks);
				File.WriteAllText("tasks.json", json);
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern uint SetThreadExecutionState(uint esFlags);
		static void KeepAlive()
		{
			SetThreadExecutionState(0x00000001 | 0x80000000);
		}

		static void wallMote1Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			var button = e.Report.SceneNumber;
			Console.WriteLine($"{DateTimeOffset.Now:t} Wallmote 1 button {button} pressed");

			switch (button)
			{
				case 1: cli.Execute("turn study lights on"); break;
				case 3: cli.Execute("turn study lights off"); break;
				case 2: cli.Execute("turn patio lights on"); break;
				case 4: cli.Execute("turn patio lights off"); break;
			}
		}

		static void wallMote2Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			var button = e.Report.SceneNumber;
			Console.WriteLine($"{DateTimeOffset.Now:t} Wallmote 2 button {button} pressed");

			switch (button)
			{
				case 1: cli.Execute("turn porch lights on"); break;
				case 3: cli.Execute("turn porch lights off"); break;
				case 2: cli.Execute("turn patio lights on"); break;
				case 4: cli.Execute("turn patio lights off"); break;
			}
		}
	}

	class Area
	{
		public string Name;
		public Area Parent;
		public List<Area> Children;
	}
}
