using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZWave.CommandClasses;

using Experiment1.ZWaveDrivers;
using System.Threading;
using System.Globalization;

namespace Experiment1
{
	public static class CommandLibrary
	{
		public static async Task TurnOnOff(Environment env, CommandMatch match)
		{
			var device = match.Parameters.ContainsKey("device") ? match.Parameters["device"] : null;
			var area = match.Parameters.ContainsKey("area") ? match.Parameters["area"] : env.Areas.Root.Name;
			var value = match.Parameters.ContainsKey("value") ? match.Parameters["value"] : null;

			bool state = value == "on" ? true : false;

			var a = env.Areas.Find(area);
			var nodes = env.Areas.Find(area).GetDevices(device);

			foreach (var node in nodes)
			{
				//if (node.IsDead) continue;
				try
				{
					if (node is GenericDevice b && b.Basic != null) await b.Basic.Set((byte)(state ? 255 : 0));
					else if (node is GenericDevice swb && swb.SwitchBinary != null) await swb.SwitchBinary.Set(state);
					else if (node is GenericDevice swml && swml.SwitchMultiLevel != null) await swml.SwitchMultiLevel.Set((byte)(state ? 255 : 0));
					else if (node is ILight light) await light.SetOn(state);
				}
				catch (Exception ex)
				{
					env.Write($"Failed to send command to {node.Name} in {node.Area.Name} - {ex.Message}");
					node.IsDead = true;
				}
			}
		}

		public static async Task GetBattery(Environment env, CommandMatch match)
		{
            await Task.Run(() =>
            {
                foreach (var info in env.ZWaveService.Nodes)
                {
                    Console.Write($"Node {info.Node}: ");
                    if (info.HasBattery == null) Console.WriteLine("unknown");
                    else if (info.HasBattery == true)
                    {
                        if (info.Battery != null) Console.WriteLine($"{info.Battery}");
                        else Console.WriteLine($"battery state unknown");
                    }
                    else Console.WriteLine("no battery");
                }
            });
		}

		public static async Task ShowArea(Environment env, CommandMatch match)
		{
			var startingArea = env.Areas.Find(match.Parameters["area"]);

			int count = 0;

			foreach (var area in startingArea.GetAreas())
			{
				if (area.Devices.Count() > 0)
				{
					Console.WriteLine($"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(area.Name)}");
					foreach (var device in area.Devices)
					{
						var state = await device.GetState();
						var stateString = state.Count > 0 && state[0] != null ? state[0].ToString() : null;
						if (stateString != null) Console.WriteLine($"  {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(device.Name)}: {stateString}");
						count++;
					}
				}
			}

			if (count == 0)
			{
				Console.WriteLine($"There are no devices in the {startingArea.Name}.");
			}
		}

		public static async Task SetColour(Environment env, CommandMatch match)
		{
			var device = match.Parameters.ContainsKey("device") ? match.Parameters["device"] : null;
			var area = match.Parameters.ContainsKey("area") ? match.Parameters["area"] : env.Areas.Root.Name;
			var value = match.Parameters.ContainsKey("value") ? match.Parameters["value"] : null;

			var a = env.Areas.Find(area);
			var nodes = env.Areas.Find(area).GetDevices(device);

			foreach (var node in nodes)
			{
				if (node is GenericDevice dev && dev.SwitchColor != null) await dev.SwitchColor.Set(value);
				else if (node is Light light) await light.SetColor(value);
			}
		}

		public static async Task SetLevel(Environment env, CommandMatch match)
		{
			var device = match.Parameters.ContainsKey("device") ? match.Parameters["device"] : null;
			var area = match.Parameters.ContainsKey("area") ? match.Parameters["area"] : env.Areas.Root.Name;
			var value = match.Parameters.ContainsKey("value") ? match.Parameters["value"] : null;

			var level = Int32.Parse(value);
			if (level < 0) level = 0;
			if (level == 255) level = 255;
			else if (level > 99) level = 99;

			var a = env.Areas.Find(area);
			var nodes = env.Areas.Find(area).GetDevices(device);

			foreach (var node in nodes)
			{
				if (node is GenericDevice swml && swml.SwitchMultiLevel != null) await swml.SwitchMultiLevel.Set((byte)level);
				else if (node is Light light) await light.SetLevel(level);
			}
		}

		public static async Task GetAssociations(Environment env, CommandMatch match)
		{
			var value = match.Parameters.ContainsKey("value") ? match.Parameters["value"] : null;

			var nodeID = Byte.Parse(value);
			var node = env.ZWaveService.GetNode(nodeID);

			var associationDriver = new ZWaveAssociationDriver(node.Node);
			var associations = await associationDriver.Get();

			foreach (var group in associations)
			{
				Console.Write($"Group {group.Key}: ");
				foreach (var id in group.Value)
				{
					Console.Write($"{id} ");
				}
				Console.WriteLine();
			}
		}

		public static async Task AddAssociation(Environment env, CommandMatch match)
		{
			var nodeID = Byte.Parse(match.Parameters["node"]);
			var groupID = Byte.Parse(match.Parameters["group"]);
			var valueID = Byte.Parse(match.Parameters["value"]);

			var node = env.ZWaveService.GetNode(nodeID);

			var associationDriver = new ZWaveAssociationDriver(node.Node);
			await associationDriver.Add(groupID, valueID);
		}

		public static async Task RemoveAssociation(Environment env, CommandMatch match)
		{
			var nodeID = Byte.Parse(match.Parameters["node"]);
			var groupID = Byte.Parse(match.Parameters["group"]);
			var valueID = Byte.Parse(match.Parameters["value"]);

			var node = env.ZWaveService.Nodes[nodeID];

			var associationDriver = new ZWaveAssociationDriver(node.Node);
			await associationDriver.Remove(groupID, valueID);
		}

		public static async Task GetParameter(Environment env, CommandMatch match)
		{
			var nodeID = Byte.Parse(match.Parameters["node"]);
			var param = Byte.Parse(match.Parameters["param"]);

			var node = env.ZWaveService.GetNode(nodeID);

			var config = node.Node.GetCommandClass<Configuration>();
			var report = await config.Get(param);

			Console.WriteLine(report.ToString());
		}

		public static async Task SetParameter(Environment env, CommandMatch match)
		{
			var nodeID = Byte.Parse(match.Parameters["node"]);
			var param = Byte.Parse(match.Parameters["param"]);
			var value = long.Parse(match.Parameters["value"]);

			var node = env.ZWaveService.GetNode(nodeID);

			var config = node.Node.GetCommandClass<Configuration>();
            throw new NotImplementedException();
			//await config.Set(param, value, false, 0, CancellationToken.None);
		}

		public static List<Command> GetVocab()
		{
			return new List<Command>
			{
				new Command
				{
					Patterns = new List<string>
					{
						"turn {device} {value:on|off}",
						"turn {device} in {area} {value:on|off}",
						"turn {area} {device} {value:on|off}"
					},
					Action = async (env, x) => await TurnOnOff(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"exit",
						"quit"
					},
					Action = async (env, x) => { await Task.Run(() => env.Quit = true); }
				},
				new Command
				{
					Patterns = new List<string>{"verbose" },
					Action = async (env, x) =>
					{
                        await Task.Run(() => env.ZWaveService.Verbose = true);
						Console.WriteLine("OK, all events will be reported");
					}
				},
				new Command
				{
					Patterns = new List<string>{"quiet" },
					Action = async (env, x) =>
					{
                        await Task.Run(() => env.ZWaveService.Verbose = false);
						Console.WriteLine("OK, I won't tell you about incomming events");
					}
				},
				new Command
				{
					Patterns = new List<string>
					{
						"get {area} {device} battery",
						"get {device} battery",
						"get {area} battery"
					},
					Action = async (env, x) => await GetBattery(env, x)
				},
				new Command
				{
					Patterns = new List<string> {"show {area}" },
					Action = async (env, x) => await ShowArea(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"colour {area} {device} {value}",
						"colour {area} {value}",
						"colour {device} {value}"
					},
					Action = async (env, x) => await SetColour(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"set {device} level {value} in {area}",
						"set {area} {device} level {value}",
						"set {device} level {value}",
					},
					Action = async (env, x) => await SetLevel(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave get node {value} associations"
					},
					Action = async (env, x) => await GetAssociations(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave add node {value} to node {node} association group {group}"
					},
					Action = async (env, x) => await AddAssociation(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave remove node {value} from node {node} association group {group}"
					},
					Action = async (env, x) => await RemoveAssociation(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave get node {node} parameter {param}"
					},
					Action = async (env, x) => await GetParameter(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave set node {node} parameter {param} to {value}"
					},
					Action = async (env, x) => await SetParameter(env, x)
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave update node {node} neighbours"
					},
					Action = async (env, x) =>
					{
						var nodeID = Convert.ToByte(x.Parameters["node"]);

						var response = await env.ZWaveService.GetNode(nodeID).Node.RequestNeighborUpdate((y) => Console.WriteLine(y));
					}
				},
				new Command
				{
					Patterns = new List<string>
					{
						"help"
					},
					Action = async (env, x) =>
					{
						foreach (var cmd in GetVocab())
						{
							foreach (var pattern in cmd.Patterns)
							{
								Console.WriteLine(pattern);
							}
						}
					}
				},
				new Command
				{
					Patterns = new List<string>
					{
						"zwave remove node {node}"
					},
					Action = async (env, x) =>
					{
						var nodeID = Convert.ToByte(x.Parameters["node"]);
					}
				},
				new Command
				{
					Patterns = new List<string>
					{
						"create area {value}"
					},
					Action = async (env,x) =>
					{
                        await Task.Run(() => env.Areas.Create(x.Parameters["value"]));
					}
				},
				new Command
				{
					Patterns = new List<string>
					{
						"add area {area} to {value}"
					},
					Action = async (env,x) =>
					{
						await Task.Run(() => env.Areas.Add(x.Parameters["value"], x.Parameters["area"]));
					}
				}
			};
		}

	}

}
