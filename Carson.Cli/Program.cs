using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using ZWave;
using Newtonsoft.Json.Converters;

namespace Experiment1
{
	class Program
	{
		static void Main(string[] args)
		{
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

			Console.ReadKey();
			zwave.Close();
		}
	}
}
