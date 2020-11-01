using System;
using System.Windows;
using System.Text;

namespace ProxyDisabler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();
            this.MinHeight = 140;
            this.MaxHeight = 200;
            this.MinWidth = 300;

            RemoteAddress.Text = App.setting.RemoteAddress;
            LocalAddress.Text = App.setting.LocalAddress;
            RemotePort.Text = App.setting.RemotePortNo.ToString();
            LocalPort.Text = App.setting.LocalPortNo.ToString();
            AutoStart.IsChecked = App.setting.AutoStart;
            try{
                User.Text = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(App.setting.Auth)).ToString().Split(':')[0];
                Password.Password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(App.setting.Auth)).ToString().Split(':')[1];
            }
            catch(Exception){
                User.Text = "";
                Password.Password = "";
            }
        }
        private void numonly(object sender, System.Windows.Input.TextCompositionEventArgs e){
            e.Handled = !new System.Text.RegularExpressions.Regex("[0-9]").IsMatch(e.Text);
        }
        private void Apply(object sender, RoutedEventArgs e){
            App.setting.Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(User.Text + ':' + Password.Password));
            App.setting.RemoteAddress = RemoteAddress.Text;
            App.setting.LocalAddress = LocalAddress.Text;
            App.setting.RemotePortNo = int.Parse(RemotePort.Text)>65535?8080:int.Parse(RemotePort.Text);
            App.setting.LocalPortNo = int.Parse(LocalPort.Text)>65535?8080:int.Parse(LocalPort.Text);
            App.setting.AutoStart = (bool)AutoStart.IsChecked;
            Close();
        }
        private void Cancel(object sender, RoutedEventArgs e){
            Close();
        }
    }
}
