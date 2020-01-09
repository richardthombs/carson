using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Experiment1
{
	class Cli
	{
		Context context;
		List<Command> grammar;
		CommandParser parser;
		bool quit;

		public Cli(Context context)
		{
			this.context = context;
			this.grammar = LoadGrammar();
			this.parser = new CommandParser(grammar);
		}

		public void Execute(string command, bool nak = false)
		{
			var fragments = command.Split(new string[] { " then " }, StringSplitOptions.RemoveEmptyEntries);

			if (fragments.Any(x => IsWaitFor(x)))
			{
				var taskID = context.Tasks.Count + 1;
				var source = new CancellationTokenSource();
				var token = source.Token;
				var task = new BackgroundTask
				{
					TaskID = taskID,
					Command = command,
					CanncelationSource = source,
					CancellationToken = token
				};
				context.Tasks.Add(task);

				Task.Run(() =>
				{
					for (int f = 0; f < fragments.Length; f++)
					{
						if (!token.IsCancellationRequested)
						{
							var fragment = fragments[f];
							if (fragment == "repeat")
							{
								f = 0;
								fragment = fragments[f];
							}

							var ok = ExecuteFragment(fragment, task);
							if (ok && !IsWaitFor(fragment)) Console.WriteLine($"Task {task.TaskID} executed \"{fragment}\"");
							if (!ok) Console.WriteLine($"Task {task.TaskID} contains a command I don't understand: \"{fragment}\"");
						}
					}
					task.Cancelled = token.IsCancellationRequested;
					task.Completed = !token.IsCancellationRequested;
					if (!token.IsCancellationRequested) Console.WriteLine($"Task {taskID} has completed");

				}, token);

				if (!nak) Console.WriteLine($"Task {taskID} started");
			}
			else
			{
				var oks = new List<bool>();
				foreach (var fragment in fragments)
				{
					var ok = ExecuteFragment(fragment);
					if (!ok) Console.WriteLine($"Sorry, I don't understand \"{fragment}\"");
					oks.Add(ok);
				}
				if (!nak && oks.All(x => x)) Console.WriteLine("OK");
			}
		}

		public void RunCommandLoop()
		{
			while (!quit)
			{
				Console.WriteLine();
				Console.Write("> ");
				var command = Console.ReadLine();
				if (!String.IsNullOrWhiteSpace(command))
				{
					Execute(command);
				}
			}
		}

		bool IsWaitFor(string command)
		{
			return command.StartsWith("wait for");
		}


		bool ExecuteFragment(string command, BackgroundTask task = null)
		{
			var action = parser.Parse(command, task);
			if (action == null) return false;
			action();
			return true;
		}

		List<Command> LoadGrammar()
		{
			return new List<Command>
			{
				new Command
				{
					Pattern = "turn {something} on",
					Action = (p, _) =>
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
					Action = (p, _) =>
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
					Action = (p, _) =>
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
					Action = (p, _) =>
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
					Action = (p, _) =>
					{

						var t = context.Network.GetNode(Byte.Parse(p["node"]));
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else t.Item1.Name = p["name"];
					}
				},
				new Command
				{
					Pattern = "alias node {node} as {alias}",
					Action = (p, _) =>
					{
						var t = context.Network.GetNode(Byte.Parse(p["node"]));
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else t.Item1.Alias = p["alias"];
					}
				},
				new Command
				{
					Pattern = "list nodes",
					Action = (p, _) => context.Network.FindNodes(ns => true).OrderBy(t => t.Item1.Name).ToList().ForEach( t =>
					{
						Console.Write($"Node {t.Item1.NodeID:D3}: {(t.Item1.Name ?? "").PadRight(20)} ");
						Console.Write($"{t.Item1.LastContact.Ago().PadRight(25)} ");
						Console.Write($"{(t.Item1.Muted? "(Muted)" : "").PadRight(7)} ");
						Console.Write($"{(t.Item1.Failed? "(Failed)" : "").PadRight(8)} ");
						Console.Write($"{(t.Item1.Removed? "(Removed)" : "").PadRight(9)} ");
						Console.WriteLine();
					})
				},
				new Command
				{
					Pattern = "list batteries",
					Action = (p, _) =>
					{
						context.Network.FindNodes(ns => ns.CommandClasses?.Contains(CommandClass.Battery) ?? false).ForEach(t =>
						{
							Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {(t.Item1.BatteryReport != null? t.Item1.BatteryReport.Data.ToString() : "Unknown").PadRight(15)} {t.Item1.BatteryReport?.Timestamp.Ago() ?? ""}");
						});
					}
				},
				new Command
				{
					Pattern = "list sensors",
					Action = (p, _) =>
					{
						context.Network.FindNodes(ns => ns.CommandClasses?.Contains(CommandClass.SensorMultiLevel) ?? false).ForEach(t =>
						{
							var reports = new List<Report<SensorMultiLevelReport>> { t.Item1.TemperatureReport, t.Item1.RelativeHumidityReport, t.Item1.LuminanceReport };
							reports.RemoveAll(x => x == null);

							if (reports.Count == 0)
							{
								Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} No reports");
							}
							else
							{
								Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {reports[0].Data.ToString().PadRight(40)} {reports[0].Timestamp.Ago()}");
								for (int i = 1; i < reports.Count; i++)
								{
									Console.WriteLine($"{new String(' ', 30)} {reports[i].Data.ToString().PadRight(40)} {reports[i].Timestamp.Ago()}");
								}
							}
						});
					}
				},
				new Command
				{
					Pattern = "list alarms",
					Action = (p, _) =>
					{
						context.Network.FindNodes(ns => ns.CommandClasses?.Contains(CommandClass.Alarm) ?? false).ForEach(t =>
						{
							Console.WriteLine($"Node {t.Item2}: {t.Item1.Name.PadRight(20)} {(t.Item1.AlarmReport != null? t.Item1.AlarmReport.Data.ToString() : "Unknown").PadRight(50)} {t.Item1.AlarmReport?.Timestamp.Ago() ?? ""}");
						});
					}
				},
				new Command
				{
					Pattern = "list areas",
					Action = (p, _) => ListAreas(context.Areas)
				},
				new Command
				{
					Pattern = "mute node {node}",
					Action = (p, _) =>
					{
						var t = context.Network.GetNode(Byte.Parse(p["node"]));
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else t.Item1.Muted = true;
					}
				},
				new Command
				{
					Pattern = "unmute node {node}",
					Action = (p, _) =>
					{
						var t = context.Network.GetNode(Byte.Parse(p["node"]));
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else t.Item1.Muted = false;
					}
				},
				new Command
				{
					Pattern = "forget node {node}",
					Action = (p, _) =>
					{
						var id = Byte.Parse(p["node"]);
						var t = context.Network.GetNode(id);
						if (t.Item1 == null) Console.WriteLine($"Node {p["node"]} does not exist");
						else context.Network.nodeStates.Remove(id);
					}
				},
				new Command
				{
					Pattern = "quit",
					Action = (p, _) => quit = true
				},
				new Command
				{
					Pattern = "help",
					Action = (p, _) => grammar.ForEach(x => Console.WriteLine(x.Pattern))
				},
				new Command
				{
					Pattern = "wait for {event}",
					Action = (p, task) =>
					{
						var when = ParseWaitFor(p["event"]);
						if (when.HasValue)
						{
							task.SleepUntil = when.Value;
							task.CancellationToken.WaitHandle.WaitOne(when.Value - DateTimeOffset.UtcNow);
							task.SleepUntil = null;
						}
					}
				},
				new Command
				{
					Pattern = "list tasks",
					Action = (p, _) =>
					{
						if (context.Tasks.Count > 0)
						{
							var longest = context.Tasks.Max( x=> x.Command.Length);

							context.Tasks.ForEach(t =>
							{
								var state = "Running";
								if (t.Completed) state="Completed";
								if (t.Cancelled) state="Cancelled";
								if (t.SleepUntil.HasValue) state=$"{t.SleepUntil.Value.In()}";

								Console.WriteLine($"{t.TaskID.ToString().PadLeft(2)}: {t.Command.PadRight(longest + 1)} {state}");
							});
						}
					}
				},
				new Command
				{
					Pattern = "cancel task {task}",
					Action = (p, _) =>
					{
						var taskID = Int32.Parse(p["task"]);
						var task = context.Tasks.FirstOrDefault(x => x.TaskID == taskID);
						if (task != null) task.CanncelationSource.Cancel();
						else Console.WriteLine("No such task");
					}
				},
				new Command
				{
					Pattern = "cancel all tasks",
					Action = (p, _) =>
					{
						context.Tasks.ForEach( t => t.CanncelationSource.Cancel());
					}
				}
			};
		}

		DateTimeOffset? ParseWaitFor(string waitForParameter)
		{
			var delayRegex = new Regex(@"(\d+) (hour|minute|second|hour)s?");
			var delayMatch = delayRegex.Match(waitForParameter);
			if (delayMatch.Success)
			{
				TimeSpan delay = TimeSpan.Zero;
				switch (delayMatch.Groups[2].Value)
				{
					case "second": delay = TimeSpan.FromSeconds(Int32.Parse(delayMatch.Groups[1].Value)); break;
					case "minute": delay = TimeSpan.FromMinutes(Int32.Parse(delayMatch.Groups[1].Value)); break;
					case "hour": delay = TimeSpan.FromHours(Int32.Parse(delayMatch.Groups[1].Value)); break;
					default:
						Console.WriteLine(delayMatch.Groups[2]);
						return null;
				}

				return DateTimeOffset.UtcNow + delay;
			}
			else
			{
				DateTimeOffset? when = DateTimeOffset.TryParse(waitForParameter, out DateTimeOffset whenOut) ? (DateTimeOffset?)whenOut : null;
				if (!when.HasValue) return null;

				when = DateTimeOffset.UtcNow.Date.Add(when.Value.TimeOfDay);
				if (when < DateTimeOffset.UtcNow) when = when.Value.AddDays(1);
				return when;
			}
		}

		List<(NodeState, Node)> FindNodesByName(string name)
		{
			return context.Network.FindNodes(NodeNameMatch(name));
		}

		Predicate<NodeState> NodeNameMatch(string name)
		{
			return (ns) => String.Compare(ns.Name, name, true) == 0 || String.Compare(ns.Alias, name, true) == 0;
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
