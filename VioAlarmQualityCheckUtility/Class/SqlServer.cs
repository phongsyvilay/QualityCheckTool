using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Microsoft.Win32;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Windows;
using System.Configuration;

namespace VioAlarmQualityCheckUtility.Class
{
	class SqlServer
	{
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



		public List<string> GetSqlInstances()
		{
			System.Data.Sql.SqlDataSourceEnumerator instance = System.Data.Sql.SqlDataSourceEnumerator.Instance;
			DataTable dataTable = instance.GetDataSources();
			List<string> list = new List<string>();

			foreach (DataRow row in dataTable.Rows)
			{
				string temp = row.ItemArray[0].ToString();

				if (row.ItemArray[1].ToString() != "")
					temp += $@"\{row.ItemArray[1].ToString()}";

				list.Add(temp);
			}

			return list;

		}

		// Function:        Get Databases from Selected Local or Remote SQL Server Instance
		// Description:     Returns a collection of databases from the user selected local SQL Server instance
		// ==============================================================================================================================================================
		public static List<string> GetLocalSqlInstanceDatabases()
		{
			SqlConnection sqlConn = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + ";Integrated Security=True");
			List<string> databases = QueryDatabases(sqlConn);
			return databases;
		}

		public static List<string> GetSqlInstanceDatabases(string instance, string username, string password)
		{
			SqlConnection sqlConn = new SqlConnection($"Data Source={instance}; User ID={username}; Password={password}");
			List<string> databases = QueryDatabases(sqlConn);
			return databases;
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

				if (cmd.Connection.State == ConnectionState.Closed)
				{
					var dialog = new UsernamePasswordDialog();
					var username = dialog.Username;
					var password = dialog.Password;

					sqlConn = new SqlConnection(
						$"Data Source={sqlConn.DataSource}; User ID={username}; Password={password}");
					cmd.Connection.Open();
				}

				reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					databases.Add(reader[0].ToString());
				}

				reader.Close();

				cmd.Connection.Close();

			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not establish connection to selected SQL Server Instance. Try using a username and password.");
				((MainWindow)Application.Current.MainWindow).SqlServerInstance_ComboBox.SelectedIndex = -1;
			}
			
			return databases;
		}




		// Functions: GetAlarmSources, GetRemoteAlarmSources, and QueryAwxSources         
		// Description: These are all related to opening up a SQL connection to either a remote server or local SQL instance and querying the SQL DB for the sources.
		// ==============================================================================================================================================================
		public List<AwxSource> GetAlarmSources()
		{
			SqlConnection sqlConn = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + ";Integrated Security=True");
			List<AwxSource> data = QueryAwxSources(sqlConn);
			return data;
		}

		public List<AwxSource> GetRemoteAlarmSources(string instance, string username, string password)
		{
			SqlConnection sqlConn = new SqlConnection($@"Data Source={instance}; User ID={username}; Password={password}");
			List<AwxSource> data = QueryAwxSources(sqlConn);
			return data;
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

					var name = reader[0];
					var input1 = reader[1];
					var id = reader[2];
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
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}

			return data;
		}

		// Function:        
		// Description:     
		// ==============================================================================================================================================================
		public void QueryAwxSourcesToArea()
		{

		}


		// Function:        
		// Description:     
		// ==============================================================================================================================================================
		public List<AscEquipmentProperty> GetAssetEquipmentProperties()
		{
			List<AscEquipmentProperty> data = new List<AscEquipmentProperty>();

			SqlConnection sqlConn = new SqlConnection("Data Source=" + Properties.Settings.Default.SqlServerInstance + ";Integrated Security=True");

			SqlCommand cmd = new SqlCommand();

			try
			{
				cmd.CommandText = "SELECT ParentID, Name, RealtimePointName FROM " + Properties.Settings.Default.SqlServerDatabase + ".dbo.ASC_EquipmentProperties;";
				cmd.CommandType = CommandType.Text;
				cmd.Connection = sqlConn;
				cmd.Connection.Open();

				var reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					var ascEquipmentProperty = new AscEquipmentProperty
					{
						ParentID = reader[0].ToString(),
						Name = reader[1].ToString(),
						RealtimePointName = reader[2].ToString()
					};

					data.Add(ascEquipmentProperty);
				}

				reader.Close();

				cmd.Connection.Close();
			}
			catch (Exception)
			{

			}

			return data;
		}
	}
}
