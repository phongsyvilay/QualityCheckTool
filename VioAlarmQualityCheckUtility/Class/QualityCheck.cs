﻿using System;
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
							var input1 = item.Input1.Replace("x=", "").Trim();

							report.Add(new ReportModel
							{
								ID = id,
								Type = "Alarm",
								Area = item.AreaName,
								TagName = item.Name,
								PointName = item.Input1,
								PointStatus =input1.Replace("quality", "").Trim()
							});
						}

						points = item.Input1.Split('@');

							points = points.Skip(1).ToArray();


						for (var i = 0; i < points.Length ; i++)
						{
							ReportModel reportModel;

								newWord = "@" + Regex.Replace(points[i], pattern, "").Trim();
								reportModel = new ReportModel
								{
									ID = id,
									Type = "Alarm",
									Area = item.AreaName,
									TagName = item.Name,
									PointName = newWord,
									PointStatus = "Updating..."
								};

							report.Add(reportModel);
							id++;

							ReadPointAsync(newWord);
						}
					}

				//((MainWindow)Application.Current.MainWindow).Report.ItemsSource = report;

				return report;
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		/** This is testing the connection to workbench. This is used in the checkall function so that it is tested before trying to check quality of tags.
		 *	If the connection is bad it will produce the string that is being checked for in the function. **/
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

					if (item.PointName != result.UserState.ToString() && item.PointName.Contains(result.UserState.ToString()))
					{
						item.PointStatus = item.PointStatus.Replace(result.UserState.ToString(), result.Value.Status.ToString());
						if (item.PointStatus.Contains("Bad"))
						{
							item.PointStatus = "Bad";
						}
						else
						{
							string pattern = @"[\(\)\{\}\|\&\!]";
							item.PointStatus = Regex.Replace(item.PointStatus, pattern, "");

							if (!item.PointStatus.Contains("@"))
							{
								item.PointStatus = "Good";
							}
						}
					}
				}

			//((MainWindow)Application.Current.MainWindow).Report.ItemsSource = data;
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