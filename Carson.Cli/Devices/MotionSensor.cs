using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Experiment1.ZWaveDrivers;

namespace Experiment1
{
	public class MotionEvent : IDeviceState
	{
		public MotionState State { get; set; }
		public DateTimeOffset FirstMotionTimeStamp { get; set; }
		public DateTimeOffset LastMotionTimeStamp { get; set; }
		public DateTimeOffset MotionCeasedTimeStamp { get; set; }
		public int Count { get; set; }

		public override string ToString()
		{
			switch (State)
			{
				case MotionState.Unknown:
					return "unknown";

				case MotionState.Motion:
					if (Count == 1) return $"motion since {FirstMotionTimeStamp:H:mm}";
					else return $"motion since {FirstMotionTimeStamp:H:mm} lasting {(DateTimeOffset.Now - FirstMotionTimeStamp).TotalMinutes:n1} minutes";

				case MotionState.NoMotion:
					return $"no motion (last motion started at {FirstMotionTimeStamp:H:mm} and ended at {LastMotionTimeStamp:H:mm} after {(LastMotionTimeStamp - FirstMotionTimeStamp).TotalMinutes:n1} minutes)";

				default: throw new NotSupportedException();
			}
		}
	}

	public interface IMotionSensor : IDevice
	{
		Action<IDevice, MotionEvent> OnMotionDetected { get; set; }
		Action<IDevice, MotionEvent> OnMotionCeased { get; set; }
		MotionEvent State { get; set; }
		TimeSpan Duration { get; set; }
	}

	public class MotionSensor : IMotionSensor
	{
		/// <summary>
		/// Called when the motion sensor detects motion.
		/// </summary>
		public Action<IDevice, MotionEvent> OnMotionDetected { get; set; }

		/// <summary>
		/// Called when motion ceases (once the timespan set
		/// by Duration has elapsed since the last motion event)
		/// </summary>
		public Action<IDevice, MotionEvent> OnMotionCeased { get; set; }

		/// <summary>
		/// The current state of the motion detector
		/// </summary>
		public MotionEvent State { get; set; }

		/// <summary>
		/// How long after the driver last reported motion the sensor will wait
		/// until returning to the NoMotion state.
		/// </summary>
		public TimeSpan Duration { get; set; }

		public string Name { get; set; }

		public string Plural { get; set; }

		public Area Area { get; set; }

		public bool IsDead { get; set; }

		readonly IZWaveAlarmDriver driver;
		CancellationTokenSource tokenSource;

		public MotionSensor(IZWaveAlarmDriver driver)
		{
			this.driver = driver;
			Duration = TimeSpan.FromMinutes(5);

			driver.OnChange = (x) =>
			{
				if (x) MotionDetected();
				else MotionCeased();
			};

			State = new MotionEvent { State = MotionState.Unknown };
		}

		void MotionDetected()
		{
			if (State.State != MotionState.Motion)
			{
				State = new MotionEvent { Count = 0, FirstMotionTimeStamp = DateTimeOffset.Now, State = MotionState.Motion };
			}

			State.Count++;
			State.LastMotionTimeStamp = DateTimeOffset.Now;

			OnMotionDetected?.Invoke(this, State);

			tokenSource?.Cancel();
			tokenSource = new CancellationTokenSource();

			var task = Task.Delay(Duration, tokenSource.Token).ContinueWith(x =>
			{
				if (!x.IsCanceled) MotionCeased(true);
			});
		}

		void MotionCeased(bool calledByDelay = false)
		{
			if (calledByDelay)
			{
				if (State.State != MotionState.NoMotion)
				{
					State.State = MotionState.NoMotion;
					State.MotionCeasedTimeStamp = DateTimeOffset.Now;
					OnMotionCeased?.Invoke(this, State);
				}
			}
		}

		public Task<List<IDeviceState>> GetState()
		{
			return Task.FromResult(new List<IDeviceState> { State });
		}
	}
}
