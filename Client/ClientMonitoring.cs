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

namespace Client
{
    public partial class ClientMonitoring : Form
    {
        private static PerfMonitor perfMonitor = new PerfMonitor();
        private static FileTransfer fileTransfer = new FileTransfer(TransferMode.Send);
        private static ConnSend connSend = new ConnSend(8000, true, ConnectionMode.Listening);
        private static Thread thread = new Thread(new ThreadStart(Service));

        public ClientMonitoring()
        {
            InitializeComponent();
            connSend.StartListening();
            thread.Start();
        }

        private static void Service()
        {
            while (true)
            {
                Thread.Sleep(50);
                if (connSend.Communique == "MonitorsList")
                {
                    perfMonitor.MonitorsList();
                    while (true)
                    {
                        Thread.Sleep(50);
                        if (perfMonitor.EndCreateFile == true)
                            break;
                    }
                    connSend.Send(Messages.OK);
                    connSend.Communique = "";
                }
                if (connSend.Communique == "Upload")
                {
                    fileTransfer.Send(connSend.IPClient, "8001", "MonitorsList.xml");
                    connSend.Communique = "";
                }
            }
        }

        private void ClientMonitoring_FormClosed(object sender, FormClosedEventArgs e)
        {
            thread.Abort();
        }



    }
}
