using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.ComponentModel;
using System.Threading;

namespace Poker
{
    public class Client
    {
        public const uint BUFFER_SIZE = 1024;
        public static Dictionary<string, string> SendingProtocol = new Dictionary<string, string>();
        public static Dictionary<string, string> ReceivingProtocol = new Dictionary<string, string>();
        IPAddress HostIP;
        int HostPort;
        Socket serverSocket;
        public Game MyGame;
        BackgroundWorker listener;

        public Client(IPAddress hostIP, int hostPort)
        {
            HostIP = hostIP;
            HostPort = hostPort;
            serverSocket = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener = new BackgroundWorker();
            listener.DoWork += new DoWorkEventHandler(listener_DoWork);
            listener.RunWorkerCompleted += new RunWorkerCompletedEventHandler(listener_RunWorkCompleted);
            listener.WorkerSupportsCancellation = true;
        }

        

        public static void AddElemToSendingProtocol()
        {
            SendingProtocol.Add("disconnect", "-1");
            SendingProtocol.Add("connected", "00");
            SendingProtocol.Add("start", "01");
            SendingProtocol.Add("blind", "02");
            SendingProtocol.Add("action", "03");
            SendingProtocol.Add("chat", "04");
            SendingProtocol.Add("stack", "05");
            SendingProtocol.Add("end_of_msg", "$");
        }

        public static void AddElemToReceivingProtocol()
        {
            ReceivingProtocol.Add("disconnect", "-1");
            ReceivingProtocol.Add("hand", "00");
            ReceivingProtocol.Add("game", "01");
            ReceivingProtocol.Add("message", "02");
            ReceivingProtocol.Add("blind", "03");
            ReceivingProtocol.Add("showdown", "04");
            ReceivingProtocol.Add("end_of_msg", "$");
        }

        public bool PlayerConnect(string myName)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(HostIP, HostPort);
                serverSocket.Connect(remoteEP);
                SendToServer("connected", myName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void StartListening()
        {
            listener.RunWorkerAsync();
        }

        public void Disconnect()
        {
            listener.CancelAsync();
            Thread.Sleep(500);
            try
            {
                SendToServer("disconnect", "");
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();
            }
            catch { }            
        }

        private void listener_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Waiting for instruction from server...");
            string message = ReceiveFromServer();
            Console.WriteLine(">> RECEIVED: " + message);
            e.Result = message;
        }

        private void listener_RunWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string message = e.Result.ToString();
            ParseServerMessage(message);
            Drawing.UpdateUI();
            listener.RunWorkerAsync();
        }

        public void SendToServer(string protocol, string message)
        {
            string msgToSend = SendingProtocol[protocol];
            if (!string.IsNullOrEmpty(message))
            {
                msgToSend += " " + message;
            }
            msgToSend += SendingProtocol["end_of_msg"];
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msgToSend);
            try { serverSocket.Send(encodedMsg); }
            catch { }
        }

        public string ReceiveFromServer()
        {
            bool msgReceived = false;
            string message = "";
            while (!msgReceived)
            {
                byte[] msgBuffer = new byte[BUFFER_SIZE];
                int bytesReceived;
                try
                {
                    bytesReceived = serverSocket.Receive(msgBuffer);
                    message += Encoding.ASCII.GetString(msgBuffer, 0, bytesReceived);
                    if (message[^1].ToString() == ReceivingProtocol["end_of_msg"])
                    {
                        message = message[..^1];
                        break;
                    }
                }
                catch { continue; }
            }
            return message;
        }

        private void ParseServerMessage(string message)
        {
            string protocol = message[..2];
            string command = "";
            if (message.Length > 2)
                command = message[3..];
            if (protocol == ReceivingProtocol["disconnect"])
            {
                MessageBox.Show("You are disconnected from the server.");
                Disconnect();
            }
            else if (protocol == ReceivingProtocol["hand"])
            {
                MyGame.Players[MyGame.MySeat].Hand[0] = sbyte.Parse(command[..2]);
                MyGame.Players[MyGame.MySeat].Hand[1] = sbyte.Parse(command[2..]);
            }
            else if (protocol == ReceivingProtocol["game"])
            {
                string[] infos = command.Split(' ');
                foreach (string component in infos)
                {
                    switch (component[..2])
                    {
                        // Game on or off
                        case "ON":
                            if (component[3] == '0')
                                MyGame.On = false;
                            else
                                MyGame.On = true;
                            break;
                        // Blinds
                        case "BL":
                            string[] blinds = component[3..^1].Split(':');
                            MyGame.SmallBlind = uint.Parse(blinds[0]);
                            MyGame.BigBlind = uint.Parse(blinds[1]);
                            break;
                        // Players info
                        case "PL":
                            string[] players = component[3..^1].Split(',');
                            foreach (string p in players)
                            {
                                string[] playerInfos = p.Split(':');
                                byte seat = byte.Parse(playerInfos[0]);
                                Player player = MyGame.Players[seat];
                                string playerName = playerInfos[1];

                                if (playerName == "_")
                                {
                                    if (player.Enabled)
                                        player.DisablePlayer();
                                    continue;
                                }

                                if (!player.Enabled)
                                    player.EnablePlayer(playerName);

                                player.Name = playerName;
                                if (player.Name == MyGame.MyName)
                                    MyGame.MySeat = seat;
                                player.Stack = uint.Parse(playerInfos[2]);
                                uint playerBetting = uint.Parse(playerInfos[3]);
                                player.Betting = playerBetting;
                                if (playerInfos[4] == "0")
                                    player.InHand = false;
                                else
                                    player.InHand = true;
                            }
                            break;
                        // Highest & 2nd highest bet
                        case "BT":
                            string[] bet = component[3..^1].Split(':');
                            MyGame.HighestBet = uint.Parse(bet[0]);
                            MyGame.SecondHighestBet = uint.Parse(bet[1]);
                            break;
                        // Pots
                        case "PT":
                            MyGame.Pots = new uint[] { 0, 0, 0, 0, 0 };
                            if (component[3] != '0')
                            {
                                string[] pots = component[3..^1].Split(':');
                                for (int i = 0; i < pots.Length; i++)
                                    MyGame.Pots[i] = uint.Parse(pots[i]);
                            }
                            break;
                        // Dealer position
                        case "DL":
                            MyGame.Dealer = byte.Parse(component[3].ToString());
                            break;
                        // Acting position
                        case "AC":
                            MyGame.Acting = byte.Parse(component[3].ToString());
                            break;
                        // Community cards
                        case "CM":
                            MyGame.Community = new sbyte[] { -1, -1, -1, -1, -1 };
                            if (component.Length > 4)
                            {
                                string[] cards = component[3..^1].Split(':');
                                for (int i = 0; i < cards.Length; i++)
                                    MyGame.Community[i] = sbyte.Parse(cards[i]);
                            }
                            break;
                    }
                }
            }
            else if (protocol == ReceivingProtocol["message"])
            {
                MessageBox.Show(command);
            }
            else if (protocol == ReceivingProtocol["blind"])
            {

            }
            else if (protocol == ReceivingProtocol["showdown"])
            {
                string[] players = command.Split(' ');
                foreach (string p in players)
                {
                    int seat = int.Parse(p[0].ToString());
                    Player player = MyGame.Players[seat];
                    player.Hand[0] = sbyte.Parse(p[2..4]);
                    player.Hand[1] = sbyte.Parse(p[4..6]);
                    player.DrawHand();
                }
            }
        }
    }
}