using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


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
		public List<ReportSubset> ReportSubset { get; set; }
		public int SourceID { get; set; }
		public string PointStatus
		{
			get => _pointStatus;
			set
			{
				_pointStatus = value;
				OnPropertyChanged();
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string pointStatus = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pointStatus));
		}
	}

    public class ReportSubset
    {
        private string _pointStatus;
        public event PropertyChangedEventHandler PropertyChanged;

        public int ID { get; set; }
        public string Area { get; set; }
        public string TagName { get; set; }
        public string PointName { get; set; }
        public string PointStatus
        {
            get => _pointStatus;
            set
            {
                _pointStatus = value;
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string pointStatus = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pointStatus));
        }
    }
}
