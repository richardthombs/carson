using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Experiment1
{
	public class Command
	{
		public List<string> Patterns { get; set; }
		public Func<Environment, CommandMatch, Task> Action { get; set; }
	}
}
