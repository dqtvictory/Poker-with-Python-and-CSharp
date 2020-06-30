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
            // Prep works
            HostIP = hostIP;
            HostPort = hostPort;
            AddElemToSendingProtocol();
            AddElemToReceivingProtocol();

            // Initialize the Client socket object, then also initialize a background worker listening to events from the server
            serverSocket = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener = new BackgroundWorker();
            listener.DoWork += new DoWorkEventHandler(listener_DoWork);
            listener.RunWorkerCompleted += new RunWorkerCompletedEventHandler(listener_RunWorkCompleted);
            listener.WorkerSupportsCancellation = true;
        }

        private static void AddElemToSendingProtocol()
        {
            // Adding pairs of key and value to SendingProtocol dictionary
            SendingProtocol.Add("disconnect", "-1");
            SendingProtocol.Add("connected", "00");
            SendingProtocol.Add("start", "01");
            SendingProtocol.Add("blind", "02");
            SendingProtocol.Add("action", "03");
            SendingProtocol.Add("chat", "04");
            SendingProtocol.Add("stack", "05");
            SendingProtocol.Add("request_state", "06");
            SendingProtocol.Add("end_of_msg", "$");
        }

        private static void AddElemToReceivingProtocol()
        {
            // Adding pairs of key and value to ReceivingProtocol dictionary
            ReceivingProtocol.Add("disconnect", "-1");
            ReceivingProtocol.Add("hand", "00");
            ReceivingProtocol.Add("game", "01");
            ReceivingProtocol.Add("message", "02");
            ReceivingProtocol.Add("name", "03");
            ReceivingProtocol.Add("showdown", "04");
            ReceivingProtocol.Add("announcement", "05");
            ReceivingProtocol.Add("end_of_msg", "$");
        }

        public bool PlayerConnect(string myName)
        {
            // Attemp to connect to the server for the first time to establish a network stream
            // Returns true if connection succeeds, false otherwise
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
            // Start the listener's worker thread
            listener.RunWorkerAsync();
        }

        public void Disconnect()
        {
            // Properly close the socket and end the worker's thread when disconnect
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
            // The worker's thread's DoWork function. It receives a message from the server, then pass the result to its buffer property
            string message = ReceiveFromServer();
            e.Result = message;
        }

        private void listener_RunWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The worker's thread's "after work" function. It grabs the result from the buffer then pass it on to the parser
            // Then it requests the Drawing static class to update the UI after the game's state has been updated, then start again the worker
            string message = e.Result.ToString();
            bool shouldUpdateUI = ParseServerMessage(message);
            if (shouldUpdateUI)
                Drawing.UpdateUI();
            listener.RunWorkerAsync();
        }

        public void SendToServer(string protocol, string message)
        {
            // Function to properly format and send a message to the server, so that the receipent understands correctly the message
            // This can be understood as the "translator" and "poster"
            string msgToSend = SendingProtocol[protocol];
            if (!string.IsNullOrEmpty(message))
            {
                msgToSend += " " + message;
            }
            msgToSend += SendingProtocol["end_of_msg"];
            byte[] encodedMsg = Encoding.UTF8.GetBytes(msgToSend);
            try { serverSocket.Send(encodedMsg); }
            catch { }
        }

        private string ReceiveFromServer()
        {
            // Function to fully receive and extract a message from the server
            // Returns the complete message, without any protocol character (end_of_msg escape char)
            bool msgReceived = false;
            string message = "";
            while (!msgReceived)
            {
                byte[] msgBuffer = new byte[BUFFER_SIZE];
                int bytesReceived;
                try
                {
                    bytesReceived = serverSocket.Receive(msgBuffer);
                    message += Encoding.UTF8.GetString(msgBuffer, 0, bytesReceived);
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

        private bool ParseServerMessage(string message)
        {
            // The message parser. It analyzes the message passed into argument (generally a message from the server) and reacts accordingly
            // Returns true if UI should be updated, false otherwise

            // Protocols are 2 chars long, usually an int
            string protocol = message[..2];

            // The protocol is followed by the actual message that I call "command"
            string command = "";
            if (message.Length > 2)
                command = message[3..];
            if (protocol == ReceivingProtocol["disconnect"])
            {
                // Order from the server to disconnect the client
                Disconnect();
                MessageBox.Show("You are disconnected from the server.");
            }
            else if (protocol == ReceivingProtocol["hand"])
            {
                // When the player's hand is received, parse the hand
                MyGame.Players[MyGame.MySeat].Hand[0] = sbyte.Parse(command[..2]);
                MyGame.Players[MyGame.MySeat].Hand[1] = sbyte.Parse(command[2..]);
            }
            else if (protocol == ReceivingProtocol["game"])
            {
                // The game state
                string[] infos = command.Split(' ');
                foreach (string component in infos)
                {
                    switch (component[..2])
                    {
                        // Game on or off
                        case "ON":
                            if (component[3] == '0')
                            {
                                MyGame.On = false;
                                // Reset player's hand when game is off
                                MyGame.Players[MyGame.MySeat].Hand = new sbyte[] { -1, -1 };
                            }
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
                                    // Skip updating player's info if he doesn't exist
                                    if (player.Enabled)
                                        player.DisablePlayer();
                                    continue;
                                }

                                if (!player.Enabled)
                                    player.EnablePlayer(playerName);

                                // Update player's name
                                player.Name = playerName;

                                // If a player's name is actually the user's name, the user's seat is updated
                                if (player.Name == MyGame.MyName)
                                    MyGame.MySeat = seat;

                                // Update player's stack
                                player.Stack = uint.Parse(playerInfos[2]);

                                // Update player's current betting amount
                                player.Betting = uint.Parse(playerInfos[3]);

                                // Update player's in-hand status (if he is still participating in a hand)
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
                        // Pots (main & side pots)
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
                        case "NR":
                            // No-raising status, indicating if the acting player can raise higher (in case everyone else in-hand has gone all-in, he cannot raise higher because no one can call afterward)
                            if (component[3] == '1')
                                MyGame.NoRaising = true;
                            else
                                MyGame.NoRaising = false;
                            break;
                    }
                }
            }
            else if (protocol == ReceivingProtocol["message"])
            {
                // A message intended to be displayed in the chat
                if (MyGame.MyChat.Count == 0)
                    MyGame.MyChat.Add(command);
                else
                    MyGame.MyChat.Insert(0, command);
            }
            else if (protocol == ReceivingProtocol["name"])
            {
                // Change the user's name, ordered by the Server in case user chooses a name that already has existed
                MyGame.MyName = command;
                return false;
            }
            else if (protocol == ReceivingProtocol["showdown"])
            {
                // Hand showdown, either for an all-in, or after the river round
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
            else if (protocol == ReceivingProtocol["announcement"])
            {
                // Announce the winner(s). The functionality is about the same as the "message" protocol, but in this case the UI should not update itself right away because players need to see others' hands before moving on
                if (MyGame.MyChat.Count == 0)
                    MyGame.MyChat.Add(command);
                else
                    MyGame.MyChat.Insert(0, command);
                MyGame.WinnerAnnounced = true;
            }

            return true;
        }
    }
}