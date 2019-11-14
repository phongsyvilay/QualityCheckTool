using System;
using System.Windows;
using Ico.Fwx.ClientWrapper;

namespace VioAlarmQualityCheckUtility.Windows
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class WorkbenchLogin : Window
	{
		public WorkbenchLogin()
		{
			InitializeComponent();
		}


		/** Used to sign into workbench. This is needed if the user is seeing Bad - User Access Denied as the quality. **/
		private void WorkBenchSignInButton_Click(object sender, RoutedEventArgs e)
		{
			FwxClientWrapper wrapper = new FwxClientWrapper();

			if (WorkUser.Text != "" && WorkPassword.Password != "")
			{
				try
				{
					Status status = wrapper.Login(WorkUser.Text, WorkPassword.Password);
					if (status.IsGood)
						this.Close();
					else
						MessageBox.Show("Login failed");
				}
				catch (Exception)
				{
					MessageBox.Show("Login error.");
				}

			}

		}
	}
}
