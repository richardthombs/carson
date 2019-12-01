using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Experiment1
{
	public class CommandRunner
	{
		CommandParser parser;
		Environment env;
		
		public CommandRunner(CommandParser parser, Environment env)
		{
			this.parser = parser;
			this.env = env;
		}

		public async Task ExecuteCommand(string cmd, bool acknowledge = false)
		{
			var match = parser.Parse(cmd);

			if (match == null)
			{
				Console.WriteLine("What?");
				return;
			}

			var command = env.Vocab.Find(x => x.Patterns.Contains(match.Pattern));
			if (command != null)
			{
				try
				{
					await command.Action(env, match);
				}
				catch (AggregateException agg)
				{
					Console.WriteLine(agg.InnerExceptions[0].ToString());
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
				if (acknowledge) Console.WriteLine("OK");
			}
			else
			{
				Console.WriteLine("What??");
			}
		}
	}

}
