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
								tempArea.Children.Add(area);
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
			foreach (AwxSource temp in sources)
			{
				var source = temp;
				Source2Area source2Area = s2a.Find(s => s.SourceID == temp.ID);

				AreaModel area = areas.Find(a => a.Id == source2Area.AreaID);

				area.SourcesList.Add(source);
			}
		}

		
	}
}