using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VioAlarmQualityCheckUtility.Models
{
    public class ReportModel
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string TagName { get; set; }
        public string PointName { get; set; }
        public string PointStatus { get; set; }
    }
}
