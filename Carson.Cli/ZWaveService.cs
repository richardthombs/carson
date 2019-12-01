using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using ZWave;
using ZWave.CommandClasses;
using ZWave.Channel;

namespace Experiment1
{
	public class SuperNode
	{
		public Node Node { get; set; }
		public List<CommandClass> Classes { get; set; }
		public bool? HasBattery { get; set; }
		public BatteryReport Battery { get; set; }
		public TimeSpan? WakeUpInterval { get; set; }
		public int UnresponsiveCount { get; set; }

		public bool Supports(CommandClass c)
		{
			return Classes != null && Classes.Exists(x => x == c);
		}
	}

	public class ZWaveService
	{
		string portName;
		ZWaveController controller;
		ILogService log;

		public bool Verbose;

		public List<SuperNode> Nodes { get; private set; }

		public ZWaveChannel Channel { get { return controller.Channel; } }

		public ZWaveService(string portName, ILogService logSvc)
		{
			this.portName = portName;
			this.log = logSvc;
		}

		public void Start()
		{
			controller = new ZWaveController(portName);
			//controller.Channel.Log = new System.IO.StreamWriter(Console.OpenStandardOutput());
			controller.Channel.MaxRetryCount = 1;
			controller.Channel.ResponseTimeout = TimeSpan.FromSeconds(10);
			controller.Channel.ReceiveTimeout = TimeSpan.FromSeconds(10);
			controller.Open();
			Init().Wait();
		}

		public void Stop()
		{
			controller.Close();
		}

		public SuperNode GetNode(int nodeID)
		{
			return Nodes.First(x => x.Node.NodeID == nodeID);
		}

		async Task Init()
		{
			log.Write($"Version: {await controller.GetVersion()}");
			log.Write($"HomeID: {await controller.GetHomeID():X}");

			Nodes = new List<SuperNode>();
			var nodes = await controller.GetNodes();
			log.Write($"{nodes.Count()} nodes detected");

			foreach (var node in nodes)
			{
				var nodeInfo = new SuperNode { Node = node };
				Nodes.Add(nodeInfo);

				var protocolInfo = await node.GetProtocolInfo();
				log.Write($"Node {node}: Generic = {protocolInfo.GenericType}, Specific = 0x{protocolInfo.SpecificType:X2} Basic = {protocolInfo.BasicType}, Listening = {protocolInfo.IsListening} ");

				var neighbours = await node.GetNeighbours();
				if (neighbours.Length > 0)
				{
					log.Write($"Node {node}: Connected to: {String.Join(", ", neighbours.Select(x => $"Node {x}").ToArray())}");
				}

				if (protocolInfo.IsListening)
				{
					await UpdateCommandClasses(nodeInfo);
					await UpdateBattery(nodeInfo);
				}
				else
				{
					var wakeUp = node.GetCommandClass<WakeUp>();
					wakeUp.Changed += async (s, e) => await WakeUp_Changed(s, e);
				}

				Subscribe(node);
				log.Write();
			}
		}

		async Task UpdateCommandClasses(SuperNode info)
		{
			try
			{
				return;
				var classes = await info.Node.GetSupportedCommandClasses();
				foreach (var c in classes)
				{
					info.Classes.Add(c.Class);
					log.Write($"{c.Class} v{c.Version}");
				}
			}
			catch (Exception ex)
			{
				log.Write(ex.Message);
				info.UnresponsiveCount++;
			}
		}

		Task UpdateBattery(SuperNode info)
		{
			info.HasBattery = info.Supports(CommandClass.Battery);

			if (info.HasBattery ?? false)
			{
				log.Write($"Node {info.Node} has a battery");
				info.Node.GetCommandClass<Battery>().Changed += (s, f) =>
				{
					log.Write($"Battery report received from Node {info.Node}: {f.Report}");
					info.Battery = f.Report;
				};
			}

			return Task.CompletedTask;
		}

		private async Task WakeUp_Changed(object sender, ReportEventArgs<WakeUpReport> e)
		{
			if (e.Report.Awake)
			{
				log.Write($"Node {e.Report.Node:D3} has woken up");

				var info = Nodes.Find(x => x.Node == e.Report.Node);

				if (info.WakeUpInterval == null)
				{
					var wakeup = e.Report.Node.GetCommandClass<WakeUp>();
					var report = await wakeup.GetInterval();
					log.Write($"Node {wakeup.Node} will wake up every {report.Interval:g}");
					info.WakeUpInterval = report.Interval;
				}

				if (info.Classes == null)
				{
					await UpdateCommandClasses(info);
					await UpdateBattery(info);
				}

				if (info.HasBattery ?? false)
				{
					var batteryReport = await info.Node.GetCommandClass<Battery>().Get();
					info.Battery = batteryReport;
					log.Write($"Battery report fetched from Node {info.Node}: {batteryReport}");
				}
			}
		}

		void Subscribe(Node node)
		{
			var basic = node.GetCommandClass<Basic>();
			basic.Changed += (_, e) => { if (Verbose) log.Write($"Basic report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var sensorMultiLevel = node.GetCommandClass<SensorMultiLevel>();
			sensorMultiLevel.Changed += (_, e) => { if (Verbose) log.Write($"SensorMultiLevel report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var meter = node.GetCommandClass<Meter>();
			meter.Changed += (_, e) => { if (Verbose) log.Write($"Meter report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var alarm = node.GetCommandClass<Alarm>();
			alarm.Changed += (_, e) => { if (Verbose) log.Write($"Alarm report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var sensorBinary = node.GetCommandClass<SensorBinary>();
			sensorBinary.Changed += (_, e) => { if (Verbose) log.Write($"SensorBinary report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var sensorAlarm = node.GetCommandClass<SensorAlarm>();
			sensorAlarm.Changed += (_, e) => { if (Verbose) log.Write($"SensorAlarm report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var wakeUp = node.GetCommandClass<WakeUp>();
			wakeUp.Changed += (_, e) => { if (Verbose) log.Write($"WakeUp report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var switchBinary = node.GetCommandClass<SwitchBinary>();
			switchBinary.Changed += (_, e) => { if (Verbose) log.Write($"SwitchBinary report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var thermostatSetpoint = node.GetCommandClass<ThermostatSetpoint>();
			thermostatSetpoint.Changed += (_, e) => { if (Verbose) log.Write($"thermostatSetpoint report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var sceneActivation = node.GetCommandClass<SceneActivation>();
			sceneActivation.Changed += (_, e) => { if (Verbose) log.Write($"sceneActivation report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };

			var battery = node.GetCommandClass<Battery>();
			battery.Changed += (_, e) => { if (Verbose) log.Write($"battery report of Node { e.Report.Node:D3} changed to [{e.Report}]"); };

			node.UnknownCommandReceived += (_, e) => { if (Verbose) log.Write($"Unknown command {e.Command.CommandID} received from node {e.NodeID}"); };

			var scene = node.GetCommandClass<CentralScene>();
			scene.Changed += (_, e) => { if (Verbose) log.Write($"CentralScene report of Node {e.Report.Node:D3} changed to [{e.Report}]"); };
		}
	}
}
