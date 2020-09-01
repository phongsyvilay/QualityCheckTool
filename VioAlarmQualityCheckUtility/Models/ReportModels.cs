using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VioAlarmQualityCheckUtility.Models
{
	public class ReportModel : INotifyPropertyChanged
	{
		private string _pointStatus;
		public event PropertyChangedEventHandler PropertyChanged;
		public int ID { get; set; }
		public string Type { get; set; }
		public string Area { get; set; }
		public string TagName { get; set; }
		public string PointName { get; set; }
		public List<ReportModel> ReportSubset { get; set; }
		public string PointStatus
		{
			get => _pointStatus;
			set
			{
				_pointStatus = value;
				OnPropertyChanged();
			}
		}
		public int SourceID { get; set; }

		protected void OnPropertyChanged([CallerMemberName] string pointStatus = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pointStatus));
		}
	}

	//public class ReportSubset
	//{
	//	private string _pointStatus;
	//	public event PropertyChangedEventHandler PropertyChanged;

	//	public int ID { get; set; }
	//	public string Area { get; set; }
	//	public string TagName { get; set; }
	//	public string PointName { get; set; }
	//	public string PointStatus
	//	{
	//		get => _pointStatus;
	//		set
	//		{
	//			_pointStatus = value;
	//			OnPropertyChanged();
	//		}
	//	}

	//	protected void OnPropertyChanged([CallerMemberName] string pointStatus = null)
	//	{
	//		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pointStatus));
	//	}
	//}

}
