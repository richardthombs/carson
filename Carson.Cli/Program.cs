using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Newtonsoft.Json;

using ZWave;
using Newtonsoft.Json.Converters;
using ZWave.CommandClasses;
using System.Runtime.InteropServices;
using System.Threading;

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
				Orders = new List<StandingOrder>()
			};

			LoadState();

			wallmote1 = network.nodes[18].GetCommandClass<CentralScene>();
			wallmote2 = network.nodes[23].GetCommandClass<CentralScene>();

			wallmote1.Changed += wallMote1Changed;
			wallmote2.Changed += wallMote2Changed;

			var orders = new List<StandingOrder>
			{
				new StandingOrder { RunAt = DateTimeOffset.UtcNow.Date.AddHours(16), Command="turn porch light on"},
				new StandingOrder { RunAt = DateTimeOffset.UtcNow.Date.AddHours(22), Command="turn porch light off"},
				new StandingOrder { RunAt = DateTimeOffset.UtcNow.Date.AddHours(5), Command="turn porch light on"},
				new StandingOrder { RunAt = DateTimeOffset.UtcNow.Date.AddHours(8), Command="turn porch light off"},
				new StandingOrder { RunAt = DateTimeOffset.UtcNow.Date.AddHours(18), Command="turn patio lights on"},
				new StandingOrder { RunAt = DateTimeOffset.UtcNow.Date.AddHours(21), Command="turn patio lights off"}
			};
			context.Orders = orders;


			foreach (var order in context.Orders)
			{
				var now = DateTimeOffset.UtcNow;
				if (order.RunAt.HasValue)
				{
					if (order.RunAt.Value < now) order.RunAt = DateTimeOffset.UtcNow.Date.Add(order.RunAt.Value.TimeOfDay).AddDays(1);
					order.ScheduledFor = order.RunAt;
				}
				else if (order.WaitFor.HasValue)
				{
					order.ScheduledFor = now.Add(order.WaitFor.Value);
				}
			}

			foreach (var order in context.Orders.OrderBy(x => x.ScheduledFor.Value))
			{
				Task.Run(async () =>
				{
					while (true)
					{
						await ExecuteAt(order.Command, order.ScheduledFor.Value);
						Console.WriteLine($"{DateTimeOffset.Now:t} \"{order.Command}\" executed");

						var now = DateTimeOffset.UtcNow;
						if (order.RunAt.HasValue)
						{
							if (order.RunAt.Value < now) order.RunAt = DateTimeOffset.UtcNow.Date.Add(order.RunAt.Value.TimeOfDay).AddDays(1);
							order.ScheduledFor = order.RunAt;
						}
						else if (order.WaitFor.HasValue)
						{
							order.ScheduledFor = now.Add(order.WaitFor.Value);
						}

						SaveState();
					}
				});
			}

			cli = new Cli(context);

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
				"list orders"
			};
			autoexec.ForEach(x => cli.Execute(x, false));

			Console.WriteLine("\nSystem ready");

			cli.RunCommandLoop();

			network.Stop();
			zwave.Close();

			SaveState();
		}


		static void LoadState()
		{
			lock (context)
			{
				var json = File.ReadAllText("orders.json");
				context.Orders = JsonConvert.DeserializeObject<List<StandingOrder>>(json);
			}
		}

		static void SaveState()
		{
			lock (context)
			{
				var json = JsonConvert.SerializeObject(context.Orders);
				File.WriteAllText("orders.json", json);
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
				case 1: cli.Execute("turn porch light on"); break;
				case 3: cli.Execute("turn porch light off");
					ExecuteAfter("turn porch lights off", TimeSpan.FromHours(4));
					break;
				case 2:
					cli.Execute("turn patio lights on");
					ExecuteAfter("turn patio lights off", TimeSpan.FromHours(4));
					break;
				case 4: cli.Execute("turn patio lights off"); break;
			}
		}

		static async Task ExecuteAfter(string command, TimeSpan delay, CancellationToken? cancellationToken = null)
		{
			await Task.Delay(delay, cancellationToken ?? CancellationToken.None);
			cli.Execute(command);
		}

		static async Task ExecuteAt(string command, DateTimeOffset when, CancellationToken? cancellationToken = null)
		{
			await ExecuteAfter(command, when - DateTimeOffset.UtcNow, cancellationToken);
		}
	}

	class Area
	{
		public string Name;
		public Area Parent;
		public List<Area> Children;
	}

	class StandingOrder
	{
		public DateTimeOffset? RunAt;
		public TimeSpan? WaitFor;
		public string Command;

		/// <summary>
		/// When the scheduler will run the command
		/// </summary>
		public DateTimeOffset? ScheduledFor;
	}
}
