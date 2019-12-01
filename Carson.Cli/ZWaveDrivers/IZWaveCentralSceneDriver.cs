using System;

namespace Experiment1.ZWaveDrivers
{
	public interface IZWaveCentralSceneDriver
	{
		Action<byte> OnCentralScene { get; set; }
	}
}