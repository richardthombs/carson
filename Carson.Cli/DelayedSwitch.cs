using System;
using System.Threading;
using System.Threading.Tasks;

namespace Experiment1
{
	class DelayedSwitch
	{
		public string TriggerCommand { get; set; }
		public string ResetCommand { get; set; }
		public TimeSpan ResetDelay { get; set; }

		CancellationTokenSource tokenSource;
		Cli cli;

		public DelayedSwitch(Cli cli)
		{
			this.cli = cli;
			ResetDelay = TimeSpan.FromMinutes(5);
		}

		public void Trigger()
		{
			if (TriggerCommand != null) cli.Execute(TriggerCommand);

			tokenSource?.Cancel();
			tokenSource = new CancellationTokenSource();

			var task = Task.Delay(ResetDelay, tokenSource.Token).ContinueWith(x =>
			{
				if (!x.IsCanceled && ResetCommand != null) cli.Execute(ResetCommand);
			});
		}
	}
}
