using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VioAlarmQualityCheckUtility.Models
{
	public class AwxSource
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public string Input1 { get; set; }
		public string AreaName { get; set; }
        public int AreaID { get; set; }

    }
}
