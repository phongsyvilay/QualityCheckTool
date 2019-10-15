using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Documents;
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


			using (var connection =
				new SqlConnection(
					$@"Data Source={instance}; Initial Catalog = {db}; User ID={username}; Password={password}"))
			{

				connection.Open();

				using (var command = new SqlCommand("dbo.GetAllFromArea", connection)
				{ CommandType = CommandType.StoredProcedure })
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
							ID = (int) reader[1],
							Name = reader[5].ToString(),
							RecursiveParentID = input1,
						};

						if (area.RecursiveParentID != 0)
						{
							var tempArea = areasList.Find(t => t.ID == area.RecursiveParentID);
							tempArea.Children.Add(area);
						}

						areasList.Add(area);
					}

					reader.Close();
				}

				using (var command = new SqlCommand("dbo.GetAllFromAreaSource", connection) {CommandType = CommandType.StoredProcedure})
				{
					reader = command.ExecuteReader();


					while (reader.Read())
					{
						s2a.Add(new Source2Area
						{
							SourceID = (int) reader[4],
							AreaID = (int) reader[1]
						});
						
						//var columns = new List<string>();
						//for (int i = 0; i < reader.FieldCount; i++)
						//{
						//	columns.Add(reader.GetName(i));
						//	columns.Add(reader[i].GetType().ToString());
						//}
					}
				}

				AssignSourceToArea(sources, areasList, s2a);

				//List<AreaModel> topAreas = areasList.FindAll(a => a.RecursiveParentID == 0);
				//foreach (var areaModel in topAreas)
				//{
				//	RecurseList(areaModel);
				//}
			}

			return areasList;
		}

		public void AssignSourceToArea(List<AwxSource> sources, List<AreaModel> areas, List<Source2Area> s2a)
		{
			foreach (AwxSource temp in sources)
			{
				var source = temp;
				Source2Area source2Area = s2a.Find(s => s.SourceID == temp.ID);

				AreaModel area = areas.Find(a => a.ID == source2Area.AreaID);

				area.SourcesList.Add(source);
			}
		}

		
	}
}