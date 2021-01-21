using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Properties;

namespace VioAlarmQualityCheckUtility.Class
{
    public class AreaSourceHandler
    {
        private SqlConnection _connection = new SqlConnection(Settings.Default.SqlConnectionString);

        // Class:           After querying SQL, we will convert the results to this class which is used to help assign IDs
        // ====================================================================================================================
        public class Source2Area
        {
            public int SourceID { get; set; }
            public int AreaID { get; set; }
        }

        // Function:        1) Queries SQL for AreaID, Name, and ParentID of each area (only looking for Controls not Dashboards)
        //                  2) Takes the results and turns them into AreaModel objects
        //                  3) Adds objects to a list to be returned
        // ====================================================================================================================
        public List<AreaModel> GetAreas()
        {
            List<AreaModel> areasList = new List<AreaModel>
            {
                new AreaModel {Id = 0, Name = "All Tags", RecursiveParentId = 0},
                new AreaModel {Id = -1, Name = "Unassigned Tags", RecursiveParentId = 0}
            };

            using (_connection)
            {
                _connection.Open();
                _connection.ChangeDatabase(Settings.Default.SqlServerDatabase);

                try
                {
                    using (var command = new SqlCommand(
                        "WITH ControlArea(AreaID, Name, ParentID) AS (SELECT AreaID, Name, ParentID FROM dbo.AWX_Area " +
                        "WHERE AreaID = 1 " +
                        "UNION ALL " +
                        "SELECT area.AreaID, area.Name, area.ParentID " +
                        "FROM dbo.AWX_Area area, ControlArea ct " +
                        "WHERE area.ParentID = ct.AreaID) " +
                        "SELECT * from ControlArea ", _connection))
                    {
                        var reader = command.ExecuteReader();

                        while (reader.Read())
                        {

                            int input1;

                            if (!DBNull.Value.Equals(reader[2]))
                            {
                                input1 = (int)reader[2];
                            }
                            else
                                input1 = 0;


                            var area = new AreaModel
                            {
                                Id = (int)reader[0],
                                Name = reader[1].ToString(),
                                RecursiveParentId = input1,
                            };

                            if (area.RecursiveParentId != 0)
                            {
                                var tempArea = areasList.Find(t => t.Id == area.RecursiveParentId);
                                tempArea?.Children.Add(area);
                            }

                            areasList.Add(area);
                        }

                        reader.Close();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            return areasList;
        }

        // Function:        1) Queries SQL for SourceID and AreaID of sources that belong in the areas we're looking for (Controls)
        //                  2) Turns the results into Source2Area objects
        //                  3) Goes through each source in the list given to us and finds all the S2A objects that match
        //                  4) Takes the Source2Area objects and searches for the matching area 
        //                  5) If no area has been found, then add it to 'Unassigned Tags'
        //                  6) If area has been found, then add the source to the area's SourceList
        // ====================================================================================================================
        public List<AwxSource> AssignSourceToArea(List<AwxSource> sources, List<AreaModel> areas)
        {
            var s2a = new List<Source2Area>();
            var assignedSources = new List<AwxSource>();
            _connection = new SqlConnection(Settings.Default.SqlConnectionString);

            using (_connection)
            {
                _connection.Open();
                _connection.ChangeDatabase(Settings.Default.SqlServerDatabase);

                try
                {
                    using (var command = new SqlCommand("WITH ControlArea(AreaID, Name, ParentID) AS (SELECT AreaID, Name, ParentID " +
                        "FROM dbo.AWX_Area " +
                        "WHERE AreaID = 1 " +
                        "UNION ALL " +
                        "SELECT area.AreaID, area.Name, area.ParentID " +
                        "FROM dbo.AWX_Area area, ControlArea ct " +
                        "WHERE area.ParentID = ct.AreaID) " +
                        "SELECT s2a.SourceID, s2a.AreaID " +
                        "FROM dbo.AWX_Source2Area s2a, ControlArea ca " +
                        "WHERE s2a.AreaID = ca.AreaID", _connection))

                    {
                        var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            s2a.Add(new Source2Area
                            {
                                SourceID = (int)reader[0],
                                AreaID = (int)reader[1]
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }

                for (int i = 0; i < sources.Count; i++)
                {
                    try
                    {
                        var source = sources[i];
                        var source2Areas = s2a.FindAll(s => s.SourceID == source.ID);

                        if (source2Areas.Count == 0)
                        {
                            source.AreaName = areas[1].Name;
                            source.AreaID = -1;

                            //Adding to unassigned tags area
                            assignedSources.Add(source);
                        }
                        else
                        {
                            foreach (var source2Area in source2Areas)
                            {
                                source.AreaName = "";

                                var area = areas.Find(a => a.Id == source2Area.AreaID);

                                if (area.RecursiveParentId == 0)
                                {
                                    source.AreaName = area.Name;
                                }
                                else
                                {
                                    AreaModel tempArea = areas.Find(parent => parent.Id == area.RecursiveParentId);

                                    var parentAreasName = tempArea.Name;

                                    while (tempArea.RecursiveParentId != 0)
                                    {
                                        tempArea = areas.Find(a => a.Id == tempArea.RecursiveParentId);
                                        parentAreasName = tempArea.Name + "\\" + parentAreasName;
                                    }

                                    source.AreaID = source2Area.AreaID;
                                    source.AreaName = parentAreasName + "\\" + area.Name;
                                }

                                assignedSources.Add(source);
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("AssignSourceToArea Error");
                    }
                }
            }

            return assignedSources;
        }
    }
}
