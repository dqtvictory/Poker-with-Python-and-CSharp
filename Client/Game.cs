using System;
using System.Collections.Generic;
using System.Text;

namespace Poker
{
    public class Game
    {
        public const byte MAX_PLAYERS = 6;
        public Player[] Players { get; }
        public string MyName { get; }
        public byte MySeat;

        public bool On;
        public uint SmallBlind;
        public uint BigBlind;
        public sbyte[] Community;
        public uint[] Pots;
        public byte Dealer;
        public uint HighestBet;
        public uint SecondHighestBet;
        public byte Acting;        

        public Game(string myName)
        {
            MyName = myName;
            Players = new Player[MAX_PLAYERS];
        }

        public void AddPlayer(Player player, byte seat)
        {
            Players[seat] = player;
        }
    }
}
