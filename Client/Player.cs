using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;
using Combinatorics.Collections;

namespace Poker
{
    public class Player
    {
        public TextBlock NameBlock { get; }
        public TextBlock CashBlock { get; }
        public TextBlock BettingBlock { get; }
        public Ellipse DealerChip { get; }
        public Ellipse Avatar { get; }
        public StackPanel Card0 { get; }
        public StackPanel Card1 { get; }
        public string Name;
        public uint Stack;
        public uint Betting;
        public bool InHand;
        public sbyte[] Hand;
        public bool Enabled;

        public Player(
            TextBlock nameBlock,
            TextBlock cashBlock,
            TextBlock bettingBlock,
            Ellipse dealerChip,
            Ellipse avatar,
            StackPanel card0,
            StackPanel card1
            )
        {
            NameBlock = nameBlock;
            CashBlock = cashBlock;
            BettingBlock = bettingBlock;
            DealerChip = dealerChip;
            Avatar = avatar;
            Card0 = card0;
            Card1 = card1;

            DisablePlayer();
        }

        private void InitPlayerObj()
        {
            Name = string.Empty;
            Stack = Betting = 0;
            InHand = false;
            Hand = new sbyte[] { -1, -1 };
            Enabled = false;
        }

        private void HideUI()
        {
            NameBlock.Visibility = Visibility.Hidden;
            CashBlock.Visibility = Visibility.Hidden;
            BettingBlock.Visibility = Visibility.Hidden;
            Avatar.Visibility = Visibility.Hidden;
            Card0.Visibility = Visibility.Hidden;
            Card1.Visibility = Visibility.Hidden;
            DealerChip.Visibility = Visibility.Hidden;
        }

        private void ShowUI()
        {
            NameBlock.Visibility = Visibility.Visible;
            CashBlock.Visibility = Visibility.Visible;
            BettingBlock.Visibility = Visibility.Visible;
            Avatar.Visibility = Visibility.Visible;
        }

        public override string ToString()
        {
            return Name;
        }

        public void EnablePlayer(string name)
        {
            Name = name;
            Enabled = true;
            ShowUI();
        }

        public void DisablePlayer()
        {
            InitPlayerObj();
            HideUI();
        }

        public void DrawHand()
        {
            Drawing.DrawCard(Hand[0], Card0);
            Drawing.DrawCard(Hand[1], Card1);
        }
        public void UndrawHand()
        {
            Drawing.UndrawCard(Card0);
            Drawing.UndrawCard(Card1);
        }
    }
}
