using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using VioAlarmQualityCheckUtility.Class;
using VioAlarmQualityCheckUtility.Models;
using System.Data.SqlClient;
using VioAlarmQualityCheckUtility.Windows;

namespace VioAlarmQualityCheckUtility
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		QualityCheck QualityCheck = new QualityCheck();
		SqlServer SqlServer = new SqlServer();
		private string username;
		private string password;



		// Main Window
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public MainWindow()
		{
			InitializeComponent();

			//InitializeSqlServerData();
		}




		// Initialize SQL Server Data
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void InitializeSqlServerData()
		{
			Properties.Settings.Default.SqlServerInstance = "";
			Properties.Settings.Default.SqlServerDatabase = "";

			List<string> SqlServerInstances = new List<string>();

			foreach (var item in SqlServer.GetLocalSqlInstances().ToList())
			{
				SqlServerInstances.Add(item);
			}

			foreach (var item in SqlServer.GetSqlInstances())
			{
				SqlServerInstances.Add((item));
			}

			SqlServerInstance_ComboBox.ItemsSource = SqlServerInstances;
		}




		// Run Click Event
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Run(object sender, RoutedEventArgs e)
		{
			bool canRun = true;

			if (SqlServerInstance_ComboBox.SelectedItem == null)
			{
				canRun = false;
			}

			if (SqlServerDatabase_ComboBox.SelectedItem == null)
			{
				canRun = false;
			}

			if (canRun)
			{
				if(username != null && password != null)
					QualityCheck.CheckAll(SqlServer.GetRemoteAlarmSources(SqlServerInstance_ComboBox.SelectedItem.ToString(), username, password));
				else 
					QualityCheck.CheckAll(SqlServer.GetAlarmSources());
			}
			else
			{
				MessageBox.Show("Invalid SQL Server Selection.");
			}
		}




		// SQL Server Instance Combo Box Selection Changed
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		private void SqlServerInstance_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Properties.Settings.Default.SqlServerInstance = SqlServerInstance_ComboBox.SelectedItem.ToString();
			if (IntSec.IsChecked == true)
				SqlServerDatabase_ComboBox.ItemsSource = SqlServer.GetLocalSqlInstanceDatabases();
			else
			{
				var dialog = new UsernamePasswordDialog();
				if (dialog.ShowDialog() == true)
				{
					username = dialog.Username;
					password = dialog.Password;

					SqlServerDatabase_ComboBox.ItemsSource =
						SqlServer.GetSqlInstanceDatabases(e.AddedItems[0].ToString(), username, password);
				}
			}
		}




		// SQL Server Database Combo Box Selection Changed
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		private void SqlServerDatabase_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Properties.Settings.Default.SqlServerDatabase = SqlServerDatabase_ComboBox.SelectedItem.ToString();
		}

		private void Report_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void IPSearchButton_Click(object sender, RoutedEventArgs e)
		{
			IPValidator validator = new IPValidator();
			bool validation = validator.validateipv4(IPTextBox.Text);
			List<string> SqlServerInstances = new List<string>();

			if (IPTextBox.Text.Equals("."))
			{
				InitializeSqlServerData();
				SqlServer.GetSqlInstances();

			}
			else
			{
				try
				{
					if (IntSec.IsChecked == true)
					{
						SqlConnection sqlConn =
							new SqlConnection(
								$@"Data Source={IPTextBox.Text}; Network Library=DBMSSOCN; Integrated Security = True");

						if (sqlConn.State.Equals(System.Data.ConnectionState.Closed))
						{
							MessageBox.Show("Connection is closed. Try again");
						}
					}
					else
					{
						var dialog = new UsernamePasswordDialog();

						if (dialog.ShowDialog() == true)
						{
							username = dialog.Username;
							password = dialog.Password;

							SqlConnection sqlConn =
								new SqlConnection(
									$@"Data Source={IPTextBox.Text}; Network Library=DBMSSOCN; User ID={username}; Password={password}");
							sqlConn.Open();
							SqlServerInstances.Add(sqlConn.DataSource);
						}

					}

					SqlServerInstance_ComboBox.ItemsSource = SqlServerInstances;
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}
			//else
			//	MessageBox.Show("That is not a valid IP Address.");
		}


		private void AreaTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			throw new NotImplementedException();
		}
	}
}
