using System;
using System.Collections.Generic;

namespace Experiment1
{
	public class AreaCollection
	{
		public Area Root { get; set; }
		public List<string> AreaNames { get; set; }

		public AreaCollection()
		{
			Root = new Area
			{
				Name = "all",
			};

			AreaNames = new List<string> { "all" };
		}

		public Area Find(string name)
		{
			return Find(name, Root);
		}

		Area Find(string name, Area current)
		{
			if (current.Name == name) return current;

			foreach (var area in current.Children)
			{
				var found = Find(name, area);
				if (found != null) return found;
			}

			return null;
		}

		public void Create(string name)
		{
			if (Find(name) != null) throw new ArgumentException("Area already exists", nameof(name));

			var area = new Area
			{
				Name = name,
				Parent = Root
			};

			Root.Children.Add(area);

			AreaNames.Add(name);
		}

		public void Add(string parentName, params string[] areaNames)
		{
			var parent = Find(parentName) ?? throw new ArgumentException("No such area", nameof(parentName));

			foreach (var areaName in areaNames)
			{
				var area = Find(areaName) ?? throw new ArgumentException("No such area", nameof(areaNames));

				if (area.Parent != null) area.Parent.Children.Remove(area);
				area.Parent = parent;
				parent.Children.Add(area);
			}
		}

		public void AddDevice(string areaName, params IDevice[] devices)
		{
			var area = Find(areaName);

			foreach (var device in devices)
			{
				area.Devices.Add(device);
				device.Area = area;
			}
		}
	}
}
