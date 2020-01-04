using System;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveCentralSceneDriver : IZWaveCentralSceneDriver
	{
		Node node;
		CentralScene centralScene;

		public ZWaveCentralSceneDriver(Node node)
		{
			this.node = node;
			this.centralScene = node.GetCommandClass<CentralScene>();
			centralScene.Changed += CentralScene_Changed;
		}

		public Action<byte> OnCentralScene { get; set; }

		private void CentralScene_Changed(object sender, ReportEventArgs<CentralSceneReport> e)
		{
			OnCentralScene?.Invoke(e.Report.SceneNumber);
		}
	}
}
