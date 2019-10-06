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
using System.Windows.Shapes;

namespace VioAlarmQualityCheckUtility.Windows
{
	/// <summary>
	/// Interaction logic for UsernamePasswordDialog.xaml
	/// </summary>
	public partial class UsernamePasswordDialog : Window
	{
		public UsernamePasswordDialog()
		{
			InitializeComponent();
		}

		public string Username
		{
			get => usernameBox.Text;
			set => usernameBox.Text = value;
		}

		public string Password
		{
			get => passwordBox.Text;
			set => passwordBox.Text = value;
		}

		private void confirm_OnClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
