using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyDisabler
{
    public class ServerService
    {
        public string LocalAddress { get; set; }
        public int LocalPortNo { get; set; }
        public int RemotePortNo { get; set; }
        public Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
        public string RemoteAddress { get; set; } = "10.1.16.8";
        public string Auth { get; set; }

        private static SemaphoreSlim pool;
        private Task taskListen;
        private TcpListener listner = null;
        public static bool ServerState;
        public static bool StopService;
        private int MaxTask = 100;
        private Action notifyCancel;

        public ServerService(){
        }

        public bool ServiceStart(){
            StopService=false;
            if(ServerState==false){
                taskListen = Task.Run(() => startListen());
                return true;
            }
            return false;
        }

        public bool ServiceEnd(){
            StopService=true;
            if(ServerState==true){
                ServerState=false;
                if (notifyCancel != null)
                    notifyCancel();
                notifyCancel = null;
                listner.Stop();
                return true;
            }
            return false;
        }

        private void startListen(){
            List<Task> taskList = new List<Task>();
            object lockObj = new object();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            notifyCancel = new Action(() => tokenSource.Cancel());
            try{
                var ipAddress = Dns.GetHostAddresses(LocalAddress).First();
                listner = new TcpListener(ipAddress, LocalPortNo);
                ServerState=true;
                listner.Start();
                // Console.WriteLine("{0} Port[{1}]の Listen を開始しました。", DateTime.Now.ToString(), LocalPortNo);
                pool = new SemaphoreSlim(MaxTask, MaxTask);
                while(true){
                    pool.Wait();
                    TcpClient toClient = listner.AcceptTcpClient();
                    if(StopService){
                        toClient.Dispose();
                        break;
                    }
                    // Console.WriteLine("{0} クライアントが接続しました。", DateTime.Now.ToString());
                    Task task = new Task(()=>doProcess(toClient,token),token);
                    lock(lockObj){
                        taskList.Add(task);
                        // Console.WriteLine("task数: {0}", taskList.Count);
                    }
                    task.ContinueWith((t)=>{
                        toClient.Close();
                        try{
                            pool.Release();
                        }
                        catch(ObjectDisposedException){
                        }
                        lock (lockObj){
                            taskList.Remove(task);
                            // Console.WriteLine("task数: {0}", taskList.Count);
                        }
                    });
                    task.Start();
                }
            }
            catch(SocketException e){
                switch (e.ErrorCode)
                {
                    case 10048:
                        // Console.WriteLine("{0} Socket exception: {1}, errCode({2})", DateTime.Now.ToString(), e.Message, e.ErrorCode);
                        if(!App.consolemode)
                            App.w.AddLog("ソケットエラーが発生しました。多重起動していませんか?");
                        break;
                    case 11001:
                        if(!App.consolemode)
                            App.w.AddLog("ローカルアドレスを間違えていませんか?");
                            ServiceEnd();
                        break;
                    default:
                        // Console.WriteLine("{0} Socket exception: {1}, errCode({2})", DateTime.Now.ToString(), e.Message, e.ErrorCode);
                        break;
                }
            }
            finally{
                if (tokenSource != null) tokenSource.Dispose();
            }
            Task[] tasks;
            lock (lockObj) {
                tasks = taskList.ToArray();
            }
            Task.WaitAll(tasks);
            if(pool!=null)pool.Dispose();
            // Console.WriteLine("task数: {0}", taskList.Count);
        }

        private void doProcess(TcpClient toClient, CancellationToken token)
        {
            TcpClient toServer = null;
            // ネットワークストリームを取得
            NetworkStream toClientStream = null;
            NetworkStream toServerStream = null;
            try
            {
                toClientStream = toClient.GetStream();
                byte[] data;
                data = readData(toClientStream);
                try{
                    toServer = new TcpClient(RemoteAddress,RemotePortNo);
                    toServerStream = toServer.GetStream();
                    // Console.WriteLine("サーバー({0}:{1})と接続しました({2}:{3})。",
                    // ((IPEndPoint)toServer.Client.RemoteEndPoint).Address,
                    // ((IPEndPoint)toServer.Client.RemoteEndPoint).Port,
                    // ((IPEndPoint)toServer.Client.LocalEndPoint).Address,
                    // ((IPEndPoint)toServer.Client.LocalEndPoint).Port);
                }
                catch (SocketException){
                    // Console.WriteLine("プロキシサーバーに接続出来ませんでした。");
                    if(!App.consolemode)
                        App.w.AddLog("プロキシサーバーに接続出来ませんでした。");
                    return;
                }

                string method;
                (data,method) = makeReq(data);
                toServerStream.Write(data, 0, data.Length);

                byte[] resBytes = new byte[8192];
                int resSize;
                Int32 wait = 0;
                if(method=="CONNECT")
                    do
                    {
                        if(toServerStream.DataAvailable){
                            resSize = toServerStream.Read(resBytes, 0, resBytes.Length);
                            toClientStream.Write(resBytes, 0, resSize);
                            wait=0;
                        }else if(toClientStream.DataAvailable){
                            resSize = toClientStream.Read(resBytes, 0, resBytes.Length);
                            toServerStream.Write(resBytes, 0, resSize);
                            wait=0;
                        }else wait+=10;
                        Thread.Sleep(wait);
                        if(wait>700){
                            break;
                        }
                        token.ThrowIfCancellationRequested();
                    }while (GetState(toClient)==TcpState.Established&&GetState(toServer)==TcpState.Established);
                else{
                    do
                    {
                        if(toServerStream.DataAvailable){
                            resSize = toServerStream.Read(resBytes, 0, resBytes.Length);
                            toClientStream.Write(resBytes, 0, resSize);
                            wait=0;
                        }else if(toClientStream.DataAvailable){
                            resSize = toClientStream.Read(resBytes, 0, resBytes.Length);
                            (data,method) = makeReq(resBytes);
                            toServerStream.Write(data, 0, data.Length);
                            wait=0;
                        }else wait+=10;
                        Thread.Sleep(wait);
                        if(wait>700){
                            break;
                        }
                        token.ThrowIfCancellationRequested();
                    }while (GetState(toClient)==TcpState.Established&&GetState(toServer)==TcpState.Established);
                }
                // Console.WriteLine("切断しました。");
            }
            catch (OperationCanceledException) {
            }
            finally {
                if (toClientStream != null)
                    toClientStream.Close();
                toClient.Close();
                if (toServerStream != null)
                    toServerStream.Close();
                if (toServer != null)
                    toServer.Close();
            }
        }
        private (byte[],string) makeReq(byte[] resBytes){
            string rString = Encoding.GetString(resBytes);
            string[] reqs = {""};
            string method = "";
            string url = "";
            string reqMsg = "";
            try{
                reqs = rString.Split("\r\n\r\n")[0].Split("\r\n");
                method = reqs[0].Split(' ')[0];
                url = reqs[0].Split(' ')[1];
            }
            catch(Exception){
                // Console.WriteLine(rString);
                method = "RAW";
            }

            if(method =="CONNECT"){
                string host = url.Split(':')[0];
                string ip;
                try{
                    ip = Dns.GetHostAddresses(host).First().ToString();
                }
                catch(SocketException){
                    ip = host;
                }
                reqMsg = reqs[0].Replace(host, ip) + "\r\n" + "Proxy-Authorization: Basic " + Auth + "\r\n";
            }else if(method == "RAW"){
                reqMsg = rString;
                return (resBytes,method);
            }else{
                reqMsg = reqs[0] + "\r\n" + "Proxy-Authorization: Basic " + Auth + "\r\n";
            }
            for (int i = 1; i < reqs.Length ; i++) {
                reqMsg+=reqs[i] + "\r\n";
            }
            reqMsg+= "\r\n";
            // Console.WriteLine(reqMsg);
            return (Encoding.ASCII.GetBytes(reqMsg),method);
        }
        private byte[] readData(NetworkStream Stream){
            byte[] resBytes = new byte[1024];
            int resSize = 0;
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            do
            {
                resSize = Stream.Read(resBytes, 0, resBytes.Length);
                if (resSize == 0)
                {
                    // Console.WriteLine("クライアントが切断しました。");
                    break;
                }
                ms.Write(resBytes, 0, resSize);
            } while (Stream.DataAvailable);
            byte[] data = ms.ToArray();
            ms.Close();
            return data;
        }
        public static TcpState GetState(TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint));
            return foo != null ? foo.State : TcpState.Unknown;
        }
    }
}
