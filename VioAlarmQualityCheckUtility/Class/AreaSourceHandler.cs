using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Properties;

namespace VioAlarmQualityCheckUtility.Class
{
	public class AreaSourceHandler
	{
		private readonly SqlConnection _connection = new SqlConnection(Settings.Default.SqlConnectionString);

		public class Source2Area
		{
			public int SourceID { get; set; }
			public int AreaID { get; set; }
		}

		public List<AreaModel> GetAreas(List<AwxSource> sources)
		{
			List<AreaModel> areasList = new List<AreaModel>
			{
				new AreaModel {Id = 0, Name = "All Tags", RecursiveParentId = 0},
				new AreaModel {Id = 0, Name = "Unassigned Tags", RecursiveParentId = 0}
			};

			using (_connection)
			{
				_connection.Open();
				_connection.ChangeDatabase(Settings.Default.SqlServerDatabase);

				try
				{
					using (var command = new SqlCommand("SELECT AreaID, Name, ParentID FROM .dbo.AWX_Area;", _connection))
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
					// ReSharper disable once PossibleIntendedRethrow
					throw e;
				}

				AssignSourceToArea(sources, areasList);
			}

			return areasList;
		}

		public void AssignSourceToArea(List<AwxSource> sources, List<AreaModel> areas)
		{
			var s2a = new List<Source2Area>();
			
			try
			{

				using (var command = new SqlCommand("dbo.GetAllFromAreaSource", _connection)
				{ CommandType = CommandType.StoredProcedure })
				{
					var reader = command.ExecuteReader();

					while (reader.Read())
					{
						s2a.Add(new Source2Area
						{
							SourceID = (int)reader[4],
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
						var newSource = new AwxSource
						{
							AreaName = areas[1].Name,
							ID = source.ID,
							Input1 = source.Input1,
							Name = source.Name
						};
						//Adding to unassigned tags area
						areas[0].SourcesList.Add(newSource);
						areas[1].SourcesList.Add(newSource);
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

								newSource.AreaName = parentAreasString + "\\" + area.Name;
							}

							area.SourcesList.Add(newSource);
							areas[0].SourcesList.Add(newSource);
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