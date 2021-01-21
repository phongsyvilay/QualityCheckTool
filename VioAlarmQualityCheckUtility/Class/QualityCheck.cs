using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Ico.Fwx.ClientWrapper;
using VioAlarmQualityCheckUtility.Models;

namespace VioAlarmQualityCheckUtility.Class
{
    internal class QualityCheck
    {
        private readonly FwxClientWrapper _fwxClientWrapper = new FwxClientWrapper();
        private ReadDoneDelegate _readDoneDelegate;

        // Class:              Class used as a parameter to the delegate, which will return a result and then cast to this class
        // ====================================================================================================================
        private class ReportState
        {
            public int ParentId { get; set; }
            public int Id { get; set; }
            public string Input { get; set; }
        }

        // Function:            This function does multiple things: 
        //                      1) Convert the sources from the db into ReportModel and ReportSubset objects
        //                      2) Add these objects to a report list which will get returned
        //                      3) Find the area that the source belongs to and add the ReportModel object to the area's list
        //                      4) Finally, create a ReportState object of the report and then run that through the 
        //                          ReadPointAsync function.
        // ====================================================================================================================
        public List<ReportModel> CheckAll(List<AwxSource> sources, List<AreaModel> areas)
        {
            try
            {
                var report = new List<ReportModel>();
                var id = 0;
                var original = 0; //will help us keep track if a tag needs to be broken down and which one 
                int subsetId = 0;

                _readDoneDelegate = ReadDoneCallBack;

                foreach (AwxSource item in sources) //after grabbing all sources turn them into report objects
                {
                    const string pattern = @"[(){}|&!]|(==\s+\d*)"; //commonly found chars in tag names to be removed
                    string input1;

                    if (item.Input1.Contains("x="))
                    {
                        input1 = item.Input1.Substring(2).Trim();    //removing the x=
                        input1 = Regex.Replace(input1, pattern, "");    //removing chars
                        input1 = input1.Replace("quality", "").Trim();

                        var reportModel = new ReportModel
                        {
                            ID = id,
                            Type = "Alarm",
                            Area = item.AreaName,
                            TagName = item.Name,
                            PointName = item.Input1,
                            PointStatus = input1,
                            SourceID = item.ID,
                            ReportSubset = new List<ReportSubset>()
                        };

                        report.Add(reportModel);
                        original = id;
                    }
                    else
                    {
                        input1 = item.Input1;
                        input1 = Regex.Replace(input1, pattern, "");
                    }

                    string[] points = input1.Split('@');
                    points = points.Skip(1).ToArray(); //first object in array is blank so skip to second

                    foreach (var inputPoint in points) //go through all the separated points and turn into reports
                    {
                        var newWord = "@" + inputPoint.Trim();

                        if (original != 0)
                        {
                            var reportSubset = new ReportSubset
                            {
                                ID = subsetId,
                                Area = item.AreaName,
                                TagName = item.Name,
                                PointName = newWord,
                                PointStatus = "Updating...",
                            };

                            report[original].ReportSubset.Add(reportSubset);
                            _fwxClientWrapper.ReadAsync(newWord, _readDoneDelegate, new ReportState
                            {
                                ParentId = id,
                                Id = reportSubset.ID,
                                Input = newWord
                            });

                            subsetId++;
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
                            var foundAreas = areas.FindAll(a => a.Id == item.AreaID);

                            if (foundAreas is null)
                            {
                                areas[1].SourcesList.Add(reportModel);
                            }
                            else
                            {
                                foreach (var area in foundAreas)
                                {
                                    areas.Find(a => a.Id == area.Id)?.SourcesList.Add(reportModel);
                                }
                            }

                            _fwxClientWrapper.ReadAsync(newWord, _readDoneDelegate, new ReportState
                            {
                                ParentId = id,
                                Id = id,
                                Input = newWord
                            });
                        }
                    }

                    id++;
                    subsetId = 0;
                    original = 0;
                }

                return report;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        // Function:            Used in the _fwxClientWrapper.ReadAsync function. Once the async returns a result it goes to this
        //                      function which will take the result, find the report
        // ====================================================================================================================
        private void ReadDoneCallBack(ReadDoneResult result)
        {
            var data = ((MainWindow)Application.Current.MainWindow)?.allReports;

            if (data != null)
            {
                try
                {
                    var item = data[(result.UserState as ReportState).ParentId];
                    if (item.ReportSubset != null)
                    {
                        var subset = item.ReportSubset[(result.UserState as ReportState).Id];

                        if (subset != null)
                        {
                            subset.PointStatus = result.Value.Status.IsBad ? "Bad" : result.Value.Status.ToString();
                            item.PointStatus = ReplaceFirst(item.PointStatus, ((ReportState)result.UserState).Input, subset.PointStatus);

                            if (!item.PointStatus.Contains("@"))
                                item.PointStatus = item.PointStatus.Contains("Bad") ? "Bad" : "Good";
                        }
                    }
                    else
                    {
                        item.PointStatus = result.Value.Status.IsBad ? "Bad" : result.Value.Status.ToString();
                    }
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Exception in ReadDoneCallBack");
                }
            }
        }

        // Function:        Duplicate method found in MainWindow.xaml.cs; Replaces the first found matching string
        // ====================================================================================================================
        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}