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

		public void Trigger()
		{
			cli.Execute(StateCommands[state]);
			if (++state >= StateCommands.Length) state = 0;
		}
	}
}
