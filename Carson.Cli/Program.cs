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

			// Start the network
			var controller = CreateController("COM3");
			var nodeStates = LoadStates();
			var network = new ZWaveNetwork(controller, nodeStates)
			{
				OnStateChange = () => SaveStates(nodeStates)
			};
			Task.Run(() => network.Start()).Wait();

			// Build a context for commands to run in
			var context = new Context
			{
				Network = network,
				Areas = new List<Area>(),
				Tasks = new List<BackgroundTask>()
			};

			// Create the command line interface
			cli = new Cli(context);

			// Load any background tasks and launch them again
			var tasks = LoadTasks();
			tasks.ForEach(x => cli.Execute(x, nak: true));

			// Run any startup commands
			var autoexec = LoadAutoexec();
			autoexec.ForEach(x => cli.Execute(x, nak: true));

			// TODO: Hack in Wallmote button handlers until I write some built-in support
			wallmote1 = network.nodes[18].GetCommandClass<CentralScene>();
			wallmote2 = network.nodes[23].GetCommandClass<CentralScene>();
			wallmote1.Changed += wallMote1Changed;
			wallmote2.Changed += wallMote2Changed;

			Console.WriteLine("\nSystem ready");

			cli.RunCommandLoop();

			network.Stop();
			controller.Close();

			SaveStates(nodeStates);
			SaveTasks(context.Tasks);
		}

		private static ZWaveController CreateController(string portName)
		{
			var zwaveController = new ZWaveController(portName);
			//zwave.Channel.Log = new System.IO.StreamWriter(Console.OpenStandardOutput());
			zwaveController.Channel.MaxRetryCount = 1;
			zwaveController.Channel.ResponseTimeout = TimeSpan.FromSeconds(10);
			zwaveController.Channel.ReceiveTimeout = TimeSpan.FromSeconds(10);
			zwaveController.Open();
			return zwaveController;
		}

		static Dictionary<byte, NodeState> LoadStates()
		{
			try
			{
				var json = File.ReadAllText("state.json");
				return JsonConvert.DeserializeObject<Dictionary<byte, NodeState>>(json);
			}
			catch { }

			return new Dictionary<byte, NodeState>();
		}

		static void SaveStates(Dictionary<byte, NodeState> nodeStates)
		{
			var json = JsonConvert.SerializeObject(nodeStates);
			File.WriteAllText("state.json", json);
		}

		static List<string> LoadTasks()
		{
			try
			{
				var json = File.ReadAllText("tasks.json");
				return JsonConvert.DeserializeObject<List<string>>(json);
			}
			catch { }

			return new List<string>();
		}

		static void SaveTasks(List<BackgroundTask> tasks)
		{
			var runningTasks = tasks.Where(x => !x.Completed && !x.Cancelled && x.Command.EndsWith("then repeat")).Select(x => x.Command).ToList();
			var json = JsonConvert.SerializeObject(runningTasks);
			File.WriteAllText("tasks.json", json);
		}

		static List<string> LoadAutoexec()
		{
			return new List<string>
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
