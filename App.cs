using System;
using System.Windows;
using System.Text;

namespace ProxyDisabler
{
    [Serializable()]
    public class Setting{
        public string LocalAddress { get; set; }
        public int LocalPortNo { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePortNo { get; set; }
        public string Auth  { get; set; }
        public uint Height  { get; set; }
        public uint Width  { get; set; }
        public bool AutoStart  { get; set; }
    }


    public partial class App : Application{
        public static bool consolemode;
        public static MainWindow w;
        public static Setting setting = new Setting();

        void InitConf(){
            setting.LocalAddress="localhost";
            setting.LocalPortNo=8080;
            setting.RemoteAddress="proxy.maizuru-ct.ac.jp";
            setting.RemotePortNo=8080;
            setting.Auth = "";
            setting.Height=200;
            setting.Width=400;
            setting.AutoStart=false;
        }
        void LoadConf(){
            System.IO.StreamReader sr = null;
            try{
                if(System.IO.File.Exists("Setting.xml")){
                    System.Xml.Serialization.XmlSerializer serializer2 = new System.Xml.Serialization.XmlSerializer(typeof(Setting));
                    sr = new System.IO.StreamReader("Setting.xml", new System.Text.UTF8Encoding(false));
                    setting = (Setting)serializer2.Deserialize(sr);
                    sr.Close();
                }else{
                    InitConf();
                }
            }
            catch(InvalidOperationException){
                InitConf();
                sr.Close();
            }
        }
        void SaveConf(){
            if(!System.IO.File.Exists("Setting.xml"))System.IO.File.Create("Setting.xml").Close();
            System.Xml.Serialization.XmlSerializer serializer1 = new System.Xml.Serialization.XmlSerializer(typeof(Setting));
            System.IO.StreamWriter sw = new System.IO.StreamWriter("Setting.xml", false, new System.Text.UTF8Encoding(false));
            serializer1.Serialize(sw, setting);
            sw.Close();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            SaveConf();
        }
        protected override void OnStartup(StartupEventArgs e){
            base.OnStartup(e);
            consolemode = false;
            foreach (string arg in e.Args){
                switch (arg){
                    case "-c":
                        consolemode = true;
                    break;
                    default:
                        MessageBox.Show("未知の引数：" + arg);
                        new MainWindow().Close();
                        return;
                }
            }
            LoadConf();
            w = new MainWindow();
            if(consolemode==false){
                w.Show();
            }
            // else{
            //     ServerService service = new ServerService(){
            //     };
            //     service.ServiceStart();
            //     Console.WriteLine("Press Enter to exit");
            //     // Console.ReadLine();
            //     service.ServiceEnd();
            //     // w.Close();
            // }
        }
    }
}
