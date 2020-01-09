using System;
using System.Threading;

namespace Experiment1
{
	class BackgroundTask
	{
		public int TaskID;
		public string Command;
		public CancellationToken CancellationToken;
		public CancellationTokenSource CanncelationSource;
		public bool Completed;
		public bool Cancelled;
		public DateTimeOffset? SleepUntil;
	}
}
