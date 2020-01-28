﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using VioAlarmQualityCheckUtility.Class;
using VioAlarmQualityCheckUtility.Models;
using VioAlarmQualityCheckUtility.Properties;

namespace VioAlarmQualityCheckUtility
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	///
	public partial class MainWindow
	{
		private readonly SqlServer _sqlServer = new SqlServer();
		List<ReportModel> _allReports = new List<ReportModel>();
		private List<ReportModel> _foundReports = new List<ReportModel>();

		private string _netPassword = "";
		private string _netUsername = "";
		private string _netConnection = "";
		private string _serverPassword = "";
		private string _serverUsername = "";

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

		/** Generates all the sources that are connected with the selected area and its children **/
		public List<ReportModel> RecurseList(AreaModel area)
		{
			try
			{
				foreach (var source in area.SourcesList)
				{
					var found = _allReports.FindAll(s => s.TagName == source.Name && s.Area == source.AreaName);
					_foundReports.AddRange(found);
				}

				foreach (var child in area.Children)
				{
					if (child.SourcesList.Count != 0)
						foreach (var awxSource in child.SourcesList)
						{
							var found = _allReports.FindAll(s =>
								s.TagName == awxSource.Name && s.Area == awxSource.AreaName);
							_foundReports.AddRange(found);
						}

					if (child.Children.Count != 0)
						RecurseList(child);
				}

			}
			catch (Exception)
			{
				MessageBox.Show("List Error");
			}

			return _foundReports;
		}

		/************************************************************************
		 * Button action and event functions that are triggered from the view
		 ************************************************************************/

		/** Searches for sql server instances for the computer that is being targeted **/
		private void SearchButton_Click(object sender, RoutedEventArgs e)
		{
			SearchButtonUiClear();

			if (LocalMachine.IsChecked != null && LocalMachine.IsChecked.Value)
			{
				InitializeSqlServerData(".\\");
			}
			else if (LocalNetwork.IsChecked != null && LocalNetwork.IsChecked.Value)
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

		/** This button first is checking that items were selected
		 * in the combo boxes. Then it is grabbing those inputs no matter what they are. If the search
		 * box is looking at the local computer or local network than it is using the server instance name
		 * as the server name else it will put together the computer name and instance name.
		 * Grabs the sources from the DB, then checks the quality of the tags.
		 *  
		 **/

		public void Run(object sender, RoutedEventArgs e)
		{
			if (SqlServerDatabase_ComboBox.SelectedItem != null)
			{
				string username;
				string password;
				var database = SqlServerDatabase_ComboBox.SelectedItem.ToString();
				var connection = NetConnection.Text;
				var instance = SqlServerInstance_ComboBox.Text;
				QualityCheck qualityCheck = new QualityCheck();

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

					var ash = new AreaSourceHandler();
					var allAreas = ash.GetAreas(instance, database, username, password, sources);
					IList<AreaModel> selectedArea = allAreas.FindAll(i => i.RecursiveParentId == 0);

					AreaTreeView.ItemsSource = selectedArea;
					Report.ItemsSource = _allReports = qualityCheck.CheckAll(allAreas[0].SourcesList);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
					MessageBox.Show("Could not run quality check.");

				}
			}
		}

		/** Search button that is searching for the input which is a tag **/
		private void TagSearchButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string tag = TagSearchbox.Text;

				if (tag != "")
				{
					TagSearch(tag, SearchExact.IsChecked == true ? "exact" : "contains");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Tag search error.");
			}
		}


		private void PointSearchButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string tag = PointSearchbox.Text;

				if (tag != "")
				{
					if (tag != "")
						PointSearch(tag, PointExact.IsChecked == true ? "exact" : "contains");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Point name search error.");
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
				NetConnection.Visibility = Visibility.Visible;
			}
			else
			{
				if (NetCredentialPanel != null)
				{
					NetCredentialPanel.Visibility = Visibility.Collapsed;
					NetConnection.Visibility = Visibility.Collapsed;
					_netPassword = "";
					_netUsername = "";
					_netConnection = "";
					NetConnection.Text = "Name or IP Address";
					NetUsernameBox.Text = "Login";
					NetPasswordBox.Password = "Password";

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
					ServerCredentialPanel.Visibility = Visibility.Collapsed;
					_serverUsername = "";
					_serverPassword = "";
					ServerUsernameBox.Text = "Username";
					ServerPasswordBox.Password = "Password";

				}
			}
		}

		/** The following two search functions are dependent on which radio button is selected. Will keep these separate
		 *	since they are searching within different properties. **/
		private void TagSearch(string tag, string searchType)
		{
			List<ReportModel> foundReports = new List<ReportModel>();

			if (searchType == "contains")
			{
				foundReports = _allReports.FindAll(r => r.TagName.Contains(tag));
			}
			else if (searchType == "exact")
				foundReports = _allReports.FindAll(r => r.TagName == tag);


			if (foundReports.Count == 0)
			{
				Report.ItemsSource = null;
				OverlayText.Text = "No tags were found.";
				Overlay.Visibility = Visibility.Visible;

			}
			else
			{
				Overlay.Visibility = Visibility.Collapsed;
				Report.ItemsSource = foundReports;
			}

			Report.Items.Refresh();
		}


		private void PointSearch(string pointName, string searchType)
		{
			List<ReportModel> foundReports = new List<ReportModel>();

			if (searchType == "contains")
			{
				foundReports = _allReports.FindAll(r => r.PointName.Contains(pointName));
			}
			else if (searchType == "exact")
				foundReports = _allReports.FindAll(r => r.PointName == pointName);


			if (foundReports.Count == 0)
			{
				Report.ItemsSource = null;
				OverlayText.Text = "No tags were found.";
				Overlay.Visibility = Visibility.Visible;
			}
			else
			{
				Overlay.Visibility = Visibility.Collapsed;
				Report.ItemsSource = foundReports;
			}

			Report.Items.Refresh();
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


				if (LocalMachine.IsChecked == true || LocalNetwork.IsChecked == true)
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
							_netConnection + "\\" + Settings.Default.SqlServerInstance, _netUsername, _netPassword);
					else
						SqlServerDatabase_ComboBox.ItemsSource = SqlServer.GetSqlInstanceDatabases(
							_netConnection + "\\" + Settings.Default.SqlServerInstance, _serverUsername,
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

			_foundReports.Clear();
			_foundReports = RecurseList((AreaModel)e.NewValue);

			if (_foundReports.Count == 0)
			{
				Report.ItemsSource = null;
				OverlayText.Text = "There are no tags in this area.";
				Overlay.Visibility = Visibility.Visible;
			}
			else
			{
				Overlay.Visibility = Visibility.Collapsed;
				Report.ItemsSource = _foundReports;
			}

			Report.Items.Refresh();
		}

		/************************************************************************
		 * Textbox and Passwordbox events
		 ************************************************************************/

		private void netPasswordBox_GotFocus(object sender, RoutedEventArgs e)
		{
			NetPasswordBox.Password = _netPassword == "" ? "" : _netPassword;
		}

		private void netPasswordBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (NetPasswordBox.Password == "")
			{
				NetPasswordBox.Password = "Password";
				_netPassword = "";
			}
			else
			{
				_netPassword = NetPasswordBox.Password;
			}
		}

		private void netUsernameBox_GotFocus(object sender, RoutedEventArgs e)
		{
			NetUsernameBox.Text = _netUsername == "" ? "" : _netUsername;
		}

		private void netUsernameBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (NetUsernameBox.Text == "")
			{
				NetUsernameBox.Text = "Login";
				_netUsername = "";
			}
			else
			{
				_netUsername = NetUsernameBox.Text.Trim();
			}
		}

		private void netConnection_GotFocus(object sender, RoutedEventArgs e)
		{
			NetConnection.Text = _netConnection == "" ? "" : _netConnection;
		}

		private void netConnection_LostFocus(object sender, RoutedEventArgs e)
		{
			if (NetConnection.Text == "")
			{
				NetConnection.Text = "Name or IP Address";
				_netConnection = "";
			}
			else
			{
				_netConnection = NetConnection.Text.Trim();
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

		private void TagSearchbox_GotFocus(object sender, RoutedEventArgs e)
		{
			TagSearchbox.Text = TagSearchbox.Text == "Tag Name" ? "" : TagSearchbox.Text;
		}

		private void TagSearchbox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (TagSearchbox.Text == "")
			{
				TagSearchbox.Text = "Tag Name";
			}
			else
			{
				TagSearchbox.Text = TagSearchbox.Text.Trim();
			}
		}

		private void PointSearchbox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (PointSearchbox.Text == "")
			{
				PointSearchbox.Text = "Point Name";
			}
			else
			{
				PointSearchbox.Text = PointSearchbox.Text.Trim();
			}
		}

		private void PointSearchbox_GotFocus(object sender, RoutedEventArgs e)
		{
			PointSearchbox.Text = PointSearchbox.Text == "Point Name" ? "" : PointSearchbox.Text;

		}

		private void Report_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			bool different = false;
			string editedText = ((TextBox)e.EditingElement).Text;
			ReportModel rm = (ReportModel) e.Row.Item;

			if (((DataGrid)sender).CurrentColumn.Header.ToString() == "Tag Name")
			{
				if (editedText != rm.TagName)
					different = true;

				if(different)
					_sqlServer.UpdateAwxSourceTagName(rm, editedText);
			}
			else
			{
				// Goes into this statement for point name 
				if (editedText != rm.PointName)
					different = true;

				if (different)
				{
					if (rm.PointName.Contains("x="))
					{
						Console.WriteLine("here");
					}
					_sqlServer.UpdateAwxSourcePointName(rm, editedText);
				}
			}

		}
	}
	/************************************************************************
	 * Unused methods
	 ************************************************************************/
	/** Part of the searching tag function. This opens the area in the treeview if it is found within
	 * the current itemcontrol being recursed **/
	//public static void OpenTreeViewItem(ItemsControl ic, object area)
	//{

	//	ic.UpdateLayout();

	//	TreeViewItem tvi = ic.ItemContainerGenerator.ContainerFromItem(area) as TreeViewItem;

	//	if (tvi == null)
	//	{
	//		foreach (object i in ic.Items)
	//		{
	//			//Get the TreeViewItem associated with the iterated object model
	//			TreeViewItem tvi2 = ic.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;

	//			if (tvi2 == null)
	//				break;
	//			else
	//				OpenTreeViewItem(tvi2, area);

	//		}
	//	}
	//	else
	//	{
	//		tvi.IsExpanded = true;
	//		tvi.IsSelected = true;
	//	}

	/** This creates the list of parents that need to be found within the treeview. It is starting with the
	 * root that needs to be found first, then working down a tree until the tag's parent is found **/
	//private List<AreaModel> FindListOfParents(string searchString, string searchProperty)
	//{
	//	List<AreaModel> returnedAreaModels = new List<AreaModel>();

	//	foreach (AreaModel area in ((List<AreaModel>)AreaTreeView.ItemsSource))
	//	{
	//		List<AwxSource> sources = RecurseList(area);
	//		try
	//		{
	//			AwxSource source = searchProperty == "Name"
	//				? sources.Find(s => s.Name == searchString)
	//				: sources.Find(s => s.Input1 == searchString);

	//			if (source != null)
	//			{
	//				var parentAreas = source.AreaName.Split('\\');

	//				foreach (var parentArea in parentAreas)
	//				{
	//					AreaModel tempArea = _allAreas.Find(a => a.Name == parentArea);
	//					returnedAreaModels.Add(tempArea);
	//				}

	//				break;
	//			}

	//		}
	//		catch (Exception ex)
	//		{
	//			MessageBox.Show(ex.Message);
	//		}
	//	}

	//	return returnedAreaModels;
	//}


}


