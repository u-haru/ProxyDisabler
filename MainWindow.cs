using System;
using System.Windows;

namespace ProxyDisabler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ServerService service = new ServerService();
        
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += Window_Closing;
            this.MinHeight = 120;
            this.MinWidth = 200;
            LogBox.Text = "Hello";
            Button_Start.IsEnabled = true;
            Button_Stop.IsEnabled = false;

            
            this.Height = App.setting.Height;
            this.Width = App.setting.Width;
            service.LocalAddress = App.setting.LocalAddress;
            service.LocalPortNo = App.setting.LocalPortNo;
            service.RemotePortNo = App.setting.RemotePortNo;
            service.Auth = App.setting.Auth;
            if(App.setting.AutoStart)Start(null,null);
        }
        public void AddLog(string str){
            this.Dispatcher.Invoke((Action)(() =>
            {
                LogBox.Text += "\n" + str;
                LogBox.ScrollToEnd();
            }));
        }
        public void SetLog(string str){
            this.Dispatcher.Invoke((Action)(() =>
            {
                LogBox.Text = str;
                LogBox.ScrollToEnd();
            }));
        }
        private void Start(object sender, RoutedEventArgs e)
        {
            Button_Stop.IsEnabled = true;
            Button_Start.IsEnabled = false;
            Button_Config.IsEnabled = false;
            service.LocalAddress = App.setting.LocalAddress;
            service.LocalPortNo = App.setting.LocalPortNo;
            service.RemoteAddress = App.setting.RemoteAddress;
            service.RemotePortNo = App.setting.RemotePortNo;
            service.Auth = App.setting.Auth;
            if(service.ServiceStart()){
                SetLog("サービスを開始しました。");
                AddLog("設定でプロキシのアドレスを" + App.setting.LocalAddress + "に、ポートを" + App.setting.LocalPortNo.ToString() + "に設定してください。");
            }
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            Button_Start.IsEnabled = true;
            Button_Stop.IsEnabled = false;
            Button_Config.IsEnabled = true;
            if(service.ServiceEnd()){
                AddLog("サービスを停止しました。");
            }
        }
        private void Config(object sender, RoutedEventArgs e)
        {
            ConfigWindow window = new ConfigWindow();
            window.Top = this.Top + 10;
            window.Left = this.Left + 10;
            window.ShowDialog();
        }

        protected virtual void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.setting.Height = (uint)Height;
            App.setting.Width = (uint)Width;
            if ((Button_Stop.IsEnabled == true) && MessageBoxResult.Yes != MessageBox.Show("稼働中です。閉じてもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Information))
            {
                e.Cancel = true;
                return;
            }
            service.ServiceEnd();
        }
    }
}
