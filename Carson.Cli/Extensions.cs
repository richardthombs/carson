using System;

namespace Experiment1
{
	static class Extensions
	{
		public static bool IsOlderThan(this DateTimeOffset timestamp, TimeSpan value)
		{
			return (DateTimeOffset.UtcNow - timestamp) > value;
		}

		public static bool IsOlderThan(this DateTimeOffset? timestamp, TimeSpan value)
		{
			if (!timestamp.HasValue) return true;

			return timestamp.Value.IsOlderThan(value);
		}

		public static string In(this TimeSpan span)
		{
			if (span.Days >= 7) return $"in {dayPlural(span.Days)}";

			if (span.Days >= 1) return $"in {dayPlural(span.Days)}, {hourPlural(span.Hours)}";

			if (span.Hours >= 1) return $"in {hourPlural(span.Hours)}, {minutePlural(span.Minutes)}";

			if (span.Minutes >= 1) return $"in {minutePlural(span.Minutes)}";

			if (span.Seconds >= 1) return $"in {secondPlural(span.Seconds)}";

			return "right now";
		}

		public static string In(this DateTimeOffset when)
		{
			var now = DateTimeOffset.UtcNow;
			var span = when - now;

			if (now.AddDays(1).Date == when.Date) return $"tomorrow at {when:t}";
			if (span.Days >= 7) return $"{when:d} at {when:t}";
			if (span.Days >= 1) return $"this {when:dddd} at {when:t}";

			return In(span);
		}

		public static string Ago(this DateTimeOffset timestamp)
		{
			var span = DateTimeOffset.Now - timestamp;

			if (span.Days >= 28) return timestamp.ToString("d");

			if (span.Days >= 7) return $"{dayPlural(span.Days)} ago";

			if (span.Days >= 1) return $"{dayPlural(span.Days)}, {hourPlural(span.Hours)} ago";

			if (span.Hours >= 1) return $"{hourPlural(span.Hours)}, {minutePlural(span.Minutes)} ago";

			if (span.Minutes >= 1) return $"{minutePlural(span.Minutes)} ago";

			if (span.Seconds >= 1) return $"{secondPlural(span.Seconds)} ago";

			return "just now";
		}

		public static string Ago(this DateTimeOffset? timestamp)
		{
			if (!timestamp.HasValue) return "never";
			return timestamp.Value.Ago();
		}

		static string dayPlural(int days)
		{
			return days == 1 ? "1 day" : $"{days} days";
		}

		static string hourPlural(int hours)
		{
			return hours == 1 ? "1 hour" : $"{hours} hours";
		}

		static string minutePlural(int minutes)
		{
			return minutes == 1 ? "1 minute" : $"{minutes} minutes";
		}

		static string secondPlural(int minutes)
		{
			return minutes == 1 ? "1 second" : $"{minutes} seconds";
		}
	}
}
