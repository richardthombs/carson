using System.Collections.Generic;
using System.Threading.Tasks;

using ZWave;
using ZWave.CommandClasses;

namespace Experiment1.ZWaveDrivers
{
	public class ZWaveAssociationDriver : IZWaveAssociationDriver
	{
		Node node;

		public ZWaveAssociationDriver(Node node)
		{
			this.node = node;
		}

		public async Task<Dictionary<byte,List<byte>>> Get()
		{
			var dict = new Dictionary<byte, List<byte>>();

			var assoc = node.GetCommandClass<Association>();
			var groupsReport = await assoc.GetGroups();

			var nodes = new List<byte>();
			for (byte group=1; group <=groupsReport.GroupsSupported; group++)
			{
				var groupReport = await assoc.Get(group);
				foreach (var node in groupReport.Nodes)
				{
					nodes.Add(node);
				}

				dict.Add(group, nodes);
			}

			return dict;
		}

		public async Task Remove(byte groupID, byte nodeID)
		{
			var assoc = node.GetCommandClass<Association>();
			await assoc.Remove(groupID, nodeID);
		}

		public async Task Add(byte groupID, byte nodeID)
		{
			var assoc = node.GetCommandClass<Association>();
			await assoc.Add(groupID, nodeID);
		}
	}
}
