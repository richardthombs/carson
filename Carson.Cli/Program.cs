using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

using ZWave;
using Newtonsoft.Json.Converters;
using ZWave.Channel;
using ZWave.CommandClasses;
using System.Runtime.InteropServices;

namespace Experiment1
{
	class Program
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern uint SetThreadExecutionState(uint esFlags);
		static void KeepAlive()
		{
			SetThreadExecutionState(0x00000001 | 0x80000000);
		}

		static void Main()
		{
			KeepAlive();

			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				Converters = {new StringEnumConverter()}
			};

			var zwave = new ZWaveController("COM3");
			//zwave.Channel.Log = new System.IO.StreamWriter(Console.OpenStandardOutput());
			zwave.Channel.MaxRetryCount = 1;
			zwave.Channel.ResponseTimeout = TimeSpan.FromSeconds(10);
			zwave.Channel.ReceiveTimeout = TimeSpan.FromSeconds(10);
			zwave.Open();

			var network = new ZWaveNetwork(zwave);
			Task.Run(() => network.Start()).Wait();

			bool quit = false;
			var grammar = new List<Command>
			{
				new Command
				{
					Pattern = "turn {something} on",
					Action = p =>
					{
						var nodes = network.FindNodes(ns => String.Compare(ns.Name, p["something"], true) == 0);
						if (nodes.Count == 0) Console.WriteLine($"Can't find any nodes called \"{p["something"]}\"");
						else
						{
							nodes.ForEach(t =>
							{
								if (t.Item1.CommandClasses.Contains(CommandClass.SwitchBinary)) t.Item2.GetCommandClass<SwitchBinary>().Set(true);
								else if (t.Item1.CommandClasses.Contains(CommandClass.SwitchMultiLevel)) t.Item2.GetCommandClass<SwitchMultiLevel>().Set(255);
							});
							Console.WriteLine($"OK I turned {p["something"]} on");
						}
					}
				},
				new Command
				{
					Pattern = "turn {something} off",
					Action = p =>
					{
						var nodes = network.FindNodes(ns => String.Compare(ns.Name, p["something"], true) == 0);
						if (nodes.Count == 0) Console.WriteLine($"Can't find any nodes called \"{p["something"]}\"");
						else
						{
							nodes.ForEach(t =>
							{
								if (t.Item1.CommandClasses.Contains(CommandClass.SwitchBinary)) t.Item2.GetCommandClass<SwitchBinary>().Set(false);
								else if (t.Item1.CommandClasses.Contains(CommandClass.SwitchMultiLevel)) t.Item2.GetCommandClass<SwitchMultiLevel>().Set(0);
							});
							Console.WriteLine($"OK I turned {p["something"]} off");
						}
					}
				},
				new Command
				{
					Pattern = "set node {node} to {value}",
					Action = p => Console.WriteLine($"{p["node"]} = {p["value"]}")
				},
				new Command { Pattern = "create area {area}" },
				new Command { Pattern = "add area {area1} to {area2}" },
				new Command
				{
					Pattern = "list nodes",
					Action = p => network.FindNodes(ns => !String.IsNullOrEmpty(ns.Name)).ForEach( t =>
					{
						Console.WriteLine($"Node {t.Item2}: {t.Item1.Name}");
					})
				},
				new Command
				{
					Pattern = "list batteries",
					Action = p =>
					{
						network.FindNodes(ns => ns.CommandClasses?.Contains(CommandClass.Battery) ?? false).ForEach(x =>
						{
							Console.WriteLine($"{x.Item1.Name}: {(x.Item1.BatteryReport != null? x.Item1.BatteryReport.Data.ToString() : "Unknown")}");
						});
					}
				},
				new Command
				{
					Pattern = "quit",
					Action = p => quit = true
				}
			};

			var parser = new CommandParser(grammar);

			while(!quit)
			{
				Console.WriteLine();
				var command = Console.ReadLine();
				if (!String.IsNullOrWhiteSpace(command))
				{
					if (!parser.Parse(command))
					{
						Console.WriteLine("What?");
					}
				}
			}
			zwave.Close();
		}
	}
}
