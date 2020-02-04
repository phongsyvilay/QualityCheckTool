using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Microsoft.Win32;
using VioAlarmQualityCheckUtility.Models;

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

				var value = key.GetValue(valueName) as string[];
				if (value == null) return Enumerable.Empty<string>();

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
			RegistryKey envRegistryKey;
			List<string> instances = new List<string>();

			using (NetworkShareAccessor.Access(computerName, Environment.UserName, username, password))
			{
				RegistryView registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
				envRegistryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computerName, registryView);

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
			_sqlConn = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + ";Integrated Security=True");
			//List<string> databases = QueryDatabases(_sqlConn);
			//return databases;
			return QueryDatabases(_sqlConn);
		}

		public static List<string> GetSqlInstanceDatabases(string instance, string username, string password)
		{
			_sqlConn = new SqlConnection($"Data Source={instance}; User ID={username}; Password={password}");
			//List<string> databases = QueryDatabases(_sqlConn);
			//return databases;
			return QueryDatabases(_sqlConn);
		}

		public static List<string> QueryDatabases(SqlConnection sqlConn) 
		{
			List<string> databases = new List<string>();
			SqlCommand cmd = new SqlCommand();
			SqlDataReader reader;
			try
			{
				cmd.CommandText = "SELECT name FROM sys.databases;";
				cmd.CommandType = CommandType.Text;
				cmd.Connection = sqlConn;
				cmd.Connection.Open();

				reader = cmd.ExecuteReader();

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
		public List<AwxSource> GetAlarmSources()
		{
			//_sqlConn = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + ";Integrated Security=True");
			//List<AwxSource> data = QueryAwxSources(_sqlConn);
			//return data;
			return QueryAwxSources(_sqlConn);
		}

		public List<AwxSource> GetRemoteAlarmSources(string instance, string username, string password)
		{
			//_sqlConn = new SqlConnection($@"Data Source={instance}; User ID={username}; Password={password}");
			//List<AwxSource> data = QueryAwxSources(_sqlConn);
			//return data;
			return QueryAwxSources(_sqlConn);
		}

		public static List<AwxSource> QueryAwxSources(SqlConnection sqlConn)
		{
			SqlCommand cmd = new SqlCommand();
			List<AwxSource> data = new List<AwxSource>();

			try
			{
				cmd.CommandText = "SELECT Name, Input1, SourceID FROM " + Properties.Settings.Default.SqlServerDatabase + ".dbo.AWX_Source;";
				cmd.CommandType = CommandType.Text;
				cmd.Connection = sqlConn;
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
			}
			catch (Exception e)
			{
				throw e;
			}

			return data;
		}

		public void UpdateAwxSourceTagName(ReportModel sourceToUpdate, string newName)
		{
			SqlCommand cmd = new SqlCommand();

			try
			{
				cmd.Connection = _sqlConn;
				cmd.Connection.Open();
				cmd.CommandText =
					$"UPDATE {Properties.Settings.Default.SqlServerDatabase}.dbo.AWX_Source SET Name = '{newName}' WHERE SourceID = {sourceToUpdate.SourceID}";

				cmd.CommandType = CommandType.Text;
				cmd.ExecuteNonQuery();
				cmd.Connection.Close();

			}
			catch (SqlException)
			{
				MessageBox.Show("Unable to update tag name.");
				cmd.Connection.Close();
			}
			catch (Exception e)
			{
				MessageBox.Show("Unable to update tag name.");
				cmd.Connection.Close();
			}
		}

		public void UpdateAwxSourcePointName(ReportModel sourceToUpdate, string newName)
		{
			SqlCommand cmd = new SqlCommand();

			try
			{
				cmd.Connection = _sqlConn;
				cmd.Connection.Open();
				cmd.CommandText =
					$"UPDATE {Properties.Settings.Default.SqlServerDatabase}.dbo.AWX_Source SET Input1 = '{newName}' WHERE SourceID = {sourceToUpdate.SourceID}";

				cmd.CommandType = CommandType.Text;
				cmd.ExecuteNonQuery();
				cmd.Connection.Close();

			}
			catch (SqlException)
			{
				MessageBox.Show("Unable to update point name.");
				cmd.Connection.Close();
			}
			catch (Exception e)
			{
				MessageBox.Show("Unable to update point name");
				cmd.Connection.Close();
			}
		}


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
