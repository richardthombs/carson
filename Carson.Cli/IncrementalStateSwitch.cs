using System;

namespace Experiment1
{
	class IncrementalStateSwitch
	{
		int state = 0;
		Cli cli;

		public IncrementalStateSwitch(Cli cli)
		{
			this.cli = cli;
		}

		public string[] StateCommands { get; set; }

		public void Inc()
		{
			cli.Execute(StateCommands[state], echo: true);
			state = Math.Min(state + 1, StateCommands.Length - 1);
		}

		public void Dec()
		{
			cli.Execute(StateCommands[state], echo: true);
			state = Math.Max(state - 1, 0);
		}
	}
}
