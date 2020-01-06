using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1
{
	class ZWaveNetwork
	{
		ZWaveController z;
		Dictionary<byte, NodeState> nodeStates;
		NodeCollection nodes;

		CentralScene wallmote1;
		CentralScene wallmote2;
		SwitchBinary studyLight;
		SwitchMultiLevel studyBulb;
		SwitchMultiLevel porchBulb;
		List<SwitchMultiLevel> patioBulbs;

		public ZWaveNetwork(ZWaveController zwave)
		{
			this.nodeStates = new Dictionary<byte, NodeState>();
			this.z = zwave;
		}

		public async Task Start()
		{
			try
			{
				LoadState();
			}
			catch
			{
				Console.WriteLine("Could not load state, resetting...");
				Console.WriteLine();
				nodeStates = new Dictionary<byte, NodeState>();
			}

			var homeId = await z.GetHomeID();
			var nodeId = await z.GetNodeID();

			Console.WriteLine($"Home ID: {homeId}, Controller ID: {nodeId}");
			Console.WriteLine();

			nodes = await z.GetNodes();
			await ListNodes(nodes);
			await QueryNodes(nodes);

			Console.WriteLine("System ready");

			SubscribeAll(nodes);

			/* This is persisted now, but I keep it just so I don't forget and I reset the state
			nodeStates[1].Name = "Controller";
			nodeStates[18].Name = "Wallmote 1";
			nodeStates[23].Name = "Wallmote 2";
			nodeStates[13].Name = "Study light switch";
			nodeStates[10].Name = "Study bulb";
			nodeStates[14].Name = "Porch bulb";
			nodeStates[15].Name = "Patio bulb";
			nodeStates[16].Name = "Patio bulb";
			nodeStates[17].Name = "Patio bulb";
			nodeStates[24].Name = "Patio bulb";
			nodeStates[22].Name = "Sensor 1";
			nodeStates[25].Name = "Sensor 2";
			nodeStates[8].Name = "Motion sensor 1";
			nodeStates[11].Name = "Motion sensor 2";
			nodeStates[12].Name = "Motion sensor 3";
			nodeStates[2].Name = "Plug 1";
			*/

			wallmote1 = nodes[18].GetCommandClass<CentralScene>();
			wallmote2 = nodes[23].GetCommandClass<CentralScene>();
			studyLight = nodes[13].GetCommandClass<SwitchBinary>();
			studyBulb = nodes[10].GetCommandClass<SwitchMultiLevel>();
			porchBulb = nodes[14].GetCommandClass<SwitchMultiLevel>();
			patioBulbs = new List<SwitchMultiLevel> {
				nodes[15].GetCommandClass<SwitchMultiLevel>(),
				nodes[16].GetCommandClass<SwitchMultiLevel>(),
				nodes[17].GetCommandClass<SwitchMultiLevel>(),
				nodes[24].GetCommandClass<SwitchMultiLevel>(),
			};

			wallmote1.Changed += wallMote1Changed;
			wallmote2.Changed += wallMote2Changed;
		}

		public List<(NodeState, Node)> FindNodes(Predicate<NodeState> predicate)
		{
			return nodeStates.Where(x => predicate(x.Value)).Select(x => (x.Value, nodes[x.Key])).ToList();
		}

		public (NodeState, Node) GetNode(byte nodeID)
		{
			if (!nodeStates.ContainsKey(nodeID)) return (null,null);
			if (nodes[nodeID] == null) return (null, null);
			return (nodeStates[nodeID], nodes[nodeID]);
		}

		private void wallMote1Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			Console.WriteLine($"{DateTimeOffset.Now:t} Wallmote 1 button {e.Report.SceneNumber} pressed");

			if (e.Report.SceneNumber == 1 || e.Report.SceneNumber == 3)
			{
				studyLight.Set(e.Report.SceneNumber == 1);
				studyBulb.Set((byte)(e.Report.SceneNumber == 1? 255 : 0));
			}

			if (e.Report.SceneNumber == 2 || e.Report.SceneNumber == 4)
			{
				patioBulbs.ForEach(bulb => bulb.Set((byte)(e.Report.SceneNumber == 2 ? 255 : 0)));
			}
		}

		private void wallMote2Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			Console.WriteLine($"{DateTimeOffset.Now:t} Wallmote 2 button {e.Report.SceneNumber} pressed");

			if (e.Report.SceneNumber == 1 || e.Report.SceneNumber == 3)
			{
				porchBulb.Set((byte)(e.Report.SceneNumber == 1? 255 : 0));
			}

			if (e.Report.SceneNumber == 2 || e.Report.SceneNumber == 4)
			{
				patioBulbs.ForEach(bulb => bulb.Set((byte)(e.Report.SceneNumber == 2 ? 255 : 0)));
			}
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

					nodeStates.Add(n.NodeID, new NodeState());
				}
				var state = nodeStates[n.NodeID];

				if (state.FirstAdded == null) state.FirstAdded = DateTimeOffset.UtcNow;
				state.HasBattery = !protocolInfo.IsListening;
				state.GenericType = protocolInfo.GenericType;
			}

			SaveState();
		}

		void SubscribeAll(NodeCollection nodes)
		{
			foreach (var n in nodes)
			{
				Subscribe(n);
			}
		}

		async Task QueryNodes(NodeCollection nodes)
		{
			foreach (var n in nodes)
			{
				var state = nodeStates[n.NodeID];

				if (state.CommandClasses != null) continue;
				if (state.HasBattery)
				{
					Console.WriteLine($"Won't query node {n} until it wakes up");
					continue;
				}
				if (state.FailCount >= 5 && !state.LastFailed.IsOlderThan(TimeSpan.FromDays(1)))
				{
					Console.WriteLine($"Won't query node {n} because it has failed {state.FailCount} times, most recently {state.LastFailed.Ago()}");
					continue;
				}

				await QueryNode(n);
			}

			Console.WriteLine();
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

		private void Subscribe(Node node)
		{
			var basic = node.GetCommandClass<Basic>();
			basic.Changed += (_, e) => ReceiveReport(e.Report);

			var sensorMultiLevel = node.GetCommandClass<SensorMultiLevel>();
			sensorMultiLevel.Changed += (_, e) => ReceiveReport(e.Report);

			var meter = node.GetCommandClass<Meter>();
			meter.Changed += (_, e) => ReceiveReport(e.Report);

			var alarm = node.GetCommandClass<Alarm>();
			alarm.Changed += (_, e) => ReceiveReport(e.Report);

			var sensorBinary = node.GetCommandClass<SensorBinary>();
			sensorBinary.Changed += (_, e) => ReceiveReport(e.Report);

			var sensorAlarm = node.GetCommandClass<SensorAlarm>();
			sensorAlarm.Changed += (_, e) => ReceiveReport(e.Report);

			var wakeUp = node.GetCommandClass<WakeUp>();
			wakeUp.Changed += async (_, e) => {
				ReceiveReport(e.Report);

				var state = nodeStates[e.Report.Node.NodeID];
				state.LastWakeUp = DateTimeOffset.UtcNow;
				if (state.CommandClasses == null) await QueryNode(e.Report.Node);

				if (state.CommandClasses.Contains(ZWave.Channel.CommandClass.Battery) && (state.BatteryReport == null || state.BatteryReport.Timestamp.IsOlderThan(TimeSpan.FromDays(1))))
				{
					Console.WriteLine("{DateTimeOffset.Now:t} Requesting battery state...");
					node.GetCommandClass<Battery>().Get();
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
			Console.WriteLine($"{DateTimeOffset.Now:t} Report from node {r.Node}: {r.GetType().Name} [{r}]");

			lock (nodeStates)
			{
				var state = nodeStates[r.Node.NodeID];
				state.RecordContact();
			}

			SaveState();
		}

		void ReceiveBatteryReport(ZWave.CommandClasses.BatteryReport report)
		{
			var state = nodeStates[report.Node.NodeID];
			var r = new Report<BatteryReport>
			{
				Timestamp = DateTime.UtcNow,
				Data = new BatteryReport
				{
					IsLow = report.IsLow,
					Value = report.Value
				}
			};
			state.BatteryReport = r;
		}

		void SaveState()
		{
			lock (nodeStates)
			{
				var json = JsonConvert.SerializeObject(nodeStates);
				File.WriteAllText("state.json", json);
			}
		}

		void LoadState()
		{
			lock (nodeStates)
			{
				var json = File.ReadAllText("state.json");
				this.nodeStates = JsonConvert.DeserializeObject<Dictionary<byte, NodeState>>(json);
			}
		}
	}
}
