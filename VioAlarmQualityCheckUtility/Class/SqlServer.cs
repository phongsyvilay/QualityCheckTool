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

		// Function:        Get local SQL server instances
		// Description:     Returns a collection of installed local SQL Server instances
		// ====================================================================================================================
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

		// Function:        Get local SQL server instances helper function
		// ====================================================================================================================
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

		// Function:        Get SQL server instances from a remote computer
		// ====================================================================================================================
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

		// Function:        Get Local SQL Server Instances
		// ====================================================================================================================
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
		// ====================================================================================================================
		public static List<string> GetLocalSqlInstanceDatabases()
		{
			Settings.Default.SqlConnectionString = "Data Source=" + Settings.Default.SqlServerInstance + ";Integrated Security=True";
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);
			return QueryDatabases();
		}

		// Function:        Get databases from sql server
		// ====================================================================================================================
		public static List<string> GetSqlInstanceDatabases(string instance, string username, string password)
		{
			Settings.Default.SqlConnectionString = $"Data Source={instance}; User ID={username}; Password={password}";
			_sqlConn = new SqlConnection(Settings.Default.SqlConnectionString);
			return QueryDatabases();
		}

		// Function:       Gets databases by querying the sql server
		// ====================================================================================================================
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
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				MessageBox.Show("Could not establish connection to selected SQL Server Instance.");
			}
			
			return databases;
		}


		// Function:		opening up a SQL connection to either a remote server or local SQL instance 
		//					and querying the SQL DB for the sources.
		// ====================================================================================================================
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

		// Function:        Updates the point name in SQL when edited in the datagrid from the window.
		// ====================================================================================================================
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
	}
}
