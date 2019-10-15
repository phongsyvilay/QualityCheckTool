using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Ico.Fwx.ClientWrapper;
using VioAlarmQualityCheckUtility.Models;

namespace VioAlarmQualityCheckUtility.Class
{
	class QualityCheck
	{
		SqlServer SqlServer = new SqlServer();

		FwxClientWrapper fwxClientWrapper = new FwxClientWrapper();
		ReadDoneDelegate readDoneDelegate;




		// Check All
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<ReportModel> CheckAll(List<AwxSource> sources)
		{
			List<AwxSource> awxSourceList = sources;
			List<AscEquipmentProperty> ascEquipmentPropertyList = new List<AscEquipmentProperty>();

			List<ReportModel> report = new List<ReportModel>();

			int id = 0;

			// Alarm Sources
			//awxSourceList = SqlServer.GetAlarmSources();

			readDoneDelegate = new ReadDoneDelegate(ReadDoneCallBack);

			foreach (var item in awxSourceList)
			{
				if (item.Input1.Contains("@"))
				{
					string clean = item.Input1.Replace("x=", "")
						.Replace("(", "")
						.Replace(")", "")
						.Replace("{", "")
						.Replace("}", "")
						.Replace("|", "")
						.Replace("&", "")
						.Replace("!", "");

					string[] points = clean.Split('@');

					for (int i = 0; i < points.Length - 1; i++)
					{
						ReportModel reportModel = new ReportModel
						{
							ID = id,
							Type = "Alarm",
							TagName = item.Name,
							PointName = "@" + points[i + 1].Trim(),
							PointStatus = "Updating..."
						};

						report.Add(reportModel);
						id++;

						ReadPointAsync("@" + points[i + 1].Trim());
					}
				}
			}

			((MainWindow)Application.Current.MainWindow).Report.ItemsSource = report;

			return report;
		}

		//readDoneDelegate = new ReadDoneDelegate(ReadDoneCallback);

		//foreach (var item in awxSourceList)
		//{
		//    if (item.Input1.Contains("@"))
		//    {
		//        string clean = item.Input1.Replace("x=", "")
		//            //.Replace("(", "")
		//            //.Replace(")", "")
		//            .Replace("{", "")
		//            .Replace("}", "")
		//            .Replace("|", "")
		//            .Replace("&", "")
		//            .Replace("!", "");

		//        string[] points = clean.Split('@');

		//        for (int i = 0; i < points.Length - 1; i++)
		//        {
		//            ReadPointAsync("@" + points[i + 1].Trim());

		//        }
		//    }

		//}


		// Asset Equipment Properties
		//ascEquipmentPropertyList = SqlServer.GetAssetEquipmentProperties();

		//foreach (var item in ascEquipmentPropertyList)
		//{
		//    if (item.RealtimePointName.Contains("@"))
		//    {
		//        string clean = item.RealtimePointName.Replace("x=", "")
		//            .Replace("(", "")
		//            .Replace(")", "")
		//            .Replace("{", "")
		//            .Replace("}", "")
		//            .Replace("|", "")
		//            .Replace("&", "")
		//            .Replace("!", "");

		//        string[] points = clean.Split('@');

		//        for (int i = 0; i < points.Length; i++)
		//        {
		//            // points[i] will always be ""
		//            //WriteEvent("Equipment Property", item.Name, GetPointQuality("@" + points[i + 1].Trim()));
		//            i++;
		//        }
		//    }
		//}






		// Read Point Async
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void ReadPointAsync(string pointName)
		{
			fwxClientWrapper.ReadAsync(pointName, readDoneDelegate, pointName);
		}




		// Read Done Call Back
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		private void ReadDoneCallBack(ReadDoneResult result)
		{
			List<ReportModel> data = (List<ReportModel>)((MainWindow)Application.Current.MainWindow).Report.ItemsSource;

			if (data != null)
			{
				foreach (var item in data)
				{
					if (item.PointName == result.UserState.ToString())
					{
						item.PointStatus = result.Value.Status.ToString();
					}
				}
			}

			((MainWindow)Application.Current.MainWindow).Report.ItemsSource = data;
			((MainWindow)Application.Current.MainWindow).Report.Items.Refresh();
		}
	}
}
