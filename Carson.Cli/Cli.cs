using System;
using System.Collections.Generic;
using System.Linq;

using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Experiment1
{
	class Cli
	{
		Context context;
		bool quit;
		List<Command> grammar;
		CommandParser parser;

		public Cli(Context context)
		{
			this.context = context;
			this.grammar = LoadGrammar();
			this.parser = new CommandParser(grammar);
		}

		public void RunCommandLoop()
		{
			while (!quit)
			{
				Console.WriteLine();
				var command = Console.ReadLine();
				if (!String.IsNullOrWhiteSpace(command))
				{
					if (Execute(command)) Console.WriteLine("\nOK");
					else Console.WriteLine("\nWhat?");
				}
			}
		}

		public bool Execute(string command)
		{
			return parser.Parse(command);
		}

		List<Command> LoadGrammar()
		{
			return new List<Command>
			{
				new Command
				{
					Pattern = "turn {something} on",
					Action = p =>
					{
						var nodes = FindNodesByName(p["something"]);
						if (nodes.Count == 0) Console.WriteLine($"Can't find any nodes called \"{p["something"]}\"");
						else
						{
							nodes.ForEach(t =>
							{
								if (t.Item1.CommandClasses == null) return;

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
						var nodes = FindNodesByName(p["something"]);
						if (nodes.Count == 0) Console.WriteLine($"Can't find any nodes called \"{p["something"]}\"");
						else
						{
							nodes.ForEach(t =>
							{
								if (t.Item1.CommandClasses == null) return;

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
						if (context.Areas.Exists(a => String.Compare(a.Name,p["area"],true) == 0))
						{
							Console.WriteLine($"Area \"{p["area"]}\" already exists");
						}
						else
						{
							context.Areas.Add(new Area
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
						var area1 = FindArea(p["area1"], context.Areas);
						var area2 = FindArea(p["area2"], context.Areas);
						if (area1 == null) Console.WriteLine($"Area \"{p["area1"]}\" does not exist");
						else if (area2 == null) Console.WriteLine($"Area \"{p["area2"]}\" does not exist");
						else
						{
							if (area1.Parent != null) area1.Parent.Children.Remove(area1);
							else context.Areas.Remove(area1);
							area1.Parent = area2;
							if (area2.Children == null) area2.Children=new List<Area>();
							area2.Children.Add(area1);
						}
					}
				},
				new Command
				{
					Pattern = "name node {node} as {name}",
					Action = p =>
					{

						var t = context.Network.GetNode(Byte.Parse(p["node"]));
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else
						{
							t.Item1.Name = p["name"];
						}
					}
				},
				new Command
				{
					Pattern = "alias node {node} as {alias}",
					Action = p =>
					{
						var t = context.Network.GetNode(Byte.Parse(p["node"]));
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else
						{
							t.Item1.Alias = p["alias"];
						}
					}
				},
				new Command
				{
					Pattern = "list nodes",
					Action = p => context.Network.FindNodes(ns => !String.IsNullOrEmpty(ns.Name)).ForEach( t =>
					{
						Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {t.Item1.LastContact.Ago()}");
					})
				},
				new Command
				{
					Pattern = "list batteries",
					Action = p =>
					{
						context.Network.FindNodes(ns => ns.CommandClasses?.Contains(CommandClass.Battery) ?? false).ForEach(t =>
						{
							Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {(t.Item1.BatteryReport != null? t.Item1.BatteryReport.Data.ToString() : "Unknown").PadRight(15)} {t.Item1.BatteryReport.Timestamp.Ago()}");
						});
					}
				},
				new Command
				{
					Pattern = "list areas",
					Action = p=> ListAreas(context.Areas)
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
		}

		List<(NodeState, Node)> FindNodesByName(string name)
		{
			return context.Network.FindNodes(ns => String.Compare(ns.Name, name, true) == 0 || String.Compare(ns.Alias, name, true) == 0);
		}

		Area FindArea(string name, List<Area> areas)
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

		void ListAreas(List<Area> areas, int depth = 0)
		{
			if (areas == null) return;

			foreach (var area in areas)
			{
				Console.WriteLine($"{new String(' ', depth)}{area.Name}");
				ListAreas(area.Children, depth + 1);
			}
		}
	}
}
