using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Experiment1.Tests
{
	[TestFixture]
	public class AreaTests
	{
		[Test]
		public void Area_Iterator_Works()
		{
			var root = new Area
			{
				Name = "all",
				Children = new List<Area>
				{
					new Area
					{
						Name = "1a",
						Children = new List<Area>
						{
							new Area { Name = "1a2a" },
							new Area { Name = "1a2b" }
						}
					},
					new Area { Name = "1b" }
				}
			};

			var names = new List<string>();
			foreach (var area in root.GetAreas())
			{
				names.Add(area.Name);
			}

			Assert.AreEqual("all", names[0]);
			Assert.AreEqual("1a", names[1]);
			Assert.AreEqual("1a2a", names[2]);
			Assert.AreEqual("1a2b", names[3]);
			Assert.AreEqual("1b", names[4]);
		}
	}
}
