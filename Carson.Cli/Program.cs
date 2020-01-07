using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using ZWave;
using Newtonsoft.Json.Converters;
using ZWave.CommandClasses;
using System.Runtime.InteropServices;

namespace Experiment1
{
	class Program
	{
		static CentralScene wallmote1;
		static CentralScene wallmote2;
		static SwitchBinary studyLight;
		static SwitchMultiLevel studyBulb;
		static SwitchMultiLevel porchBulb;
		static List<SwitchMultiLevel> patioBulbs;

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

			var env = new Context
			{
				Network = network,
				Quit = false,
				Areas = new List<Area>()
			};

			wallmote1 = network.nodes[18].GetCommandClass<CentralScene>();
			wallmote2 = network.nodes[23].GetCommandClass<CentralScene>();
			studyLight = network.nodes[13].GetCommandClass<SwitchBinary>();
			studyBulb = network.nodes[10].GetCommandClass<SwitchMultiLevel>();
			porchBulb = network.nodes[14].GetCommandClass<SwitchMultiLevel>();
			patioBulbs = new List<SwitchMultiLevel> {
				network.nodes[15].GetCommandClass<SwitchMultiLevel>(),
				network.nodes[16].GetCommandClass<SwitchMultiLevel>(),
				network.nodes[17].GetCommandClass<SwitchMultiLevel>(),
				network.nodes[24].GetCommandClass<SwitchMultiLevel>(),
			};

			wallmote1.Changed += wallMote1Changed;
			wallmote2.Changed += wallMote2Changed;

			var cli = new Cli(env);

			var autoexec = new List<string>
			{
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
			};
			//autoexec.ForEach(x => cli.Execute(x));

			cli.RunCommandLoop();

			zwave.Close();
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern uint SetThreadExecutionState(uint esFlags);
		static void KeepAlive()
		{
			SetThreadExecutionState(0x00000001 | 0x80000000);
		}

		static void wallMote1Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			Console.WriteLine($"{DateTimeOffset.Now:t} Wallmote 1 button {e.Report.SceneNumber} pressed");

			if (e.Report.SceneNumber == 1 || e.Report.SceneNumber == 3)
			{
				studyLight.Set(e.Report.SceneNumber == 1);
				studyBulb.Set((byte)(e.Report.SceneNumber == 1 ? 255 : 0));
			}

			if (e.Report.SceneNumber == 2 || e.Report.SceneNumber == 4)
			{
				patioBulbs.ForEach(bulb => bulb.Set((byte)(e.Report.SceneNumber == 2 ? 255 : 0)));
			}
		}

		static void wallMote2Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			Console.WriteLine($"{DateTimeOffset.Now:t} Wallmote 2 button {e.Report.SceneNumber} pressed");

			if (e.Report.SceneNumber == 1 || e.Report.SceneNumber == 3)
			{
				porchBulb.Set((byte)(e.Report.SceneNumber == 1 ? 255 : 0));
			}

			if (e.Report.SceneNumber == 2 || e.Report.SceneNumber == 4)
			{
				patioBulbs.ForEach(bulb => bulb.Set((byte)(e.Report.SceneNumber == 2 ? 255 : 0)));
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
