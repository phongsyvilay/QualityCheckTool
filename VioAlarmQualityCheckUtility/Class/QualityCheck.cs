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
				var original = 0;

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

							var reportModel = new ReportModel
							{
								ID = id,
								Type = "Alarm",
								Area = item.AreaName,
								TagName = item.Name,
								PointName = item.Input1,
								PointStatus = input1.Replace("quality", "").Trim(),
								SourceID = item.ID,
								ReportSubset = new List<ReportModel>()
							};

							report.Add(reportModel);
							original = id;
							id++;
						}

						var points = item.Input1.Split('@');
						points = points.Skip(1).ToArray();


						foreach (var inputPoint in points)
						{
							var newWord = "@" + Regex.Replace(inputPoint, pattern, "").Trim();

							if (original != 0)
							{
								var reportSubset = new ReportModel
								{
									ID = id,
									Type = "Alarm",
									Area = item.AreaName,
									TagName = item.Name,
									PointName = newWord,
									PointStatus = "Updating...",
									SourceID = item.ID
								};
								(report.Find(r => r.ID == original)).ReportSubset.Add(reportSubset);
								ReadPointAsync(newWord, new ReportState
								{
									Id = reportSubset.ID,
									Input = newWord
								});
							}
							else
							{
								var reportModel = new ReportModel
								{
									ID = id,
									Type = "Alarm",
									Area = item.AreaName,
									TagName = item.Name,
									PointName = newWord,
									PointStatus = "Updating...",
									SourceID = item.ID
								};

								report.Add(reportModel);

								ReadPointAsync(newWord, new ReportState
								{
									Id = reportModel.ID,
									Input = newWord
								});
							}
							id++;
						}

						original = 0;
					}

				return report;
			}
			catch (Exception e)
			{
				//MessageBox.Show(e.ToString());
				// ReSharper disable once PossibleIntendedRethrow
				throw e;
			}
		}

		/** This check of quality is used on current display of tags. The reports that are passed through have had sources that were 
		 * refetched from the database. **/
		public void RecheckReports(List<ReportModel> reports)
		{
			try
			{
				_readDoneDelegate = ReadDoneCallBack;
				foreach (var report in reports)
				{
					if (report.PointName.Contains("@"))
					{
						const string pattern = @"[(){}|&!]|(==\s+\d*)";

						if (report.PointName.Contains("x="))
						{
							var input1 = report.PointName.Replace("x=", "").Trim();
							input1 = Regex.Replace(input1, pattern, "");
							report.PointStatus = input1.Replace("quality", "").Trim();
						}

						var points = report.PointStatus.Split('@');
						points = points.Skip(1).ToArray();
						if(report.ReportSubset != null)
						{
							foreach(var sub in report.ReportSubset)
							{
								sub.PointName = "";
								sub.PointStatus = "Updating...";
							}
						}

						foreach (var inputPoint in points)
						{
							var newWord = "@" + Regex.Replace(inputPoint, pattern, "").Trim();

							if (report.ReportSubset != null)
							{
								var subReport = report.ReportSubset.Find(name => name.PointName == "");
								subReport.PointName = newWord;

								ReadPointAsync(subReport.PointName, new ReportState
								{
									Id = subReport.ID,
									Input = subReport.PointName
								});
							}
							else
							{
								report.PointName = newWord;
								report.PointStatus = "Updating...";

								ReadPointAsync(newWord, new ReportState
								{
									Id = report.ID,
									Input = newWord
								});
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.ToString());
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
				//MessageBox.Show(sampleValue.Status.ToString());
				return sampleValue.Status.ToString() != "Bad - User Access Denied";
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
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


		// Read Done Call Back
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		private void ReadDoneCallBack(ReadDoneResult result)
		{
			var data = ((MainWindow)Application.Current.MainWindow)?.allReports;

			if (data != null)
			{
				foreach (var item in data)
				{
					if (item.ReportSubset != null)
					{
						var subset = item.ReportSubset.Find(s => s.ID == ((ReportState)result.UserState).Id);

						if (subset != null)
						{
							subset.PointStatus = result.Value.Status.IsBad ? "Bad" : result.Value.Status.ToString();
							item.PointStatus = ReplaceFirst(item.PointStatus, ((ReportState)result.UserState).Input, subset.PointStatus);

							if (!item.PointStatus.Contains("@"))
								item.PointStatus = item.PointStatus.Contains("Bad") ? "Bad" : "Good";
						}
					}
					else if (item.ID == ((ReportState)result.UserState).Id)
					{
						item.PointStatus = result.Value.Status.IsBad ? "Bad" : result.Value.Status.ToString();
					}
				}
			}
		}

		private string ReplaceFirst(string text, string search, string replace)
		{
			int pos = text.IndexOf(search, StringComparison.Ordinal);
			if (pos < 0)
			{
				return text;
			}

			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
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