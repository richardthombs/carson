using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;
using ZWave.Channel;

namespace Experiment1
{
	class ZWaveNetwork
	{
		public Dictionary<byte, NodeState> nodeStates;
		public NodeCollection nodes;
		public Action OnStateChange;
		ZWaveController z;

		public ZWaveNetwork(ZWaveController zwave, Dictionary<byte, NodeState> nodeStates)
		{
			this.nodeStates = nodeStates;
			this.z = zwave;
		}

		public async Task Start()
		{
			if (nodeStates.Count == 0)
			{
				Console.WriteLine("Looks like we are initialising for the first time, this might take a while...");
				Console.WriteLine();
			}

			var homeId = await z.GetHomeID();
			var nodeId = await z.GetNodeID();

			Console.WriteLine($"Home ID: {homeId:x8}, Controller ID: {nodeId:D3}");
			Console.WriteLine();

			nodes = await z.GetNodes();
			await ListNodes(nodes);
			await QueryNodes(nodes);
			await HealthCheck(nodes);

			SaveState();
			SubscribeAll(nodes);
		}

		public void Stop()
		{
			SaveState();
		}

		public List<(NodeState, Node)> FindNodes(Predicate<NodeState> predicate)
		{
			var filtered = nodeStates.Where(x => predicate(x.Value));
			var selected = filtered.Select(x => (x.Value, nodes[x.Key]));
			var list = selected.ToList();
			return list;
		}

		public (NodeState, Node) GetNode(byte nodeID)
		{
			var state = nodeStates.ContainsKey(nodeID) ? nodeStates[nodeID] : null;
			var node = nodes[nodeID];
			return (state, node);
		}

		async Task ListNodes(NodeCollection nodes)
		{
			foreach (var n in nodes)
			{
				var protocolInfo = await n.GetProtocolInfo();

				var neighbours = await n.GetNeighbours();

				if (!nodeStates.ContainsKey(n.NodeID))
				{
					Console.WriteLine($"Discovered node {n}:");
					Console.WriteLine($"Generic = {protocolInfo.GenericType}");
					Console.WriteLine($"Listening = {protocolInfo.IsListening}");
					Console.WriteLine($"Neighbours = {string.Join(", ", neighbours.Cast<object>().ToArray())}");
					Console.WriteLine();

					nodeStates.Add(n.NodeID, new NodeState { Name = $"Node {n.NodeID}" });
				}
				var state = nodeStates[n.NodeID];

				if (state.FirstAdded == null) state.FirstAdded = DateTimeOffset.UtcNow;
				state.NodeID = n.NodeID;
				state.HasBattery = !protocolInfo.IsListening;
				state.GenericType = protocolInfo.GenericType;
				state.Removed = false;
				state.Failed = await n.IsNodeFailed();
			}

			SaveState();
		}

		async Task QueryNodes(NodeCollection nodes)
		{
			bool messages = false;
			foreach (var n in nodes)
			{
				var state = nodeStates[n.NodeID];

				if (state.CommandClasses != null) continue;
				if (state.HasBattery)
				{
					Console.WriteLine($"Won't query node {n} until it wakes up");
					messages = true;
					continue;
				}
				if (state.FailCount >= 5 && !state.LastFailed.IsOlderThan(TimeSpan.FromDays(1)))
				{
					Console.WriteLine($"Won't query node {n} because it has failed {state.FailCount} times, most recently {state.LastFailed.Ago()}");
					messages = true;
					continue;
				}

				await QueryNode(n);
			}

			if (messages) Console.WriteLine();
		}

		async Task QueryNode(Node n)
		{
			var state = nodeStates[n.NodeID];

			try
			{
				Console.Write($"{DateTimeOffset.Now:t} Querying node {n}...");
				var classes = await n.GetSupportedCommandClasses();
				state.RecordContact();
				state.CommandClasses = classes.Select(x => x.Class).ToList();
				Console.WriteLine("OK");
			}
			catch
			{
				Console.WriteLine("Failed");
				state.RecordFailure();
			}

			SaveState();
		}

		async Task HealthCheck(NodeCollection nodes)
		{
			bool messages = false;
			foreach (var n in nodes)
			{
				if (n.NodeID == 1) continue;

				var tooLong = TimeSpan.FromDays(2);
				var state = nodeStates[n.NodeID];

				if (state.LastContact.HasValue && state.LastContact.IsOlderThan(tooLong) || !state.LastContact.HasValue && state.FirstAdded.IsOlderThan(tooLong))
				{
					if (state.FailCount < 5)
					{
						messages = true;
						Console.Write($"Node {n} has not been heard from recently, trying to ping it...");
						try
						{
							await n.GetSupportedCommandClasses();
							state.RecordContact();
							Console.WriteLine("Success");
						}
						catch
						{
							Console.WriteLine("Failed");
							state.RecordFailure();
						}
					}
				}
			}

			if (messages) Console.WriteLine();
			messages = false;

			foreach (var n in nodes)
			{
				var state = nodeStates[n.NodeID];
				if (state.FailCount > 0)
				{
					Console.WriteLine($"Warning: Last contact with node {n} was {state.LastContact.Ago()}. {state.FailCount} pings have failed, the last attempt was {state.LastFailed.Ago()}");
					messages = true;
				}
			}

			if (messages) Console.WriteLine();
			messages = false;

			foreach (var state in nodeStates)
			{
				if (nodes[state.Key] == null)
				{
					Console.WriteLine($"Warning: Node {state.Key:D3} ({state.Value.Name ?? "no name"}) has been removed from the network");
					state.Value.Removed = true;
					messages = true;
				}
			}

			if (messages) Console.WriteLine();
		}

		void SubscribeAll(NodeCollection nodes)
		{
			foreach (var n in nodes)
			{
				Subscribe(n);
			}
		}

		private void Subscribe(Node node)
		{
			var basic = node.GetCommandClass<Basic>();
			basic.Changed += (_, e) => ReceiveReport(e.Report);

			var sensorMultiLevel = node.GetCommandClass<SensorMultiLevel>();
			sensorMultiLevel.Changed += (_, e) =>
			{
				ReceiveSensorMultiLevelReport(e.Report);
				ReceiveReport(e.Report);
			};

			var meter = node.GetCommandClass<Meter>();
			meter.Changed += (_, e) => ReceiveReport(e.Report);

			var alarm = node.GetCommandClass<Alarm>();
			alarm.Changed += (_, e) =>
			{
				ReceiveAlarmReport(e.Report);
				ReceiveReport(e.Report);
			};

			var sensorBinary = node.GetCommandClass<SensorBinary>();
			sensorBinary.Changed += (_, e) => ReceiveReport(e.Report);

			var sensorAlarm = node.GetCommandClass<SensorAlarm>();
			sensorAlarm.Changed += (_, e) => ReceiveReport(e.Report);

			var wakeUp = node.GetCommandClass<WakeUp>();
			wakeUp.Changed += async (_, e) =>
			{
				ReceiveReport(e.Report);

				var state = nodeStates[e.Report.Node.NodeID];
				state.LastWakeUp = DateTimeOffset.UtcNow;
				if (state.CommandClasses == null) await QueryNode(e.Report.Node);

				if (state.CommandClasses.Contains(ZWave.Channel.CommandClass.Battery) && (state.BatteryReport == null || state.BatteryReport.Timestamp.IsOlderThan(TimeSpan.FromDays(1))))
				{
					Console.WriteLine("{DateTimeOffset.Now:t} Requesting battery state...");
					await node.GetCommandClass<Battery>().Get();
				}
			};

			var switchBinary = node.GetCommandClass<SwitchBinary>();
			switchBinary.Changed += (_, e) => ReceiveReport(e.Report);

			var thermostatSetpoint = node.GetCommandClass<ThermostatSetpoint>();
			thermostatSetpoint.Changed += (_, e) => ReceiveReport(e.Report);

			var sceneActivation = node.GetCommandClass<SceneActivation>();
			sceneActivation.Changed += (_, e) => ReceiveReport(e.Report);

			var multiChannel = node.GetCommandClass<MultiChannel>();
			multiChannel.Changed += (_, e) => ReceiveReport(e.Report);

			var switchMultiLevel = node.GetCommandClass<SwitchMultiLevel>();
			switchMultiLevel.Changed += (_, e) => ReceiveReport(e.Report);

			var centralScene = node.GetCommandClass<CentralScene>();
			centralScene.Changed += (_, e) => ReceiveReport(e.Report);

			var battery = node.GetCommandClass<Battery>();
			battery.Changed += (_, e) =>
			{
				ReceiveBatteryReport(e.Report);
				ReceiveReport(e.Report);
			};
		}

		void ReceiveReport(NodeReport r)
		{
			var state = nodeStates[r.Node.NodeID];
			state.RecordContact();

			if (!state.Muted) Console.WriteLine($"{DateTimeOffset.Now:t} Report from node {r.Node}: {r.GetType().Name} [{r}]");

			SaveState();
		}

		void ReceiveBatteryReport(ZWave.CommandClasses.BatteryReport report)
		{
			var r = new Report<BatteryReport>
			{
				Timestamp = DateTime.UtcNow,
				Data = new BatteryReport
				{
					IsLow = report.IsLow,
					Value = report.Value
				}
			};

			var state = nodeStates[report.Node.NodeID];
			state.BatteryReport = r;
		}

		void ReceiveAlarmReport(ZWave.CommandClasses.AlarmReport report)
		{
			var r = new Report<AlarmReport>
			{
				Timestamp = DateTime.UtcNow,
				Data = new AlarmReport
				{
					Type = report.Type,
					Level = report.Level,
					Detail = report.Detail,
					Unknown = report.Unknown
				}
			};

			var state = nodeStates[report.Node.NodeID];
			state.AlarmReport = r;
			if (state.CommandClasses == null || !state.CommandClasses.Contains(CommandClass.Alarm))
			{
				if (state.CommandClasses == null) state.CommandClasses = new List<CommandClass>();
				state.CommandClasses.Add(CommandClass.Alarm);
			}
		}

		void ReceiveSensorMultiLevelReport(ZWave.CommandClasses.SensorMultiLevelReport report)
		{
			var r = new Report<SensorMultiLevelReport>
			{
				Timestamp = DateTime.UtcNow,
				Data = new SensorMultiLevelReport
				{
					Type = report.Type,
					Value = report.Value,
					Unit = report.Unit,
					Scale = report.Scale
				}
			};

			var state = nodeStates[report.Node.NodeID];
			switch (report.Type)
			{
				case SensorType.Temperature: state.TemperatureReport = r; break;
				case SensorType.RelativeHumidity: state.RelativeHumidityReport = r; break;
				case SensorType.Luminance: state.LuminanceReport = r; break;
			}
		}

		void SaveState()
		{
			OnStateChange?.Invoke();
		}
	}
}
