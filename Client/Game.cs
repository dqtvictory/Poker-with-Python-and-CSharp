using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Poker
{
    public class Game
    {
        public const byte MAX_PLAYERS = 6;
        public const string GM_NAME = "TrungDam";
        public Player[] Players { get; }
        public string MyName;
        public byte MySeat;
        public ObservableCollection<string> MyChat = new ObservableCollection<string>();

        public bool On;
        public uint SmallBlind;
        public uint BigBlind;
        public sbyte[] Community;
        public uint[] Pots;
        public byte Dealer;
        public uint HighestBet;
        public uint SecondHighestBet;
        public byte Acting;
        public bool NoRaising;
        public bool WinnerAnnounced;

        public Game(string myName)
        {
            MyName = myName;
            Players = new Player[MAX_PLAYERS];
        }

        public void AddPlayer(Player player, byte seat)
        {
            // Assign a player's object to a seat
            Players[seat] = player;
        }
    }
}
