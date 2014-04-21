using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PerformanceMonitoring;
using System.Threading;
using System.IO;

namespace Server
{
    public partial class ServerMonitoring : Form
    {
        private ConnSend connSend = new ConnSend(8000, ConnectionMode.Connecting);
        private FileTransfer fileTransfer = new FileTransfer(TransferMode.Receive);
        private PerfMonitor perfMonitor = new PerfMonitor();

        public ServerMonitoring()
        {
            InitializeComponent();
        }

        private void DownloadCountersList_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                File.Delete("MonitorsList.xml");
                lock (this)
                {
                    connSend.StartConnecting(textBox1.Text);
                    while (true)
                    {
                        Thread.Sleep(50);
                        if (connSend.Connected == true)
                        {
                            connSend.Send("MonitorsList");
                            break;
                        }
                    }
                    while (true)
                    {
                        Thread.Sleep(50);
                        if (connSend.Communique == Messages.OK)
                        {
                            fileTransfer.Receive("8001", "MonitorsList.xml");
                            Thread.Sleep(100);
                            connSend.Send("Upload");
                            break;
                        }
                    }
                    while (true)
                    {
                        Thread.Sleep(50);
                        if (fileTransfer.DownloadFile == true)
                        {
                            MessageBox.Show("Lista została pobrana.");
                            comboBox1.Items.AddRange(perfMonitor.ReadAllCategory("MonitorsList.xml"));
                            break;
                        }
                    }
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            comboBox3.Items.Clear();
            comboBox2.Text = "";
            comboBox3.Text = "";
            comboBox2.Items.AddRange(perfMonitor.ReadAllInstance("MonitorsList.xml", (string)comboBox1.SelectedItem));
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3.Items.Clear();
            comboBox3.Text = "";
            comboBox3.Items.AddRange(perfMonitor.ReadAllProperty("MonitorsList.xml",(string)comboBox1.SelectedItem,(string)comboBox2.SelectedItem));
        }

    }
}
