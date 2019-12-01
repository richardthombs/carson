using System.Collections.Generic;

namespace Experiment1
{
	public class Area
	{
		public Area Parent { get; set; }
		public List<Area> Children { get; set; }
		public string Name { get; set; }
		public List<IDevice> Devices { get; set; }

		public Area()
		{
			Children = new List<Area>();
			Devices = new List<IDevice>();
		}

		public IEnumerable<Area> GetAreas()
		{
			return GetAreas(this);
		}

		IEnumerable<Area> GetAreas(Area current)
		{
			yield return current;

			foreach (var area in current.Children)
			{
				foreach (var x in GetAreas(area))
				{
					yield return x;
				}
			}
		}

		public IEnumerable<IDevice> GetDevices(string deviceName = null)
		{
			return GetDevices(this, deviceName);
		}

		IEnumerable<IDevice> GetDevices(Area current, string deviceName)
		{
			foreach (var device in current.Devices)
			{
				if (DeviceNameMatcher(device, deviceName)) yield return device;
			}

			foreach (var area in current.Children)
			{
				foreach (var device in area.GetDevices(area, deviceName))
				{
					if (DeviceNameMatcher(device, deviceName)) yield return device;
				}
			}
		}

		bool DeviceNameMatcher(IDevice device, string deviceName)
		{
			if (deviceName == null) return true;
			if (device.Name == deviceName) return true;
			if (device.Plural == deviceName) return true;
			return false;
		}
	}
}
