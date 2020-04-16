using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Properties;

namespace VioAlarmQualityCheckUtility.Class
{
	public class AreaSourceHandler
	{
		private readonly SqlConnection _connection = new SqlConnection(Settings.Default.SqlConnectionString);
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
			using (_connection)
			{
				try
				{
					foreach (var source in sources)
					{
						using (var command = new SqlCommand("SELECT SA.SourceID, SA.AreaID, A.Name, A.ParentID " +
						                                    "FROM .dbo.AWX_Source2Area SA, .dbo.AWX_Area A " +
						                                    "WHERE SA.SourceID = @SrcID " +
						                                    "AND SA.AreaID = A.AreaID", _connection))
						{
							command.Parameters.AddWithValue("@SrcID", source.ID);
							SqlDataReader reader = command.ExecuteReader();

							AwxSource newSource = new AwxSource
							{
								AreaName = "",
								ID = source.ID,
								Input1 = source.Input1,
								Name = source.Name
							};

							if (!reader.HasRows)
							{
								newSource.AreaName = "Unassigned Tags";

								//Adding to unassigned tags area
								areas[0].SourcesList.Add(newSource);
								areas[1].SourcesList.Add(newSource);
							}

							if (reader.HasRows)
							{
								while (reader.Read())
								{
									var area = areas.Find(a => a.Id == (int) reader[1]);

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

							reader.Close();
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					throw;
				}
			}
		}

	}
}