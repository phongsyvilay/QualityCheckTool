using System.Collections.Generic;

namespace VioAlarmQualityCheckUtility.Models
{
	public class AreaModel
	{
		public List<AreaModel> Children = new List<AreaModel>();
		public List<AwxSource> SourcesList = new List<AwxSource>();
		public int Id { get; set; }
		public int RecursiveParentId { get; set; }
		public string Name { get; set; }


		public IList<AreaModel> Items
		{
			get
			{
				IList<AreaModel> childNodes = new List<AreaModel>();

				foreach (var child in this.Children)
					childNodes.Add(child);

				return childNodes;
			}
		}
	}
}