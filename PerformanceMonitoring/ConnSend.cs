using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PerformanceMonitoring
{
    public enum ConnectionMode
    {
        Listening, Connecting
    };

    /*
     * Pomysł na klasę zaczerpnięty ze strony http://www.centrumxp.pl/dotNet/831,Komunikator-w-C.aspx
     * Wprowadzono parę modyfikacji dla większej niezawodności
     * Stworzono klasę elastyczną, możliwą do wykorzystania również w innych projektach
    */

    /// <summary>
    ///  Klasa wspierająca komunikację pomiędzy dwoma komputerami.
    /// </summary>
    public class ConnSend
    {
        private int port;
        private BackgroundWorker BWListening = new BackgroundWorker();
        private BackgroundWorker BWReceiving = new BackgroundWorker();
        private BackgroundWorker BWConnecting = new BackgroundWorker();
        private bool autoResume;
        private TcpListener Listener;
        private TcpClient Client;
        private bool _connected;
        private ConnectionMode _connectionMode;
        private string _communique;
        private NetworkStream netStream;
        private BinaryWriter writer;
        private BinaryReader reader;



        /// <summary>
        ///  Możliwość wybrania portu oraz w jakim trybie klasa ma pracować.
        /// </summary>
        ///  <param name="port">Odpowiada za podanie portu z którego chcemy korzystać podczas komuniakcji</param>
        ///  <param name="connectionMode">Tryb w którym uruchomić klasę. Listening podpowiada za nasłuchiwanie, Connection za tryb połączenia.</param>
        public ConnSend(int port, ConnectionMode connectionMode)
        {
            this.port = port;
            autoResume = false;
            _connected = false;
            _connectionMode = connectionMode;

            BWListening.WorkerReportsProgress = true;
            BWListening.WorkerSupportsCancellation = true;
            BWListening.DoWork += BWListening_DoWork;
            BWListening.RunWorkerCompleted += BWListening_RunWorkerCompleted;

            BWReceiving.WorkerReportsProgress = true;
            BWReceiving.WorkerSupportsCancellation = true;
            BWReceiving.DoWork += BWReceiving_DoWork;
            BWReceiving.RunWorkerCompleted += BWReceiving_RunWorkerCompleted;

            BWConnecting.WorkerReportsProgress = true;
            BWConnecting.WorkerSupportsCancellation = true;
            BWConnecting.DoWork += BWConnecting_DoWork;
        }



        public ConnSend(int port, bool autoResume, ConnectionMode connectionMode)
        {
            this.port = port;
            this.autoResume = autoResume;
            _connected = false;
            _connectionMode = connectionMode;

            BWListening.WorkerReportsProgress = true;
            BWListening.WorkerSupportsCancellation = true;
            BWListening.DoWork += BWListening_DoWork;
            BWListening.RunWorkerCompleted += BWListening_RunWorkerCompleted;

            BWReceiving.WorkerReportsProgress = true;
            BWReceiving.WorkerSupportsCancellation = true;
            BWReceiving.DoWork += BWReceiving_DoWork;
            BWReceiving.RunWorkerCompleted += BWReceiving_RunWorkerCompleted;

            BWConnecting.WorkerReportsProgress = true;
            BWConnecting.WorkerSupportsCancellation = true;
            BWConnecting.DoWork += BWConnecting_DoWork;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public string Communique
        {
            get
            {
                return _communique;
            }
        }

        public void StartListening()
        {
            if (_connectionMode == ConnectionMode.Listening)
                BWListening.RunWorkerAsync();
            else
                MessageBox.Show("Aby uruchomić klasa musi zostać utworzona w trybie Listening");
        }

        public void StopListening()
        {
            if (_connectionMode == ConnectionMode.Listening)
            {
                BWListening.CancelAsync();
                if (Client != null)
                    Client.Close();
            }
            else
                MessageBox.Show("Aby uruchomić klasa musi zostać utworzona w trybie Listening");
        }

        public void StartConnecting(string ipAddress)
        {
            if (_connectionMode == ConnectionMode.Connecting)
            {
                BWConnecting.RunWorkerAsync(ipAddress);
            }
            else
                MessageBox.Show("Aby uruchomić klasa musi zostać utworzona w trybie Connecting");
        }

        public void StopConnecting()
        {
            if (_connectionMode == ConnectionMode.Connecting)
            {
                Send(Messages.Disconnect);
            }
        }

        public void StopReceiving()
        {
            BWReceiving.CancelAsync();
        }

        private void BWListening_DoWork(object sender, DoWorkEventArgs e)
        {
            Listener = new TcpListener(port);
            Listener.Start();

            while (!Listener.Pending())
            {
                Thread.Sleep(1000); // Zatrzymujemy pętlę na 1 sekundę aby nie zrzerać pamięci (nieskończona pętla)
                if (BWListening.CancellationPending)
                {
                    if (Client != null) // Jeżeli Client nie będzie jeszcze przypisany to może spowodować błąd
                        Client.Close(); // Zabezpieczenie, gdyby połączenie z klientem nadal występowało to ma rozłączyć
                }
            }

            Client = Listener.AcceptTcpClient(); // Przypisanie klienta który połączył się z naszym listenerem
            netStream = Client.GetStream(); // Stworzenie połączenia przez który wysyłamy i odbieramy dane
            writer = new BinaryWriter(netStream); // Obiekt do wysyłania danych
            reader = new BinaryReader(netStream); // Obiekt do odbierania danych

            if (reader.ReadString() == Messages.Connect) // Sprawdzamy czy otrzymaliśmy komunikat o chęci połączenia się
            {
                Send(Messages.OK); // Wysyłamy komunikat że zgadzamy się na połączenie
                _connected = true;
                BWReceiving.RunWorkerAsync();
            }
            else // występuje gdy nie został do nas wysłany komunikat
            {
                if (Client != null)
                    Client.Close();
                Listener.Stop();
                _connected = false;
            }
        }

        private void BWListening_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_connected == false && autoResume == true)
                BWListening.RunWorkerAsync();
        }

        private void BWReceiving_DoWork(object sender, DoWorkEventArgs e)
        {
            string communique;
            while((communique = reader.ReadString()) != Messages.Disconnect || !BWReceiving.CancellationPending)
                _communique = communique;
            Client.Close();
            Listener.Stop();
        }

        private void BWReceiving_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (autoResume == true)
            {
                BWListening.RunWorkerAsync();
            }
        }

        private void BWConnecting_DoWork(object sender, DoWorkEventArgs e)
        {
            Client = new TcpClient();
            Client.Connect(IPAddress.Parse((string)e.Argument), port);
            netStream = Client.GetStream();
            writer = new BinaryWriter(netStream);
            reader = new BinaryReader(netStream);
            Send(Messages.Connect);

            if (reader.ReadString() == Messages.OK)
            {
                _connected = true;
                BWReceiving.RunWorkerAsync();
            }
            else
            {
                _connected = false;
                if (Client != null)
                    Client.Close();
            }
        }

        public void Send(string communique)
        {
            if (writer != null)
                writer.Write(communique);
        }
    }
}
