using System;

namespace Experiment1
{
	class IncrementalStateSwitch
	{
		int nextCommand = 0;
		Cli cli;

		public IncrementalStateSwitch(Cli cli)
		{
			this.cli = cli;
		}

		public string[] StateCommands { get; set; }
		public string ResetCommand { get; set; }

		public void Trigger()
		{
			cli.Execute(StateCommands[nextCommand], echo: true);
			nextCommand = (nextCommand + 1) % StateCommands.Length;
		}

		public void Reset()
		{
			cli.Execute(ResetCommand, echo: true);
			nextCommand = 0;
		}
	}
}
