using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VioAlarmQualityCheckUtility.Class;
using VioAlarmQualityCheckUtility.Properties;
using VioAlarmQualityCheckUtility.Windows;

namespace VioAlarmQualityCheckUtility
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string _password;
		private string _username;
		private readonly QualityCheck QualityCheck = new QualityCheck();
		private readonly SqlServer SqlServer = new SqlServer();


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
			Settings.Default.SqlServerInstance = "";
			Settings.Default.SqlServerDatabase = "";

			var SqlServerInstances = new List<string>();

			foreach (var item in SqlServer.GetLocalSqlInstances().ToList()) SqlServerInstances.Add(item);

			foreach (var item in SqlServer.GetSqlInstances()) SqlServerInstances.Add(item);

			SqlServerInstance_ComboBox.ItemsSource = SqlServerInstances;
			SelectBox.Visibility = Visibility.Visible;
		}


		// Run Click Event
		// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////a//////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Run(object sender, RoutedEventArgs e)
		{
			var canRun = SqlServerInstance_ComboBox.SelectedItem != null;

			if (SqlServerDatabase_ComboBox.SelectedItem == null) canRun = false;

			if (canRun)
			{
				if (_username != null && _password != null)
					QualityCheck.CheckAll(
						SqlServer.GetRemoteAlarmSources(SqlServerInstance_ComboBox.SelectedItem.ToString(), _username,
							_password));
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
			try
			{
				Settings.Default.SqlServerInstance = SqlServerInstance_ComboBox.SelectedItem.ToString();

				if (IntSec.IsChecked == true)
					SqlServerDatabase_ComboBox.ItemsSource = SqlServer.GetLocalSqlInstanceDatabases();
				else
					SqlServerDatabase_ComboBox.ItemsSource =
						SqlServer.GetSqlInstanceDatabases(e.AddedItems[0].ToString(), _username, _password);

				if (SqlServerInstance_ComboBox.SelectedIndex == -1)
					DBSelectBox.Visibility = Visibility.Hidden;
				else
				{
					SelectBox.Visibility = Visibility.Hidden;
					DBSelectBox.Visibility = Visibility.Visible;
				}
			}
			catch (Exception)
			{

			}
		}


		// SQL Server Database Combo Box Selection Changed
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		private void SqlServerDatabase_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				Settings.Default.SqlServerDatabase = SqlServerDatabase_ComboBox.SelectedItem.ToString();
				DBSelectBox.Visibility = Visibility.Hidden;
			}
			catch (Exception)
			{
				MessageBox.Show("Server not selected.");
			}
		}

		private void Report_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void IPSearchButton_Click(object sender, RoutedEventArgs e)
		{
			var SqlServerInstances = new List<string>();
			SqlServerInstance_ComboBox.SelectionChanged -= SqlServerInstance_ComboBox_SelectionChanged;
			SqlServerDatabase_ComboBox.SelectionChanged -= SqlServerDatabase_ComboBox_SelectionChanged;

			SqlServerInstance_ComboBox.ItemsSource = null;
			SqlServerDatabase_ComboBox.ItemsSource = null;

			if (IPTextBox.Text.Equals("."))
			{
				InitializeSqlServerData();
				IntSec.IsChecked = true;
			}
			else
			{
				try
				{
					var dialog = new UsernamePasswordDialog();

					if (dialog.ShowDialog() == true)
					{
						_username = dialog.Username;
						_password = dialog.Password;

						var sqlConn =
							new SqlConnection(
								$@"Data Source={IPTextBox.Text}; Network Library=DBMSSOCN; User ID={_username}; Password={_password}");

						UserSec.IsChecked = true;
						SqlServerInstances.Add(sqlConn.DataSource);
					}

					SqlServerInstance_ComboBox.ItemsSource = SqlServerInstances;
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}

			SqlServerInstance_ComboBox.SelectionChanged += SqlServerInstance_ComboBox_SelectionChanged;
			SqlServerDatabase_ComboBox.SelectionChanged += SqlServerDatabase_ComboBox_SelectionChanged;
		}


		private void AreaTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			throw new NotImplementedException();
		}

		private void radioButton_Checked(object sender, RoutedEventArgs e)
		{
			SqlServerInstance_ComboBox.SelectionChanged -= SqlServerInstance_ComboBox_SelectionChanged;
			SqlServerInstance_ComboBox.SelectedIndex = -1;
			SqlServerInstance_ComboBox.SelectionChanged += SqlServerInstance_ComboBox_SelectionChanged;
			var rb = sender as RadioButton;

			if (rb.Name.Equals("DifSec"))
			{
				var dialog = new UsernamePasswordDialog();
				dialog.ShowDialog();
				_username = dialog.Username;
				_password = dialog.Password;
			}
		}
	}
}