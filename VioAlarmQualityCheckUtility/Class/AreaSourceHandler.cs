using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using VioAlarmQualityCheckUtility.Models;

namespace VioAlarmQualityCheckUtility.Class
{
	public class AreaSourceHandler
	{
		public List<AreaModel> GetAreas(string instance, string db, string username, string password, List<AwxSource> sources)
		{
			List<AreaModel> areasList = new List<AreaModel>();
			SqlDataReader reader;
			List<Source2Area> s2a = new List<Source2Area>();
			var temp = Properties.Settings.Default.SqlServerInstance;
			SqlConnection connection;

			areasList.Add(new AreaModel
			{
				Id = 0,
				Name = "Not Assigned Area",
				RecursiveParentId = 0
			});


			if (username == "")
				connection = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + $";Initial Catalog = {db}; Integrated Security=True");
			else
				connection = new SqlConnection($@"Data Source={instance}; Initial Catalog = {db}; User ID={username}; Password={password}");

			using (connection)
			{
				try
				{
					connection.Open();

					using (var command = new SqlCommand("dbo.GetAllFromArea", connection)
						{CommandType = CommandType.StoredProcedure})
					{

						reader = command.ExecuteReader();

						while (reader.Read())
						{

							int input1;

							if (!DBNull.Value.Equals(reader[6]))
							{
								input1 = (int) reader[6];
							}
							else
								input1 = 0;


							var area = new AreaModel
							{
								Id = (int) reader[1],
								Name = reader[5].ToString(),
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

				try
				{
					using (var command = new SqlCommand("dbo.GetAllFromAreaSource", connection)
						{CommandType = CommandType.StoredProcedure})
					{
						reader = command.ExecuteReader();


						while (reader.Read())
						{
							s2a.Add(new Source2Area
							{
								SourceID = (int) reader[4],
								AreaID = (int) reader[1]
							});

							
						}
					}
				}
				catch (Exception e)
				{
					throw e;

				}

				AssignSourceToArea(sources, areasList, s2a);
			}

			return areasList;
		}

		public void AssignSourceToArea(List<AwxSource> sources, List<AreaModel> areas, List<Source2Area> s2a)
		{
			for (int i = 0; i < sources.Count; i++)
			{
				try
				{
					var source = sources[i];
					var source2Areas = s2a.FindAll(s => s.SourceID == source.ID);

					if (source2Areas.Count == 0)
					{
						var noArea = areas.Find(a => a.Name == "Not Assigned Area");

						noArea.SourcesList.Add(new AwxSource
						{
							AreaName = noArea.Name,
							ID = source.ID,
							Input1 = source.Input1,
							Name = source.Name
						});
					}
					else
					{
						foreach (var source2Area in source2Areas)
						{
							AwxSource newSource = new AwxSource
							{
								AreaName = "",
								ID = source.ID,
								Input1 = source.Input1,
								Name = source.Name
							};

							var area = areas.Find(a => a.Id == source2Area.AreaID);

							if (area.RecursiveParentId == 0)
							{
								newSource.AreaName = area.Name;
							}
							else
							{
								AreaModel tempArea = areas.Find(parent => parent.Id == area.RecursiveParentId);

								var parentAreasString = tempArea.Name;

								while (tempArea.RecursiveParentId != 0)
								{
									tempArea = areas.Find(a => a.Id == tempArea.RecursiveParentId);
									parentAreasString = tempArea.Name + "\\" + parentAreasString;
								}

								newSource.AreaName = parentAreasString;

							}

							area.SourcesList.Add(newSource);
						}
					}
				}
				catch
				{
					MessageBox.Show("AssignSourceToArea Error");
				}

			}
		}
		
	}
}