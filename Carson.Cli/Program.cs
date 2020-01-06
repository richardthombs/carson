using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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

			List<Area> areas = new List<Area>();

			bool quit = false;
			var grammar = new List<Command>();
			grammar = new List<Command>
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
						}
					}
				},
				new Command
				{
					Pattern = "create area {area}",
					Action = p =>
					{
						if (areas.Exists(a => String.Compare(a.Name,p["area"],true) == 0))
						{
							Console.WriteLine($"Area \"{p["area"]}\" already exists");
						}
						else
						{
							areas.Add(new Area
							{
								Name = p["area"]
							});
						}
					}
				},
				new Command
				{
					Pattern = "add area {area1} to {area2}",
					Action = p =>
					{
						var area1 = FindArea(p["area1"], areas);
						var area2 = FindArea(p["area2"], areas);
						if (area1 == null) Console.WriteLine($"Area \"{p["area1"]}\" does not exist");
						else if (area2 == null) Console.WriteLine($"Area \"{p["area2"]}\" does not exist");
						else
						{
							if (area1.Parent != null) area1.Parent.Children.Remove(area1);
							else areas.Remove(area1);
							area1.Parent = area2;
							if (area2.Children == null) area2.Children=new List<Area>();
							area2.Children.Add(area1);
						}
					}
				},
				new Command
				{
					Pattern = "list nodes",
					Action = p => network.FindNodes(ns => !String.IsNullOrEmpty(ns.Name)).ForEach( t =>
					{
						Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {t.Item1.LastContact.Ago()}");
					})
				},
				new Command
				{
					Pattern = "list batteries",
					Action = p =>
					{
						network.FindNodes(ns => ns.CommandClasses?.Contains(CommandClass.Battery) ?? false).ForEach(t =>
						{
							Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {(t.Item1.BatteryReport != null? t.Item1.BatteryReport.Data.ToString() : "Unknown").PadRight(15)} {t.Item1.BatteryReport.Timestamp.Ago()}");
						});
					}
				},
				new Command
				{
					Pattern = "list areas",
					Action = p=> ListAreas(areas)
				},
				new Command
				{
					Pattern = "quit",
					Action = p => quit = true
				},
				new Command
				{
					Pattern = "help",
					Action = p => grammar.ForEach(x => Console.WriteLine(x.Pattern))
				}
			};

			var parser = new CommandParser(grammar);

			while(!quit)
			{
				Console.WriteLine();
				var command = Console.ReadLine();
				if (!String.IsNullOrWhiteSpace(command))
				{
					if (parser.Parse(command)) Console.WriteLine("\nOK");
					else Console.WriteLine("\nWhat?");
				}
			}
			zwave.Close();
		}

		static Area FindArea(string name, List<Area> areas)
		{
			if (areas == null) return null;
			foreach (var area in areas)
			{
				if (String.Compare(area.Name, name, true) == 0) return area;
				var found = FindArea(name, area.Children);
				if (found != null) return found;
			}
			return null;
		}

		static void ListAreas(List<Area> areas, int depth=0)
		{
			if (areas == null) return;

			foreach (var area in areas)
			{
				Console.WriteLine($"{new String(' ', depth)}{area.Name}");
				ListAreas(area.Children, depth + 1);
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
