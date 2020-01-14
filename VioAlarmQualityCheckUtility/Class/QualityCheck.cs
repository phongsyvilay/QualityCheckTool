using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Ico.Fwx.ClientWrapper;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Windows;

namespace VioAlarmQualityCheckUtility.Class
{
	internal class QualityCheck
	{
		private SqlServer SqlServer = new SqlServer();

		private readonly FwxClientWrapper fwxClientWrapper = new FwxClientWrapper();
		private ReadDoneDelegate readDoneDelegate;


		// Check All
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<ReportModel> CheckAll(List<AwxSource> sources)
		{
			try
			{
				if (!TestWorkbenchConnection())
				{
					WorkbenchLogin login = new WorkbenchLogin();

					if (login.ShowDialog() == true)
					{

					}
				}

				var awxSourceList = sources;
				var ascEquipmentPropertyList = new List<AscEquipmentProperty>();

				var report = new List<ReportModel>();

				var id = 0;

				// Alarm Sources
				//awxSourceList = SqlServer.GetAlarmSources();

				readDoneDelegate = ReadDoneCallBack;

				foreach (AwxSource item in awxSourceList)
					if (item.Input1.Contains("@"))
					{
						string[] points = new string[] { };
						string newWord;
						string pattern = @"[\(\)\{\}\|\&\!]";


						if (item.Input1.Contains("x="))
						{

							report.Add(new ReportModel
							{
								ID = id,
								Type = "Alarm",
								Area = item.AreaName,
								TagName = item.Name,
								PointName = item.Input1,
								PointStatus = item.Input1.Replace("x=", "").Trim()
							});

							points = item.Input1.Split('@');

							if (points.Length != 2)
								points = points.Skip(1).ToArray();

						}
						else
							points = item.Input1.Split('@');

						for (var i = 0; i < points.Length - 1; i++)
						{
							ReportModel reportModel;

							if (points.Length == 2)
							{
								newWord = "@" + Regex.Replace(points[i + 1], pattern, "").Trim();
								reportModel = new ReportModel
								{
									ID = id,
									Type = "Alarm",
									Area = item.AreaName,
									TagName = item.Name,
									PointName = newWord,
									PointStatus = "Updating..."
								};
							}
							else
							{
								newWord = "@" + Regex.Replace(points[i], pattern, "").Trim();
								reportModel = new ReportModel
								{
									ID = id,
									Type = "Alarm",
									Area = "",
									TagName = item.Name,
									PointName = newWord,
									PointStatus = "Updating..."
								};
							}

							report.Add(reportModel);
							id++;

							ReadPointAsync(newWord);
						}
					}

				((MainWindow)Application.Current.MainWindow).Report.ItemsSource = report;

				return report;
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public bool TestWorkbenchConnection()
		{
			try
			{
				var sampleValue = fwxClientWrapper.Read(@"@RSLinx OPC Server\[CC004]U100005.UNIT_EN.Value");
				return sampleValue.Status.ToString() != "Bad - User Access Denied";
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;

			}
		}

		// Read Done Call Back
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		private void ReadDoneCallBack(ReadDoneResult result)
		{
			var data = (List<ReportModel>)((MainWindow)Application.Current.MainWindow).Report.ItemsSource;

			if (data != null)
				foreach (var item in data)
				{
					if (item.PointName == result.UserState.ToString())
					{
						item.PointStatus = result.Value.Status.ToString();

					}

					if (item.PointName.Contains(result.UserState.ToString()))
					{
						item.PointStatus = item.PointStatus.Replace(result.UserState.ToString(), result.Value.Status.ToString());
					}
				}

			((MainWindow)Application.Current.MainWindow).Report.ItemsSource = data;
			((MainWindow)Application.Current.MainWindow).Report.Items.Refresh();
		}

		// Read Point Async
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void ReadPointAsync(string pointName)
		{
			fwxClientWrapper.ReadAsync(pointName, readDoneDelegate, pointName);
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




	}
}