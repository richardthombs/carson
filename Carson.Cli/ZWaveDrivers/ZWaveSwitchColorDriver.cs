using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveSwitchColorDriver : IZWaveSwitchColorDriver
	{
		Node node;
		string state;

		public ZWaveSwitchColorDriver(Node node)
		{
			this.node = node;
		}

		public async Task Set(string value)
		{
			var sw = node.GetCommandClass<Color>();

			var components = GetColorComponents(value);
			await sw.Set(components);

			state = value;
		}

		public Task<string> Get()
		{
			return Task.FromResult(state);
		}

		public static ColorComponent[] GetColorComponents(string colourName)
		{
			var colour = System.Drawing.ColorTranslator.FromHtml(colourName);

			switch (colourName)
			{
				case "white":
					return new ColorComponent[] { new ColorComponent(0, 255), new ColorComponent(1, 0), new ColorComponent(2, 0), new ColorComponent(3, 0), new ColorComponent(4, 0) };

				default:
					return new ColorComponent[] { new ColorComponent(0, 0), new ColorComponent(1, 0), new ColorComponent(2, colour.R), new ColorComponent(3, colour.G), new ColorComponent(4, colour.B) };
			}
		}
	}
}
