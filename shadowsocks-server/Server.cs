//#define DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using shadowsocks_csharp;
using System.Threading;
using System.Timers;

namespace shadowsocks
{
    class ControlServer
    {
        private Config config;
        Dictionary<int, Server> lstServer = new Dictionary<int, Server>();
        public class UdpState
        {
            public UdpClient udpclient = null;
            public IPEndPoint remoteIpEndPoint;
            public UdpState(UdpClient udpclient, IPEndPoint remoteIpEndPoint)
            {
                this.udpclient = udpclient;
                this.remoteIpEndPoint = remoteIpEndPoint;
            }
        }

        public ControlServer(Config config)
        {
            this.config = config;
        }

        public void Start()
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 4000);
            UdpClient udpClient = new UdpClient(remoteIpEndPoint);
            udpClient.BeginReceive(ReceiveCallBack, new UdpState(udpClient, remoteIpEndPoint));
        }

        public void ReceiveCallBack(IAsyncResult ar)
        {
            UdpClient udpClient = null;
            UdpState udpState = null;
            try
            {
                udpState = (UdpState)ar.AsyncState;
                udpClient = udpState.udpclient;
                byte[] data = udpClient.EndReceive(ar, ref udpState.remoteIpEndPoint);
                string strdata = Encoding.ASCII.GetString(data);
                string[] arrdata = strdata.Split(':');
                if (arrdata.Length == 4)
                {
                    //if (arrdata[0] == "hi")//!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    {
                        int port = int.Parse(arrdata[1]);
                        if (arrdata[3] == "0")
                        {
                            //del
                            if (lstServer.ContainsKey(port))
                            {
                                //stop server only!
                                lstServer[port].Stop();
                                lstServer.Remove(port);
                                Console.WriteLine(String.Format("server {0} stopping...", port));
                            }
                        }
                        if (arrdata[3] == "1")
                        {
                            //new
                            if (lstServer.ContainsKey(port))
                            {
                                if (lstServer[port].config.password != arrdata[2])
                                {
                                    //Restarted
                                    Console.WriteLine(String.Format("server {0} already run but passwd changed", port));
                                    lstServer[port].Stop();
                                    lstServer.Remove(port);
                                    Console.WriteLine(String.Format("server {0} stopping...", port));
                                    Config config = this.config.Copy();
                                    config.server_port = port;
                                    config.password = arrdata[2];
                                    lstServer[port] = new Server(config);
                                    lstServer[port].Start();
                                    Console.WriteLine(String.Format("server {0} starting...", port));
                                }
                                else
                                {
                                    Console.WriteLine(String.Format("server {0} already run", port));
                                }
                            }
                            else
                            {
                                Config config = this.config.Copy();
                                config.server_port = port;
                                config.password = arrdata[2];
                                lstServer[port] = new Server(config);
                                lstServer[port].Start();
                                Console.WriteLine(String.Format("server {0} starting...", port));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                try
                {
                    udpClient.BeginReceive(ReceiveCallBack, udpState);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
    
    class ServerTransfer
    {
        public int nSetCount = 1000;
        public UInt64 transfer_upload = 0;
        public UInt64 transfer_download = 0;
    }

    class Server
    {
        public Config config;
        private Socket listener;
        private ServerTransfer transfer;

        public Server(Config config)
        {
            this.config = config;
            this.transfer = new ServerTransfer();
        }

        public void SetTransfer(UInt64 du, UInt64 dd)
        {
            bool bSend = false;
            lock (this.transfer)
            {
                this.transfer.transfer_upload += du;
                this.transfer.transfer_download += dd;
                //2KB~2MB
                if (this.transfer.nSetCount-- <= 0)
                {
                    this.transfer.nSetCount = 1000;
                    bSend = true;
                }
            }
            if (bSend)
                UpdateTransfer();
        }

        public void UpdateTransfer()
        {
            try
            {
                string strdata = null;
                lock (this.transfer)
                {
                    strdata = String.Format("{0}:{1}:{2}", this.config.server_port, this.transfer.transfer_upload, this.transfer.transfer_download);
                }
                IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteIpEndPoint = new IPEndPoint(ipaddress, 4001);
                UdpClient udpClient = new UdpClient();
                udpClient.Connect(remoteIpEndPoint);
                byte[] data = Encoding.ASCII.GetBytes(strdata);
                udpClient.BeginSend(data, data.Length, new AsyncCallback(UpdateTransferSendCallback), udpClient);
            }
            catch (Exception)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        public static void UpdateTransferSendCallback(IAsyncResult ar)
        {
            try
            {
                UdpClient u = (UdpClient)ar.AsyncState;
                u.Close();
            }
            catch (Exception)
            {
                try
                {
                    UdpClient u = (UdpClient)ar.AsyncState;
                    u.Close();
                }
                catch (Exception)
                {
                    //Console.WriteLine(e.ToString());
                }
            }
        }


        public void Start()
        {
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint severEndPoint = new IPEndPoint(0, config.server_port);
            // Bind the socket to the sever endpoint and listen for incoming connections.
            listener.Bind(severEndPoint);
            //half open count
            listener.Listen(10);
            // Start an asynchronous socket to listen for connections.
            Console.WriteLine("Waiting for a connection...");
            listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                listener);
        }

        public void Stop()
        {
            listener.Close();
        }

        //local connected
        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket conn = listener.EndAccept(ar);
                Handler handler = new Handler(this);
                handler.connection = conn;
                handler.encryptor = new Encryptor(config.method, config.password);
                handler.config = config;
                handler.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                try
                {
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                }
                catch (Exception)
                {
                    //stop by control
                    //Console.WriteLine(e.ToString());
                }
            }
        }
    }


    class Handler
    {
        private Server server;
        public Encryptor encryptor = null;
        public Config config;
        // Client  socket.
        public Socket remote;
        public Socket connection;
        // Size of receive buffer.
        public const int BufferSize = 4096;
        // remote receive buffer
        public byte[] remoteBuffer = new byte[BufferSize];
        // connection receive buffer
        public byte[] connetionBuffer = new byte[BufferSize];
        // connection Stage
        private int stage = 0;
        //remote addr
        private string destAddr = null;
        private bool closed = false;
        //Zombie Conn Clean
        System.Threading.Timer timeOutTimer = null;

        public Handler(Server server)
        {
            this.server = server;
        }

        public void Start()
        {
            try
            {
                int ivLent = encryptor.GetivLen();
                if (ivLent > 0)
                {
                    connection.BeginReceive(this.connetionBuffer, 0, ivLent, 0,
                        new AsyncCallback(handshakeReceiveCallback), null);
                }
                else
                {
                    stage = 1;
                    connection.BeginReceive(this.connetionBuffer, 0, 1, 0,
                        new AsyncCallback(handshakeReceiveCallback), null);
                }
                timeOutTimer = new System.Threading.Timer(new TimerCallback(ReceiveTimeOut), null, 10000, 100000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        public void ReceiveTimeOut(Object obj)
        {
            try
            {
                lock (timeOutTimer)
                {
                    this.timeOutTimer.Dispose();
                    this.Close();
                }
            }
            catch(Exception)
            {
                //already recycle
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (closed)
                {
                    return;
                }
                closed = true;
            }
            if (connection != null)
            {
                try
                {
                    Console.WriteLine("close:" + connection.RemoteEndPoint.ToString());
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Close();
                    connection = null;
                }
                catch (SocketException)
                {
                    //
                }
            }
            if (remote != null)
            {
                try
                {
                    remote.Shutdown(SocketShutdown.Both);
                    remote.Close();
                    remote = null;
                }
                catch (SocketException)
                {
                    //
                }
            }
            lock (this)
            {
                if (encryptor != null)
                {
                    encryptor.Dispose();
                    encryptor = null;
                }
            }
            config = null;
            remoteBuffer = null;
            connetionBuffer = null;
            destAddr = null;
            timeOutTimer = null;
            server = null;
        }

        //Single thread
        private void handshakeReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);
                if (bytesRead == 0)
                {
                    throw new Exception("Error When handshakeReceiveCallback");
                }
                //Console.WriteLine("bytesRead" + bytesRead.ToString() + " stage" + stage.ToString());
                if (stage == 0)
                {
                    //recv numbers of ivlen data
                    byte[] iv = encryptor.Decrypt(this.connetionBuffer, bytesRead);
                    //Decrypt sucessful
                    //iv
                    stage = 1;
                    connection.BeginReceive(this.connetionBuffer, 0, 1, 0,
                        new AsyncCallback(handshakeReceiveCallback), null);
                }
                else if (stage == 1)
                {
                    byte[] buff = encryptor.Decrypt(this.connetionBuffer, bytesRead);
                    //Decrypt sucessful
                    //addrtype
                    char addrtype = (char)buff[0];
                    if (addrtype == 1)
                    {
                        //type of ipv4
                        stage = 4;
                        connection.BeginReceive(this.connetionBuffer, 0, 4, 0,
                            new AsyncCallback(handshakeReceiveCallback), addrtype);

                    }
                    else if (addrtype == 3)
                    {
                        //type of url
                        stage = 3;
                        connection.BeginReceive(this.connetionBuffer, 0, 1, 0,
                            new AsyncCallback(handshakeReceiveCallback), addrtype);
                    }
                    else if (addrtype == 4)
                    {
                        //type of ipv6
                        stage = 4;
                        connection.BeginReceive(this.connetionBuffer, 0, 16, 0,
                            new AsyncCallback(handshakeReceiveCallback), addrtype);
                    }
                    else
                    {
                        throw new Exception("Error Socket5 AddrType");
                    }
                }
                else if (stage == 3)
                {
                    //addr len
                    byte[] buff = encryptor.Decrypt(this.connetionBuffer, bytesRead);
                    stage = 4;
                    //recv addr
                    connection.BeginReceive(this.connetionBuffer, 0, buff[0], 0,
                        new AsyncCallback(handshakeReceiveCallback), ar.AsyncState);
                }
                else if (stage == 4)
                {
                    //addr
                    byte[] buff = encryptor.Decrypt(this.connetionBuffer, bytesRead);
                    if (1 == (char)ar.AsyncState)
                    {
                        //ipv4
                        destAddr = string.Format("{0:D}.{1:D}.{2:D}.{3:D}", buff[0], buff[1], buff[2], buff[3]);
                    }
                    else if (3 == (char)ar.AsyncState)
                    {
                        //ipv6 url
                        destAddr = ASCIIEncoding.Default.GetString(buff);
                    }
                    else
                    {
                        //url
                        destAddr = string.Format("[{0:x2}{1:x2}:{2:x2}{3:x2}:{4:x2}{5:x2}:{6:x2}{7:x2}:{8:x2}{9:x2}:{10:x2}{11:x2}:{12:x2}{13:x2}:{14:x2}{15:x2}]"
                            , buff[0], buff[1], buff[2], buff[3], buff[4], buff[5], buff[6], buff[7], buff[8], buff[9], buff[10], buff[11], buff[12], buff[13], buff[14], buff[15]);
                    }
                    stage = 5;
                    //recv port
                    connection.BeginReceive(this.connetionBuffer, 0, 2, 0,
                        new AsyncCallback(handshakeReceiveCallback), null);
                }
                else if (stage == 5)
                {
                    //port
                    byte[] buff = encryptor.Decrypt(this.connetionBuffer, bytesRead);
                    int port = (int)(buff[0] << 8) + (int)buff[1];

                    stage = 6;
                    //handshake completed
                    lock (timeOutTimer)
                    {
                        this.timeOutTimer.Dispose();
                    }

                    //Begin to connect remote
                    IPAddress ipAddress;
                    bool parsed = IPAddress.TryParse(destAddr, out ipAddress);
                    if (!parsed)
                    {
                        IPAddress cache_ipAddress = DNSCache.GetInstence().Get(destAddr);
                        if (cache_ipAddress == null)
                        {
                            DNSCbContext ct = new DNSCbContext(destAddr, port);
                            Dns.BeginGetHostEntry(destAddr, new AsyncCallback(GetHostEntryCallback), ct);
                            return;
                        }
                        ipAddress = cache_ipAddress;
                    }
                    
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    remote = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                    remote.BeginConnect(remoteEP,
                        new AsyncCallback(remoteConnectCallback), null);
                }
            }
            catch (Exception e)
            {
                try
                {
                    //handshake time out
                    lock (timeOutTimer)
                    {
                        this.timeOutTimer.Dispose();
                    }
                    Console.WriteLine(e.ToString());
                    this.Close();
                }
                catch(Exception)
                {
                    //already recycle
                }
            }
        }

        public void GetHostEntryCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            DNSCbContext ct = (DNSCbContext)ar.AsyncState;
            try
            { 
                IPHostEntry ipHostInfo = Dns.EndGetHostEntry(ar);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                DNSCache.GetInstence().Put(ct.host, ipAddress);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, ct.port);

                remote = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                remote.BeginConnect(remoteEP,
                    new AsyncCallback(remoteConnectCallback), null);
            }
            catch (Exception)
            {
                try
                {
                    Console.WriteLine("Unknow Host " + ct.host);
                    this.Close();
                }
                catch (Exception)
                {
                    //already recycle
                }
            }
        }

        private void remoteConnectCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                // Complete the connection.
                remote.EndConnect(ar);

#if !DEBUG
                Console.WriteLine("Connected to {0}",
                    remote.RemoteEndPoint.ToString());
#endif

                MainForm.GetInstance().Log("Connected to " + remote.RemoteEndPoint.ToString());

                connection.BeginReceive(connetionBuffer, 0, BufferSize, 0,
                    new AsyncCallback(ConnectionReceiveCallback), null);
                remote.BeginReceive(remoteBuffer, 0, BufferSize, 0,
                    new AsyncCallback(RemoteReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        //Callback Runing at other thread
        private void ConnectionReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);
#if !DEBUG
                Console.WriteLine("bytesRead from client: " + bytesRead.ToString());
#endif
                if (bytesRead > 0)
                {
                    server.SetTransfer((UInt64)bytesRead, 0);
                    byte[] buf = encryptor.Decrypt(connetionBuffer, bytesRead);
                    remote.BeginSend(buf, 0, buf.Length, 0, new AsyncCallback(RemoteSendCallback), null);
                }
                else
                {
                    Console.WriteLine("client closed");
                    this.Close();
                }
            }
            catch (Exception e)
            {
                try
                {
                    Console.WriteLine(e.ToString());
                    this.Close();
                }
                catch (Exception)
                {
                    //already recycle
                }
            }
        }

        private void RemoteReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                int bytesRead = remote.EndReceive(ar);
#if !DEBUG
                Console.WriteLine("bytesRead from remote: " + bytesRead.ToString());
#endif
                if (bytesRead > 0)
                {
                    server.SetTransfer(0, (UInt64)bytesRead);
                    byte[] buf = encryptor.Encrypt(remoteBuffer, bytesRead);
                    connection.BeginSend(buf, 0, buf.Length, 0, new AsyncCallback(ConnectionSendCallback), null);
                }
                else
                {
                    //remote closed
                    Console.WriteLine("remote closed");
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        private void RemoteSendCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                int bytesSend = remote.EndSend(ar);
#if !DEBUG
                Console.WriteLine("bytesSend to remote: " + bytesSend.ToString());
#endif
                connection.BeginReceive(this.connetionBuffer, 0, BufferSize, 0,
                    new AsyncCallback(ConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        private void ConnectionSendCallback(IAsyncResult ar)
        {
            try
            {
                //already recycle
                lock (this)
                {
                    if (closed)
                        return;
                }
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                int bytesSend = connection.EndSend(ar);
#if !DEBUG
                Console.WriteLine("bytesSend to client: " + bytesSend.ToString());
#endif
                remote.BeginReceive(this.remoteBuffer, 0, BufferSize, 0,
                    new AsyncCallback(RemoteReceiveCallback), null);
            }
            catch (Exception e)
            {
                try
                {
                    Console.WriteLine(e.ToString());
                    this.Close();
                }
                catch(Exception)
                {
                    //already recycle
                }
            }
        }
    }
}
