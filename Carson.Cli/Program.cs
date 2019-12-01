using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

using ZWave.Channel;

using Experiment1.ZWaveDrivers;

namespace Experiment1
{
	class Program
	{
		static ILogService log;

		static void Main(string[] args)
		{
			log = new ConsoleLogger();
			var env = new Environment
			{
				Vocab = CommandLibrary.GetVocab(),
				Areas = new AreaCollection(),
				Write = (x) => Console.WriteLine(x)
			};

			env.Areas.Create("downstairs");
			env.Areas.Create("snug");
			env.Areas.Create("study");
			env.Areas.Create("kitchen");
			env.Areas.Create("dining hall");
			env.Areas.Add("downstairs", "study", "snug", "kitchen", "dining hall");

			env.Areas.Create("upstairs");
			env.Areas.Create("master bedroom");
			env.Areas.Add("upstairs", "master bedroom");

			env.Areas.Create("house");
			env.Areas.Add("house", "upstairs", "downstairs");

			env.Areas.Create("outside");
			env.Areas.Create("porch");
			env.Areas.Create("patio");
			env.Areas.Add("outside", "porch", "patio");

			try
			{
				env.ZWaveService = new ZWaveService("COM3", log);
				env.ZWaveService.Start();
				CreateAndAddDevices(env);
				RunCommandLine(env).Wait();
				env.ZWaveService.Stop();
			}
			catch (AggregateException ex)
			{
				Console.WriteLine(ex.InnerExceptions[0]);
				Console.ReadKey();
			}
		}

		static void CreateAndAddDevices(Environment env)
		{
			var plug2 = new Light
			{
				Name = "plug",
				Plural = "plugs",
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(2).Node,
					Classes = new List<CommandClass> { CommandClass.Basic }
				}
			};

			var zipato10 = new Light
			{
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(10).Node,
					Classes = new List<CommandClass> { CommandClass.Basic, CommandClass.Color, CommandClass.SwitchMultiLevel, CommandClass.Battery, CommandClass.WakeUp }
				},
				Name = "light",
				Plural = "lights"
			};

			var everspring8 = new MotionSensor(new ZWaveAlarmDriver(env.ZWaveService.GetNode(8).Node))
			{
				Name = "motion",
				Plural = "motion sensors",
				OnMotionDetected = async (d, v) =>
				{
					LogMotionDetected(d, v, speak: true);
					await env.Runner.ExecuteCommand("turn porch lights on", acknowledge: false);
				},
				OnMotionCeased = async (d, v) =>
				{
					LogMotionCeased(d, v);
					await env.Runner.ExecuteCommand("turn porch lights off", acknowledge: false);
				},
				Duration = TimeSpan.FromMinutes(5)
			};

			var everspring12 = new MotionSensor(new ZWaveAlarmDriver(env.ZWaveService.GetNode(12).Node))
			{
				Name = "motion",
				Plural = "motion sensors",
				OnMotionDetected = async (d, v) =>
				{
					LogMotionDetected(d, v);
					await env.Runner.ExecuteCommand("turn patio lights on", acknowledge: false);
				},
				OnMotionCeased = async (d, v) =>
				{
					LogMotionCeased(d, v);
					await env.Runner.ExecuteCommand("turn patio lights off", acknowledge: false);
				},
				Duration = TimeSpan.FromMinutes(5)
			};

			var everspring11 = new MotionSensor(new ZWaveAlarmDriver(env.ZWaveService.GetNode(11).Node))
			{
				Name = "motion",
				Plural = "motion sensors",
				OnMotionDetected = (d, v) => LogMotionDetected(d, v),
				OnMotionCeased = (d, v) => LogMotionCeased(d, v),
				Duration = TimeSpan.FromMinutes(5)
			};

			var switch13 = new Light
			{
				Name = "light",
				Plural = "lights",
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(13).Node,
					Classes = new List<CommandClass> { CommandClass.Basic }
				}
			};

			var temp21 = new TemperatureSensor(new ZWaveTemperatureDriver(env.ZWaveService.GetNode(21).Node))
			{
				Name = "temperature",
				Plural = "temperatures",
				OnUpdate = (d, v) => LogTemperature(d, v)
			};

			var temp22 = new TemperatureSensor(new ZWaveTemperatureDriver(env.ZWaveService.GetNode(22).Node))
			{
				Name = "temperature",
				Plural = "temperatures",
				OnUpdate = (d, v) => LogTemperature(d, v)
			};

			var sensor21_motion = new MotionSensor(new ZWaveAlarmDriver(env.ZWaveService.GetNode(21).Node))
			{
				Name = "motion",
				Plural = "motion sensors",
				OnMotionDetected = (d, v) => LogMotionDetected(d, v),
				OnMotionCeased = (d, v) => LogMotionCeased(d, v),
				Duration = TimeSpan.FromMinutes(5)
			};

			var sensor22_motion = new MotionSensor(new ZWaveAlarmDriver(env.ZWaveService.GetNode(22).Node))
			{
				Name = "motion",
				Plural = "motion sensors",
				OnMotionDetected = (d, v) => LogMotionDetected(d, v),
				OnMotionCeased = (d, v) => LogMotionCeased(d, v),
				Duration = TimeSpan.FromMinutes(5)
			};

			var zipato14 = new Light
			{
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(14).Node,
					Classes = new List<CommandClass> { CommandClass.Basic, CommandClass.Color, CommandClass.SwitchMultiLevel, CommandClass.Battery, CommandClass.WakeUp }
				},
				Name = "light",
				Plural = "lights"
			};

			var zipato15 = new Light
			{
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(15).Node,
					Classes = new List<CommandClass> { CommandClass.Basic, CommandClass.Color, CommandClass.SwitchMultiLevel, CommandClass.Battery, CommandClass.WakeUp }
				},
				Name = "light",
				Plural = "lights"
			};

			var zipato16 = new Light
			{
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(16).Node,
					Classes = new List<CommandClass> { CommandClass.Basic, CommandClass.Color, CommandClass.SwitchMultiLevel, CommandClass.Battery, CommandClass.WakeUp }
				},
				Name = "light",
				Plural = "lights"
			};

			var zipato17 = new Light
			{
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(17).Node,
					Classes = new List<CommandClass> { CommandClass.Basic, CommandClass.Color, CommandClass.SwitchMultiLevel, CommandClass.Battery, CommandClass.WakeUp }
				},
				Name = "light",
				Plural = "lights"
			};

			var zipato24 = new Light
			{
				Node = new SuperNode
				{
					Node = env.ZWaveService.GetNode(24).Node,
					Classes = new List<CommandClass> { CommandClass.Basic, CommandClass.Color, CommandClass.SwitchMultiLevel, CommandClass.Battery, CommandClass.WakeUp }
				},
				Name = "light",
				Plural = "lights"
			};

			var wallmote18 = new GenericDevice
			{
				Name = "wallmote",
				Plural = "wallmotes",
				CentralScene = new ZWaveCentralSceneDriver(env.ZWaveService.GetNode(18).Node)
				{
					OnCentralScene = async (x) =>
					{
						switch (x)
						{
							case 1:
								await env.Runner.ExecuteCommand("turn study lights on", acknowledge: false);
								log.Speak("study lights on");
								break;

							case 3:
								await env.Runner.ExecuteCommand("turn study lights off", acknowledge: false);
								log.Speak("study lights off");
								break;

							case 2:
								await env.Runner.ExecuteCommand("turn patio lights on", acknowledge: false);
								log.Speak("patio lights on");
								break;

							case 4:
								await env.Runner.ExecuteCommand("turn patio lights off", acknowledge: false);
								log.Speak("patio lights off");
								break;
						}
					}
				}
			};

			var wallmote23 = new GenericDevice
			{
				Name = "wallmote",
				Plural = "wallmotes",
				CentralScene = new ZWaveCentralSceneDriver(env.ZWaveService.GetNode(23).Node)
				{
					OnCentralScene = async (x) =>
					{
						switch (x)
						{
							case 1:
								await env.Runner.ExecuteCommand("turn porch lights on", acknowledge: false);
								log.Speak("porch lights on");
								break;

							case 3:
								await env.Runner.ExecuteCommand("turn porch lights off", acknowledge: false);
								log.Speak("porch lights off");
								break;

							case 2:
								await env.Runner.ExecuteCommand("turn patio lights on", acknowledge: false);
								log.Speak("patio lights on");
								break;

							case 4:
								await env.Runner.ExecuteCommand("turn patio lights off", acknowledge: false);
								log.Speak("patio lights off");
								break;
						}
					}
				}
			};
			env.Areas.AddDevice("kitchen", plug2, temp21, sensor21_motion, wallmote23);
			env.Areas.AddDevice("study", wallmote18, zipato10, switch13);
			env.Areas.AddDevice("porch", everspring8, zipato14);
			env.Areas.AddDevice("snug", everspring11);
			env.Areas.AddDevice("dining hall", temp22, sensor22_motion);
			env.Areas.AddDevice("patio", zipato15, zipato16, zipato17, zipato24, everspring12);
		}

		static void LogMotionDetected(IDevice d, MotionEvent v, bool speak = false)
		{
			if (v.Count == 1)
			{
				var message = $"Motion detected in {d.Area.Name}";
				if (speak) log.Speak(message);
				else log.Write(message);
			}
		}

		static void LogMotionCeased(IDevice d, MotionEvent v, bool speak=false)
		{
			var message = $"Motion ceased in {d.Area.Name} after {(v.LastMotionTimeStamp - v.FirstMotionTimeStamp).TotalMinutes:n1} minutes";

			if (speak) log.Speak(message);
			else log.Write(message);
		}

		static void LogTemperature(IDevice d, SimpleSensorState<float> v)
		{
			//log.Write($"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(d.Area.Name)} temperature is {v}");
		}

		static async Task RunCommandLine(Environment env)
		{
			log.Speak("System ready");
			log.Write();

			env.Runner = CreateRunner(env);

			while (!env.Quit)
			{
				var cmd = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(cmd)) continue;

				// Recreating the parser each time is a hack to allow me to add new areas and devices at runtime
				env.Runner = CreateRunner(env);

				await env.Runner.ExecuteCommand(cmd);
			}
		}

		static CommandRunner CreateRunner(Environment env)
		{
			var parser = new CommandParser
			{
				Patterns = env.Vocab.SelectMany(x => x.Patterns).ToList(),
				Areas = env.Areas.Root.GetAreas().Select(x => x.Name).ToList(),
				Devices = env.Areas.Root.GetDevices().SelectMany(x => new List<string> { x.Name, x.Plural }).ToList()
			};
			return new CommandRunner(parser, env);
		}
	}
}
