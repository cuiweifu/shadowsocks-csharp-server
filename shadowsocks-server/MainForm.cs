using shadowsocks_csharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace shadowsocks
{
    public partial class MainForm : Form
    {
        [System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize", ExactSpelling = true, CharSet =
            System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        private static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        Config config;
        Server server;
        ControlServer controlserver;
        Thread thReleaseMem = null;

        public static MainForm instanse = null;

        public MainForm()
        {
            instanse = this;
            InitializeComponent();
        }

        public static MainForm GetInstance()
        {
            return instanse;
        }

        public delegate void FlushLog(string str); 
        public void Log(string str)
        {
            if (this.label10.InvokeRequired)
            {
                FlushLog fc = new FlushLog(Log); 
                this.Invoke(fc, new object[1] { str});
            }
            else
            {
                this.label10.Text = str;
            }
        }

        private void SetConfigLab(Config config)
        {
            label5.Text = config.server;
            label6.Text = config.server_port.ToString();
            label7.Text = config.method;
            label8.Text = config.password;
            label12.Text = config.multiuser_pylisten.ToString();
            if(config.multiuser_pylisten)
            {
                label6.Text = "start by python";
                label8.Text = "start by python";
            }
        }
        public static void ReleaseMemory()
        {
            while (true)
            {
                GC.Collect(GC.MaxGeneration);
                GC.WaitForPendingFinalizers();
                //SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                Thread.Sleep(60 * 1000);
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            try
            {
                Config config = Config.Load();
                Config.Save(config);
                this.config = config;
                SetConfigLab(config);
                try
                {
                    Encryptor encryptor = new Encryptor(config.method, config.password);
                    encryptor.Dispose();
                }
                catch(Exception)
                {
                    MessageBox.Show("Open SSL library init failed!");
                    return;
                }

                if (config.multiuser_pylisten == true)
                {
                    this.controlserver = new ControlServer(config);
                    controlserver.Start();
                }
                else
                {
                    server = new Server(config);
                    server.Start();
                }
                thReleaseMem = new Thread(new ThreadStart(ReleaseMemory));
                thReleaseMem.Start();
                thReleaseMem.IsBackground = true;

                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void UpDateStatusList()
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(3500, "I'm here", "wwwwwww", ToolTipIcon.Info);
                this.ShowInTaskbar = false;
            }
        }
        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            this.Show();
        }
    }
}
