using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PerformanceMonitoring
{
    public enum TransferMode
    {
        Send, Receive
    };

    public class FileTransfer
    {
        private const int bufferSize = 1024;
        private TransferMode tm;
        private BackgroundWorker BWSendFile = new BackgroundWorker();
        private BackgroundWorker BWReceiveFile = new BackgroundWorker();
        private int totalRecBytes;
        private bool downloadFile = false;

        public FileTransfer(TransferMode transferMode)
        {
            tm = transferMode;

            if (tm == TransferMode.Send)
            {
                BWSendFile.WorkerReportsProgress = true;
                BWSendFile.WorkerSupportsCancellation = true;
                BWSendFile.DoWork += BWSendFile_DoWork;
            }
            else
            {
                BWReceiveFile.WorkerReportsProgress = true;
                BWReceiveFile.WorkerSupportsCancellation = true;
                BWReceiveFile.DoWork += BWReceiveFile_DoWork;
            }
        }

        public int TotalRecBytes
        {
            get
            {
                return totalRecBytes;
            }
        }

        public bool DownloadFile
        {
            get
            {
                return downloadFile;
            }
        }

        public void Send(string IP, string port, string file)
        {
            string [] param = { IP, port, file };
            if (tm == TransferMode.Send)
                BWSendFile.RunWorkerAsync(param);
        }

        public void Receive(string port, string file)
        {
            string[] param = { port, file };
            if (tm == TransferMode.Receive)
                BWReceiveFile.RunWorkerAsync(param);
        }

        private void BWReceiveFile_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener Listener = null;
            string[] param = (string[])e.Argument;
            downloadFile = false;

            try
            {
                Listener = new TcpListener(IPAddress.Any, Convert.ToInt32(param[0]));
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            byte[] recData = new byte[bufferSize];
            int recBytes;

            while(true)
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                try
                {
                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();
                        int totalrecbytes = 0;
                        FileStream file = new FileStream(param[1], FileMode.OpenOrCreate, FileAccess.Write);
                        while ((recBytes = netstream.Read(recData, 0, recData.Length)) > 0)
                        {
                            file.Write(recData, 0, recBytes);
                            totalrecbytes += recBytes;
                            totalRecBytes = totalrecbytes;
                        }
                        file.Close();
                        netstream.Close();
                        client.Close();
                        downloadFile = true;
                        break;
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void BWSendFile_DoWork(object sender, DoWorkEventArgs e)
        {

            byte[] sendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            string[] param = (string[])e.Argument;
            try
            {
                client = new TcpClient(param[0], Convert.ToInt32(param[1]));
                netstream = client.GetStream();
                FileStream file = new FileStream(param[2], FileMode.Open, FileAccess.Read);
                int packets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(file.Length) / Convert.ToDouble(bufferSize)));

                int totalLength = (int)file.Length;
                int currentPacketLength = 0;

                for (int i = 0; i < packets; i++)
                {
                    if (totalLength > bufferSize)
                    {
                        currentPacketLength = bufferSize;
                        totalLength = totalLength - currentPacketLength;
                    }
                    else
                        currentPacketLength = totalLength;

                    sendingBuffer = new byte[currentPacketLength];
                    file.Read(sendingBuffer, 0, currentPacketLength);
                    netstream.Write(sendingBuffer, 0, (int)sendingBuffer.Length);
                }
                file.Close();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if(netstream != null)
                    netstream.Close();
                if(client != null)
                    client.Close();
            }
        }


    }
}
