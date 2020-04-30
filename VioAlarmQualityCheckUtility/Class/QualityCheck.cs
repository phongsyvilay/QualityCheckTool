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
		//private SqlServer SqlServer = new SqlServer();
		private readonly FwxClientWrapper _fwxClientWrapper = new FwxClientWrapper();
		private ReadDoneDelegate _readDoneDelegate;

		public class ReportState
		{
			public int Id { get; set; }
			public string Input { get; set; }
		}

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
				var report = new List<ReportModel>();
				var id = 0;


				//var ascEquipmentPropertyList = new List<AscEquipmentProperty>();
				// Alarm Sources
				//awxSourceList = SqlServer.GetAlarmSources();

				_readDoneDelegate = ReadDoneCallBack;

				foreach (AwxSource item in awxSourceList)
					if (item.Input1.Contains("@"))
					{
						const string pattern = @"[(){}|&!]|(==\s+\d*)";

						if (item.Input1.Contains("x="))
						{
							var input1 = item.Input1.Replace("x=", "").Trim();
							input1 = Regex.Replace(input1, pattern, "");

							report.Add(new ReportModel
							{
								ID = id,
								Type = "Alarm",
								Area = item.AreaName,
								TagName = item.Name,
								PointName = item.Input1,
								PointStatus = input1.Replace("quality", "").Trim(),
								SourceID = item.ID,
								MultipleInputs = true

							});
							id++;
						}

						var points = item.Input1.Split('@');
						points = points.Skip(1).ToArray();


						for (var i = 0; i < points.Length; i++)
						{
							var newWord = "@" + Regex.Replace(points[i], pattern, "").Trim();
							var reportModel = new ReportModel
							{
								ID = id,
								Type = "Alarm",
								Area = item.AreaName,
								TagName = item.Name,
								PointName = newWord,
								PointStatus = "Updating...",
								SourceID = item.ID,
								MultipleInputs = false
							};

							report.Add(reportModel);
							id++;

							ReadPointAsync(newWord, new ReportState
							{
								Id = reportModel.ID,
								Input = newWord
							});
						}
					}

				return report;
			}
			catch (Exception e)
			{
				// ReSharper disable once PossibleIntendedRethrow
				throw e;
			}
		}

		/** This is testing the connection to workbench. This is used in the checkall function so that it is tested before trying to check quality of tags.
		 *	If the connection is bad it will produce the string that is being checked for in the function. **/
		public bool TestWorkbenchConnection()
		{
			try
			{
				var sampleValue = _fwxClientWrapper.Read(@"@RSLinx OPC Server\[CC100]U502500.AUX.Value");
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
			var data = ((MainWindow)Application.Current.MainWindow)?.allReports;

			if (data != null)
				foreach (var item in data)
				{
					if (item.MultipleInputs && item.PointStatus.Contains(((ReportState)result.UserState).Input))
					{

						item.PointStatus = item.PointStatus.Replace(((ReportState)result.UserState).Input, result.Value.Status.ToString());

					}
					else if (item.MultipleInputs && !item.PointStatus.Contains("@"))
					{
						item.PointStatus = item.PointStatus.Contains("Bad") ? "Bad" : "Good";

					}
					else if (item.ID == ((ReportState)result.UserState).Id)
					{
						item.PointStatus = result.Value.Status.IsBad ? "Bad" : result.Value.Status.ToString();
						if (item.ID == 3310)
						{
							MessageBox.Show("1!!!! \nID: " + item.ID + ". \nPointname: " + item.PointName + ". \nStatus: " + result.Value.Status + ".");
						}
					}
				}
		}

		// Read Point Async
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void ReadPointAsync(string pointName, ReportState state)
		{
			_fwxClientWrapper.ReadAsync(pointName, _readDoneDelegate, state);

		}


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