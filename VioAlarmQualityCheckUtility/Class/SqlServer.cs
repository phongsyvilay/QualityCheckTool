using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Microsoft.Win32;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Properties;

namespace VioAlarmQualityCheckUtility.Class
{
	class SqlServer
	{
		private static SqlConnection _sqlConn;

		// Function:        Get Local SQL Server Instances
		// Description:     Returns a collection of installed local SQL Server instances
		// ==============================================================================================================================================================
		public static IEnumerable<string> GetLocalSqlInstances()
		{
			if (Environment.Is64BitOperatingSystem)
			{
				using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
				{
					foreach (string item in GetLocalSqlInstances(hive))
					{
						yield return item;
					}
				}

				using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
				{
					foreach (string item in GetLocalSqlInstances(hive))
					{
						yield return item;
					}
				}
			}
			else
			{
				foreach (string item in GetLocalSqlInstances(Registry.LocalMachine))
				{
					yield return item;
				}
			}
		}

		private static IEnumerable<string> GetLocalSqlInstances(RegistryKey hive)
		{
			const string keyName = @"Software\Microsoft\Microsoft SQL Server";
			const string valueName = "InstalledInstances";
			const string defaultName = "MSSQLSERVER";

			using (var key = hive.OpenSubKey(keyName, false))
			{
				if (key == null) return Enumerable.Empty<string>();

				if (!(key.GetValue(valueName) is string[] value)) return Enumerable.Empty<string>();

				for (int index = 0; index < value.Length; index++)
				{
					if (string.Equals(value[index], defaultName, StringComparison.OrdinalIgnoreCase))
					{
						value[index] = ".";
					}
					else
					{
						value[index] = @".\" + value[index];
					}
				}

				return value;
			}
		}


		public List<string> GetRemoteInstances(string computerName, string username, string password)
		{
			List<string> instances = new List<string>
			{
				"-- Select a Server Instance --"
			};

			using (NetworkShareAccessor.Access(computerName, Environment.UserName, username, password))
			{
				RegistryView registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
				var envRegistryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computerName, registryView);

				RegistryKey temp = envRegistryKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", false);
				if (temp != null)
				{

					foreach (var names in temp.GetValueNames())
					{
						instances.Add(names);
					}
				}
				
				return instances;
			}
		}


		public List<string> GetSqlInstances()
		{
			System.Data.Sql.SqlDataSourceEnumerator instance = System.Data.Sql.SqlDataSourceEnumerator.Instance;
			DataTable dataTable = instance.GetDataSources();
			List<string> list = new List<string>();

			foreach (DataRow row in dataTable.Rows)
			{
				string temp = row.ItemArray[0].ToString();

				if (row.ItemArray[1].ToString() != "")
					temp += $@"\{row.ItemArray[1]}";

				list.Add(temp);
			}

			return list;

		}

		// Function:        Get Databases from Selected Local or Remote SQL Server Instance
		// Description:     Returns a collection of databases from the user selected local SQL Server instance
		// ==============================================================================================================================================================
		public static List<string> GetLocalSqlInstanceDatabases()
		{
			Settings.Default.SqlConnectionString = "Data Source=" + Settings.Default.SqlServerInstance + ";Integrated Security=True";
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);
			return QueryDatabases();
		}

		public static List<string> GetSqlInstanceDatabases(string instance, string username, string password)
		{
			Settings.Default.SqlConnectionString = $"Data Source={instance}; User ID={username}; Password={password}";
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);
			return QueryDatabases();
		}

		public static List<string> QueryDatabases() 
		{
			List<string> databases = new List<string>();
			databases.Add("-- Select a Database --");
			SqlCommand cmd = new SqlCommand();

			try
			{
				cmd.CommandText = "SELECT name FROM sys.databases;";
				cmd.CommandType = CommandType.Text;
				cmd.Connection = _sqlConn;
				cmd.Connection.Open();

				var reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					databases.Add(reader[0].ToString());
				}

				reader.Close();

				cmd.Connection.Close();

			}
			catch (Exception)
			{
				MessageBox.Show("Could not establish connection to selected SQL Server Instance. Try using a username and password.");
				throw;
			}
			
			return databases;
		}


		// Functions: GetAlarmSources, GetRemoteAlarmSources, and QueryAwxSources         
		// Description: These are all related to opening up a SQL connection to either a remote server or local SQL instance and querying the SQL DB for the sources.
		// ==============================================================================================================================================================

		public List<AwxSource> QueryAwxSources()
		{
			SqlCommand cmd = new SqlCommand();
			List<AwxSource> data = new List<AwxSource>();
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);

			cmd.CommandText = "SELECT Name, Input1, SourceID FROM " + Settings.Default.SqlServerDatabase + ".dbo.AWX_Source;";
			cmd.CommandType = CommandType.Text;
			cmd.Connection = _sqlConn;
			cmd.Connection.Open();
		
			var reader = cmd.ExecuteReader();

			while (reader.Read())
			{
				var awxSource = new AwxSource
				{
					Name = reader[0].ToString(),
					Input1 = reader[1].ToString(),
					ID = (int)reader[2]
				};

				data.Add(awxSource);
			}

			reader.Close();

			cmd.Connection.Close();

			return data;
		}

		public List<ReportModel> UpdateReportSource(List<ReportModel> reports)
		{
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);
			using (_sqlConn)
			{
				_sqlConn.Open();
				_sqlConn.ChangeDatabase(Settings.Default.SqlServerDatabase);
				try
				{
					foreach (var reportModel in reports)
					{
						using (var command = new SqlCommand("SELECT Name, Input1 " +
						                                    "FROM .dbo.AWX_Source " +
						                                    "WHERE SourceID = @SrcID ", _sqlConn))
						{
							command.Parameters.AddWithValue("@SrcID", reportModel.SourceID);
							var reader = command.ExecuteReader();

							while (reader.Read())
							{
								reportModel.TagName = reader[0].ToString();
								reportModel.PointName = reader[1].ToString();
							}

							reader.Close();
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show(ex.ToString());
					//MessageBox.Show("Unable to query source.");
				}
				catch (Exception e)
				{
					MessageBox.Show(e.ToString());
				}
			}

			return reports;
		}

		/* Updates the point name in SQL when edited in the datagrid from the window. */
		public void UpdateAwxSourcePointName(ReportModel sourceToUpdate, string newName)
		{
			SqlCommand cmd = new SqlCommand();
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);

			try
			{
				cmd.Connection = _sqlConn;
				cmd.Connection.Open();
				cmd.CommandText =
					$"UPDATE {Settings.Default.SqlServerDatabase}.dbo.AWX_Source SET Input1 = '{newName}' WHERE SourceID = {sourceToUpdate.SourceID}";

				cmd.CommandType = CommandType.Text;
				cmd.ExecuteNonQuery();
				cmd.Connection.Close();

			}
			catch (SqlException)
			{
				MessageBox.Show("Unable to update point name.");
				cmd.Connection.Close();
			}
			catch (Exception)
			{
				MessageBox.Show("Unable to update point name");
				cmd.Connection.Close();
			}
		}

		//public void UpdateAwxSourceTagName(ReportModel sourceToUpdate, string newName)
		//{
		//	SqlCommand cmd = new SqlCommand();

		//	try
		//	{
		//		cmd.Connection = SqlConn;
		//		cmd.Connection.Open();
		//		cmd.CommandText =
		//			$"UPDATE {Settings.Default.SqlServerDatabase}.dbo.AWX_Source SET Name = '{newName}' WHERE SourceID = {sourceToUpdate.SourceID}";

		//		cmd.CommandType = CommandType.Text;
		//		cmd.ExecuteNonQuery();
		//		cmd.Connection.Close();

		//	}
		//	catch (SqlException)
		//	{
		//		MessageBox.Show("Unable to update tag name.");
		//		cmd.Connection.Close();
		//	}
		//	catch (Exception)
		//	{
		//		MessageBox.Show("Unable to update tag name.");
		//		cmd.Connection.Close();
		//	}
		//}


		// Function:        
		// Description:     
		// ==============================================================================================================================================================
		//public List<AscEquipmentProperty> GetAssetEquipmentProperties()
		//{
		//	List<AscEquipmentProperty> data = new List<AscEquipmentProperty>();

		//	SqlConnection sqlConn = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + ";Integrated Security=True");

		//	SqlCommand cmd = new SqlCommand();

		//	try
		//	{
		//		cmd.CommandText = "SELECT ParentID, Name, RealtimePointName FROM " + Properties.Settings.Default.SqlServerDatabase + ".dbo.ASC_EquipmentProperties;";
		//		cmd.CommandType = CommandType.Text;
		//		cmd.Connection = sqlConn;
		//		cmd.Connection.Open();

		//		var reader = cmd.ExecuteReader();

		//		while (reader.Read())
		//		{
		//			var ascEquipmentProperty = new AscEquipmentProperty
		//			{
		//				ParentID = reader[0].ToString(),
		//				Name = reader[1].ToString(),
		//				RealtimePointName = reader[2].ToString()
		//			};

		//			data.Add(ascEquipmentProperty);
		//		}

		//		reader.Close();

		//		cmd.Connection.Close();
		//	}
		//	catch (Exception)
		//	{
		//		MessageBox.Show("GetAssetEquipmentProperty Error");
		//	}

		//	return data;
		//}
	}
}
