using System;
using System.Collections.Generic;

namespace Experiment1
{
	public class Environment
	{
		public AreaCollection Areas { get; set; }
		public List<SuperNode> Nodes { get; set; }
		public ZWaveService ZWaveService { get; set; }
		public CommandRunner Runner { get; set; }
		public List<Command> Vocab { get; set; }
		public bool Quit { get; set; }
		public Action<string> Write { get; set; }
	}

}
