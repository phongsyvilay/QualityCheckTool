using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ico.Fwx.ClientWrapper;
using VioAlarmQualityCheckUtility.Class;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Properties;
using VioAlarmQualityCheckUtility.Windows;

namespace VioAlarmQualityCheckUtility
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly QualityCheck _qualityCheck = new QualityCheck();
		private readonly SqlServer _sqlServer = new SqlServer();
		private string _netPassword = "SD#9136200";
		private string _netUsername = "Administrator";
		private string _serverPassword = "SD#9136200";
		private string _serverUsername = "sa";
		private List<AwxSource> _sources = new List<AwxSource>();
		private string _netConnection = "US3GRRPVIZEMU04.saespe.amcs.tld";


		/** Main Window **/
		public MainWindow()
		{
			InitializeComponent();
		}

		/************************************************************************
		 * Functions that have no attachment to the GUI
		 ************************************************************************/

		/** Initialize SQL Server Data **/
		public void InitializeSqlServerData(string input)
		{
			Settings.Default.SqlServerInstance = "";
			Settings.Default.SqlServerDatabase = "";

			var sqlServerInstances = new List<string>();

			if (input.Equals(".\\"))
				foreach (var item in SqlServer.GetLocalSqlInstances().ToList())
					sqlServerInstances.Add(item);
			else
				foreach (var item in _sqlServer.GetSqlInstances())
					sqlServerInstances.Add(item);

			SqlServerInstance_ComboBox.ItemsSource = sqlServerInstances;
			SelectBox.Visibility = Visibility.Visible;
		}

		/** Sets the server's authentication credentials from the view's textbox **/
		private void SetServerInfo()
		{
			_serverUsername = ServerUsernameBox.Text;
			_serverPassword = ServerPasswordBox.Password;
		}

		/**  **/
		public List<AwxSource> RecurseList(AreaModel area)
		{
			try
			{
				foreach (var source in area.SourcesList) _sources.Add(source);

				foreach (var child in area.Children)
				{
					if (child.SourcesList.Count != 0)
						foreach (var awxSource in child.SourcesList)
							_sources.Add(awxSource);

					if (child.Children.Count != 0)
						RecurseList(child);
				}

			}
			catch (Exception)
			{
				MessageBox.Show("List Error");
			}

			return _sources;
		}

		/************************************************************************
		 * Button action and event functions that are triggered from the view
		 ************************************************************************/

		/** Searches for sql server instances for the computer that is being targeted **/
		private void SearchButton_Click(object sender, RoutedEventArgs e)
		{
			SearchButtonUiClear();

			if (LocalMachine.IsChecked.Value)
			{
				InitializeSqlServerData(".\\");
			}
			else if (LocalNetwork.IsChecked.Value)
			{
				InitializeSqlServerData(".");
			}
			else
			{
				try
				{
					var sqlServerInstances = _sqlServer.GetRemoteInstances(_netConnection, _netUsername, _netPassword);
					SqlServerInstance_ComboBox.ItemsSource = sqlServerInstances;
					SelectBox.Visibility = Visibility.Visible;
				}
				catch (Exception)
				{
					MessageBox.Show("Searching Error");
				}
			}

			SqlServerInstance_ComboBox.SelectionChanged += SqlServerInstance_ComboBox_SelectionChanged;
			SqlServerDatabase_ComboBox.SelectionChanged += SqlServerDatabase_ComboBox_SelectionChanged;
			AreaTreeView.SelectedItemChanged += AreaTreeView_OnSelectedItemChanged;
		}

		/** If the search button is clicked this function is clearing the combo boxes and the text hovering over
		 * the combo boxes
		 **/
		private void SearchButtonUiClear()
		{
			SqlServerInstance_ComboBox.SelectionChanged -= SqlServerInstance_ComboBox_SelectionChanged;
			SqlServerDatabase_ComboBox.SelectionChanged -= SqlServerDatabase_ComboBox_SelectionChanged;
			AreaTreeView.SelectedItemChanged -= AreaTreeView_OnSelectedItemChanged;

			SqlServerInstance_ComboBox.ItemsSource = null;
			SqlServerDatabase_ComboBox.ItemsSource = null;
			Report.ItemsSource = null;
			AreaTreeView.ItemsSource = null;

			SelectBox.Visibility = Visibility.Hidden;
			DBSelectBox.Visibility = Visibility.Hidden;

		}

		/** sets the computer credentials based on the text in the textblocks**/
		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			_netUsername = netUsernameBox.Text;
			_netPassword = netPasswordBox.Password;
		}

		/** Sets the server information **/
		private void InstanceCreds_OnClick(object sender, RoutedEventArgs e)
		{
			SetServerInfo();
		}

		/** Sets the server information to empty**/
		private void InstanceClear_OnClick(object sender, RoutedEventArgs e)
		{
			ServerUsernameBox.Text = "";
			ServerPasswordBox.Password = "";
			SetServerInfo();

		}

		/** This button first is checking that items were selected
		 * in the combo boxes. Then it is grabbing those inputs no matter what they are. If the search
		 * box is looking at the local computer or local network than it is using the server instance name
		 * as the server name else it will put together the computer name and instance name.
		 * Grabs the sources from the DB, then checks the quality of the tags.
		 *  
		 **/
		public void Run(object sender, RoutedEventArgs e)
		{
			if (_qualityCheck.TestWorkbenchConnection())
			{
				if (SqlServerDatabase_ComboBox.SelectedItem != null)
				{
					string username;
					string password;
					var database = SqlServerDatabase_ComboBox.SelectedItem.ToString();
					var connection = netConnection.Text;
					var instance = SqlServerInstance_ComboBox.Text;

					if (RemoteMachine.IsChecked == true)
						instance = connection + "\\" + instance;

					if (_serverUsername != "" && _serverPassword != "")
					{
						username = _serverUsername;
						password = _serverPassword;
					}
					else
					{
						username = _netUsername;
						password = _netPassword;
					}

					try
					{
						List<AwxSource> sources;
						if (username != "" && password != "")
							sources = _sqlServer.GetRemoteAlarmSources(instance, username, password);
						else
							sources = _sqlServer.GetAlarmSources();

						_qualityCheck.CheckAll(sources);

						var ash = new AreaSourceHandler();

						var allAreas = ash.GetAreas(instance, database, username, password, sources);
						IList<AreaModel> selectedArea = allAreas.FindAll(i => i.RecursiveParentId == 0);
						AreaTreeView.ItemsSource = selectedArea;
					}
					catch (Exception)
					{
						MessageBox.Show("Could not run quality check.");
						//WorkbenchLogin login = new WorkbenchLogin();
						//login.Show();

					}
				}
			}
			else
			{
				WorkbenchLogin login = new WorkbenchLogin();

				if (login.ShowDialog() == true)
				{

				}
			}
		}


		/************************************************************************
		 * Radio button action and event functions that are triggered from the view
		 ************************************************************************/

		/** Computer connection radio buttons **/
		private void radioButton_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is RadioButton rb && rb.Name.Equals("RemoteMachine"))
			{
				NetCredentialPanel.Visibility = Visibility.Visible;
				netConnection.Visibility = Visibility.Visible;
			}
			else
			{
				if (NetCredentialPanel != null)
				{
					NetCredentialPanel.Visibility = Visibility.Collapsed;
					netConnection.Visibility = Visibility.Collapsed;
				}
			}
		}

		/** Server instance radio button selections **/
		private void ServRadioButton_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is RadioButton rb && rb.Name.Equals("ServDif"))
			{
				ServerCredentialPanel.Visibility = Visibility.Visible;
			}
			else
			{

				if (ServerCredentialPanel != null)
				{
					ServerCredentialPanel.Visibility = Visibility.Hidden;

					ServerUsernameBox.Text = "";
					ServerPasswordBox.Password = "";

					SetServerInfo();
				}
			}
		}

		/************************************************************************
		 * Combo boxes selection actions
		 ************************************************************************/

		/** When a server name is selected it is trying to connect to that instance with either
		 * windows authentication or a login and pw
		 **/
		private void SqlServerInstance_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				Settings.Default.SqlServerInstance = SqlServerInstance_ComboBox.SelectedItem.ToString();


				if (LocalMachine.IsChecked == true)
				{
					if (_serverUsername == "" && _serverPassword == "")
						SqlServerDatabase_ComboBox.ItemsSource = SqlServer.GetLocalSqlInstanceDatabases();
					else
						SqlServerDatabase_ComboBox.ItemsSource =
							SqlServer.GetSqlInstanceDatabases(Settings.Default.SqlServerInstance, _serverUsername,
								_serverPassword);
				}
				else
				{
					if (_serverUsername == "" && _serverPassword == "")
						SqlServerDatabase_ComboBox.ItemsSource = SqlServer.GetSqlInstanceDatabases(
							netConnection.Text + "\\" + Settings.Default.SqlServerInstance, _netUsername, _netPassword);
					else
						SqlServerDatabase_ComboBox.ItemsSource = SqlServer.GetSqlInstanceDatabases(
							netConnection.Text + "\\" + Settings.Default.SqlServerInstance, _serverUsername,
							_serverPassword);
				}

				SelectBox.Visibility = Visibility.Hidden;
				DBSelectBox.Visibility = Visibility.Visible;

			}
			catch (Exception)
			{
				SqlServerInstance_ComboBox.SelectionChanged -= SqlServerInstance_ComboBox_SelectionChanged;

				SqlServerInstance_ComboBox.SelectedIndex = -1;
				DBSelectBox.Visibility = Visibility.Hidden;
				SelectBox.Visibility = Visibility.Visible;

				SqlServerInstance_ComboBox.SelectionChanged += SqlServerInstance_ComboBox_SelectionChanged;
			}
		}

		/** SQL Server Database Combo Box Selection Changed **/
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


		/************************************************************************
		 * TreeView Items
		 ************************************************************************/

		/** Every time an item from the tree is selected it is goes and grabs all the sources from all the
		 *  children of that node and the node itself. It will again check the quality of those tags.
		 **/
		private void AreaTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (_sources.Count != 0)
				_sources.Clear();

			_sources = RecurseList((AreaModel)e.NewValue);

			Report.ItemsSource = _qualityCheck.CheckAll(_sources);
		}

		/************************************************************************
		 * Textbox and Passwordbox events
		 ************************************************************************/

		private void netPasswordBox_GotFocus(object sender, RoutedEventArgs e)
		{
			netPasswordBox.Password = _netPassword == "" ? "" : _netPassword;
		}

		private void netPasswordBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (netPasswordBox.Password == "")
			{
				netPasswordBox.Password = "Password";
				_netPassword = "";
			}
			else
			{
				_netPassword = netPasswordBox.Password;
			}
		}

		private void netUsernameBox_GotFocus(object sender, RoutedEventArgs e)
		{
			netUsernameBox.Text = _netUsername == "" ? "" : _netUsername;
		}

		private void netUsernameBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (netUsernameBox.Text == "")
			{
				netUsernameBox.Text = "Login";
				_netUsername = "";
			}
			else
			{
				_netUsername = netUsernameBox.Text.Trim();
			}
		}
		private void netConnection_GotFocus(object sender, RoutedEventArgs e)
		{
			netConnection.Text = _netConnection == "" ? "" : _netConnection;
		}

		private void netConnection_LostFocus(object sender, RoutedEventArgs e)
		{
			if (netConnection.Text == "")
			{
				netConnection.Text = "Name or IP Address";
				_netConnection = "";
			}
			else
			{
				_netConnection = netConnection.Text.Trim();
			}
		}

		private void ServerUsernameBox_GotFocus(object sender, RoutedEventArgs e)
		{
			ServerUsernameBox.Text = _serverUsername == "" ? "" : _serverUsername;
		}

		private void ServerUsernameBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (ServerUsernameBox.Text == "")
			{
				ServerUsernameBox.Text = "Username";
				_serverUsername = "";
			}
			else
			{
				_serverUsername = ServerUsernameBox.Text.Trim();
			}
		}

		private void ServerPasswordBox_GotFocus(object sender, RoutedEventArgs e)
		{
			ServerPasswordBox.Password = _serverPassword == "" ? "" : _serverPassword;
		}

		private void ServerPasswordBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (ServerPasswordBox.Password == "")
			{
				ServerPasswordBox.Password = "Password";
				_serverPassword = "";
			}
			else
			{
				_serverPassword = ServerPasswordBox.Password.Trim();
			}

		}
	}
}