using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Experiment1
{
	public class CommandParser
	{
		public List<string> Patterns { get; set; }
		public List<string> Areas { get; set; }
		public List<string> Devices { get; set; }

		public CommandMatch Parse(string command)
		{
			var simplified = command.Replace(" the ", " ");

			foreach (var pattern in Patterns)
			{
				var re = ConvertToRegex(pattern);

				if (re.Contains("{area}") && !re.Contains("{device}"))
				{
					foreach (var area in Areas)
					{
						var replaced = re
							.Replace("{area}", area);

						var regex = new Regex("^" + replaced + "$");
						var match = regex.Match(simplified);
						if (match.Success)
						{
							return new CommandMatch
							{
								Pattern = pattern,
								Parameters = new Dictionary<string, string>
								{
									{ "area", area },
									{ "value", match.Groups["value"].Value }
								}
							};
						}
					}
				}
				else if (re.Contains("{device}") && !re.Contains("{area}"))
				{
					foreach (var device in Devices)
					{
						var replaced = re
							.Replace("{device}", device);

						var regex = new Regex("^" + replaced + "$");
						var match = regex.Match(simplified);
						if (match.Success)
						{
							return new CommandMatch
							{
								Pattern = pattern,
								Parameters = new Dictionary<string, string>
								{
									{ "device", device },
									{ "value", match.Groups["value"].Value }
								}
							};

						}
					}
				}
				else if (re.Contains("{area}") && re.Contains("{device}"))
				{
					foreach (var area in Areas)
					{
						foreach (var device in Devices)
						{
							var replaced = re
								.Replace("{area}", area)
								.Replace("{device}", device);

							var regex = new Regex("^" + replaced + "$");
							var match = regex.Match(simplified);
							if (match.Success)
							{
								return new CommandMatch
								{
									Pattern = pattern,
									Parameters = new Dictionary<string, string>
									{
										{ "area", area },
										{ "device", device },
										{ "value", match.Groups["value"].Value }
									}
								};
							}
						}
					}
				}
				else
				{
					var replaced = re;

					var regex = new Regex("^" + replaced + "$");
					var match = regex.Match(simplified);
					if (match.Success)
					{
						return new CommandMatch
						{
							Pattern = pattern,
							Parameters = new Dictionary<string, string>
							{
								{ "value", match.Groups["value"].Value },
								{ "group", match.Groups["group"].Value },
								{ "node", match.Groups["node"].Value },
								{ "param", match.Groups["param"].Value }
							}
						};
					}
				}
			}

			return null;
		}

		string ConvertToRegex(string pattern)
		{
			var regex = @"\{([a-z ]+)(?::((?:[a-z]+)(?:\|[a-z]+)+))?\}";

			var re = new Regex(regex);
			var matches = re.Matches(pattern);
			var replaced = re.Replace(pattern, x =>
			{
				if (x.Value == "{area}" || x.Value == "{device}") return x.Value;

				if (x.Groups[2].Success) return $"(?<{x.Groups[1].Value}>{x.Groups[2].Value})";
				else return $"(?<{x.Groups[1].Value}>[a-z0-9]+)";
			});

			return replaced;
		}
	}

	public class CommandMatch
	{
		public string Pattern { get; set; }
		public Dictionary<string, string> Parameters { get; set; }
	}
}
