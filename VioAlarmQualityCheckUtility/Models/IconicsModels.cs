using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VioAlarmQualityCheckUtility.Models
{
	class AscEquipmentProperty
	{
		public string ParentID { get; set; }
		public string Name { get; set; }
		public string RealtimePointName { get; set; }
	}

	class AwxSource
	{
		public string Name { get; set; }
		public string Input1 { get; set; }
		public int ID { get; set; }
	}
}
