using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Experiment1.Tests
{
	[TestFixture]
	public class CommandParserTests
	{
		[Test]
		public void Area_Is_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var pattern = "show {area}";
			var parser = new CommandParser { Patterns = new List<string> { pattern }, Areas = areas };

			// Act
			var match = parser.Parse("show the study");

			// Assert
			Assert.IsNotNull(match);
			Assert.AreEqual(pattern, match.Pattern);
			Assert.AreEqual("study", match.Parameters["area"]);
		}

		[Test]
		public void Device_Is_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var devices = new List<string> { "light", "camera" };
			var pattern = "show {device}";
			var parser = new CommandParser { Patterns = new List<string> { pattern }, Areas = areas, Devices = devices };

			// Act
			var match = parser.Parse("show the camera");

			// Assert
			Assert.IsNotNull(match);
			Assert.AreEqual(pattern, match.Pattern);
			Assert.AreEqual("camera", match.Parameters["device"]);
		}

		[Test]
		public void Area_And_Device_Are_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var devices = new List<string> { "light", "camera" };
			var pattern = "show {device} in {area}";
			var parser = new CommandParser { Patterns = new List<string> { pattern }, Areas = areas, Devices = devices };

			// Act
			var match = parser.Parse("show the camera in the study");

			// Assert
			Assert.IsNotNull(match);
			Assert.AreEqual(pattern, match.Pattern);
			Assert.AreEqual("camera", match.Parameters["device"]);
			Assert.AreEqual("study", match.Parameters["area"]);
		}

		[Test]
		public void Multiple_Patterns_Are_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var devices = new List<string> { "light", "camera" };
			var patterns = new List<string>
			{
				"show {device}",
				"show {area}",
				"show {area} {device}",
				"show {device} in {area}"
			};
			var parser = new CommandParser { Patterns = patterns, Areas = areas, Devices = devices };

			// Act & Assert
			var match1 = parser.Parse("show camera");
			Assert.AreEqual("show {device}", match1.Pattern);

			var match2 = parser.Parse("show bedroom");
			Assert.AreEqual("show {area}", match2.Pattern);

			var match3 = parser.Parse("show bedroom light");
			Assert.AreEqual("show {area} {device}", match3.Pattern);

			var match4 = parser.Parse("show camera in study");
			Assert.AreEqual("show {device} in {area}", match4.Pattern);
		}

		[Test]
		public void Enum_Patterns_Are_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var devices = new List<string> { "light", "camera" };
			var pattern = "turn {area} {device} {value:on|off}";
			var parser = new CommandParser { Patterns = new List<string> { pattern }, Areas = areas, Devices = devices };

			// Act
			var match = parser.Parse("turn the study light on");

			// Assert
			Assert.IsNotNull(match);
			Assert.AreEqual(pattern, match.Pattern);
			Assert.AreEqual("study", match.Parameters["area"]);
			Assert.AreEqual("light", match.Parameters["device"]);
			Assert.AreEqual("on", match.Parameters["value"]);
		}

		[Test]
		public void Invalid_Enum_Values_Are_Not_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var devices = new List<string> { "light", "camera" };
			var pattern = "turn {area} {device} {value:on|off}";
			var parser = new CommandParser { Patterns = new List<string> { pattern }, Areas = areas, Devices = devices };

			// Act
			var match = parser.Parse("turn the study light foo");

			// Assert
			Assert.IsNull(match);
		}

		[Test]
		public void Numeric_Values_Are_Recognised()
		{
			// Arrange
			var areas = new List<string> { "study", "bedroom" };
			var devices = new List<string> { "light", "camera" };
			var pattern = "set alarm duration {value}";
			var parser = new CommandParser { Patterns = new List<string> { pattern }, Areas = areas, Devices = devices };

			// Act
			var match = parser.Parse("set alarm duration 20");

			// Assert
			Assert.IsNotNull(match);
			Assert.AreEqual(match.Parameters["value"], "20");
		}

		[Test]
		public void Two_Word_Device_Is_Recognised()
		{
			var parser = new CommandParser
			{
				Patterns = new List<string> { "turn {device} {value:on|off}" },
				Devices = new List<string> { "motion sensor" },
				Areas = new List<string> { "study" }
			};

			// Act
			var match = parser.Parse("turn motion sensor on");

			// Assert
			Assert.IsNotNull(match);
			Assert.AreEqual("motion sensor", match.Parameters["device"]);
			Assert.AreEqual("on", match.Parameters["value"]);
		}
	}
}
