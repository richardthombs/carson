using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Experiment1
{
	class Command
	{
		public string Pattern;
		public Action<Dictionary<string, string>, BackgroundTask> Action;
	}

	class CommandParser
	{
		List<Command> grammar;

		public CommandParser(List<Command> grammar)
		{
			this.grammar = grammar;
		}

		public Action Parse(string command, BackgroundTask task)
		{
			foreach (var c in grammar)
			{
				var parameterRegex = new Regex(@"\{([\w]+)\}");
				var commandPattern = "^" + parameterRegex.Replace(c.Pattern, @"([\w\d': ]+)") + "$";
				var regex = new Regex(commandPattern, RegexOptions.IgnoreCase);
				var match = regex.Match(command);
				if (match.Success)
				{
					var placeholders = GetPlaceholders(c.Pattern);
					var dictionary = new Dictionary<string, string>();
					for (int m = 0; m < match.Groups.Count-1; m++)
					{
						dictionary.Add(placeholders[m], match.Groups[m+1].Value);
					}
					return () => c.Action(dictionary, task);
				}
			}

			return null;
		}

		List<string> GetPlaceholders(string pattern)
		{
			var regex = new Regex(@"\{([\w]+)\}");
			var matches = regex.Matches(pattern);

			var names = new List<string>();
			foreach (Match m in matches)
			{
				names.Add(m.Groups[1].Value);
			}
			return names;
		}
	}
}
