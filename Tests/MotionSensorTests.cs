using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Experiment1.ZWaveDrivers;

namespace Experiment1.Tests
{
	public class DummyAlarmDriver : IZWaveAlarmDriver
	{
		public Action<bool> OnChange { get; set; }

		public Task<bool?> Get()
		{
			throw new NotImplementedException();
		}
	}

	[TestFixture]
	public class MotionSensorTests
	{
		[Test]
		public void MotionCeased_Is_Called_After_Delay()
		{
			int detectedCount = 0;
			int ceasedCount = 0;
			var driver = new DummyAlarmDriver();
			var sensor = new MotionSensor(driver) { Duration = TimeSpan.FromSeconds(1), OnMotionDetected = (d,v) => detectedCount++, OnMotionCeased = (d,v) => ceasedCount++ };

			driver.OnChange(true);

			Assert.AreEqual(1, detectedCount);
			Assert.AreEqual(0, ceasedCount);

			Thread.Sleep(TimeSpan.FromSeconds(2));

			Assert.AreEqual(1, detectedCount);
			Assert.AreEqual(1, ceasedCount);
		}

		[Test]
		public void Multiple_Alarms_Within_Duration_Raises_Only_One_MotionCeased()
		{
			int detectedCount = 0;
			int ceasedCount = 0;
			var driver = new DummyAlarmDriver();
			var sensor = new MotionSensor(driver) { Duration = TimeSpan.FromSeconds(2), OnMotionDetected = (d,v) => detectedCount++, OnMotionCeased = (d,v) => ceasedCount++ };

			driver.OnChange(true);
			Assert.AreEqual(0, ceasedCount);

			Thread.Sleep(TimeSpan.FromSeconds(1));
			driver.OnChange(true);
			Assert.AreEqual(0, ceasedCount);

			Thread.Sleep(TimeSpan.FromSeconds(1));
			Assert.AreEqual(0, ceasedCount);

			Thread.Sleep(TimeSpan.FromSeconds(3));
			Assert.AreEqual(1, ceasedCount);
		}

		[Test]
		public void Multiple_Alarms_Within_Duration_Raises_Only_One_MotionDetected()
		{
			int detectedCount = 0;
			int ceasedCount = 0;
			var driver = new DummyAlarmDriver();
			var sensor = new MotionSensor(driver) { Duration = TimeSpan.FromSeconds(2), OnMotionDetected = (d,v) => detectedCount++, OnMotionCeased = (d,v) => ceasedCount++ };

			driver.OnChange(true);
			driver.OnChange(true);
			Assert.AreEqual(1, detectedCount);

			Thread.Sleep(TimeSpan.FromSeconds(4));
			driver.OnChange(true);
			Assert.AreEqual(2, detectedCount);
		}

		[Test]
		public void Multiple_Pairs_Of_Detected_And_Ceased_Raises_Only_One_Set_Of_Events()
		{
			int detectedCount = 0;
			int ceasedCount = 0;
			var driver = new DummyAlarmDriver();
			var sensor = new MotionSensor(driver) { Duration = TimeSpan.FromSeconds(3), OnMotionDetected = (d,v) => detectedCount++, OnMotionCeased = (d,v) => ceasedCount++ };

			driver.OnChange(true);
			Thread.Sleep(TimeSpan.FromSeconds(1));
			driver.OnChange(false);
			Thread.Sleep(TimeSpan.FromSeconds(1));
			driver.OnChange(true);
			Thread.Sleep(TimeSpan.FromSeconds(1));
			driver.OnChange(false);
			Thread.Sleep(TimeSpan.FromSeconds(1));
			driver.OnChange(true);
			Thread.Sleep(TimeSpan.FromSeconds(1));
			driver.OnChange(false);

			Thread.Sleep(TimeSpan.FromSeconds(5));

			Assert.AreEqual(1, detectedCount);
			Assert.AreEqual(1, ceasedCount);
		}

		[Test]
		public void Delay1()
		{
			var stopwatch = new System.Diagnostics.Stopwatch();

			stopwatch.Start();
			Task.Delay(2000).ContinueWith((x) => {
				stopwatch.Stop();
				Console.WriteLine(stopwatch.Elapsed.ToString());
			});
			Thread.Sleep(5000);
		}
	}
}
