using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace Poker
{
    public static class Drawing
    {
        public static Game MyGame;
        public static MainWindow MyWindow;

        public static void DrawCard(int cardIndex, StackPanel cardPanel)
        {
            if (cardIndex == -1)
            {
                ImageBrush brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri(@"assets/card bg.jpg", UriKind.Relative));
                cardPanel.Background = brush;
            }
            else
            {
                string value = Card.ReadValue(cardIndex);
                string suit = Card.ReadSuit(cardIndex);
                TextBlock cardSuit = new TextBlock()
                {
                    Text = suit,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 25,
                    Height = 24.5,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = 24,
                    TextAlignment = TextAlignment.Center,
                };
                TextBlock cardValue = new TextBlock()
                {
                    Text = value,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 45.5,
                    FontSize = 32,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Georgia"),
                };
                if (suit == "♥" || suit == "♦")
                {
                    cardSuit.Foreground = Brushes.Red;
                    cardValue.Foreground = Brushes.Red;
                }
                cardPanel.Children.Add(cardSuit);
                cardPanel.Children.Add(cardValue);
                cardPanel.Background = Brushes.White;
            }
            cardPanel.Visibility = Visibility.Visible;
        }

        public static void UndrawCard(StackPanel cardPanel)
        {
            cardPanel.Children.Clear();
            cardPanel.Visibility = Visibility.Hidden;
        }

        public static void ClearUI()
        {
            foreach (Player player in MyGame.Players)
            {

                UndrawCard(player.Card0);
                UndrawCard(player.Card1);
            }
            for (int i = 0; i < 5; i++)
            {
                UndrawCard(MyWindow.CommunityPanels[i]);
            }
        }

        public static void UpdateUI()
        {
            if (MyGame.WinnerAnnounced)
            {
                // If winner has just been announced, don't update the UI to keep displaying the hand showdown
                MyWindow.startButton.Content = "Update Game's state";
                MyWindow.startButton.IsEnabled = true;
                MyWindow.actionPanel.Visibility = Visibility.Hidden;
                MyGame.WinnerAnnounced = false;
                return;
            }
            // If player's name is a GM's name, show the GM's control panel
            if (MyGame.MyName == Game.GM_NAME)
                MyWindow.gmPanel.Visibility = Visibility.Visible;

            // Update each player's labels
            for (int i = 0; i < MyGame.Players.Length; i++)
            {
                Player player = MyGame.Players[i];

                // Show dealer's button
                if (i == MyGame.Dealer)
                    player.DealerChip.Visibility = Visibility.Visible;
                else
                    player.DealerChip.Visibility = Visibility.Hidden;

                if (!player.Enabled)
                    continue;
                player.NameBlock.Text = player.Name;
                player.CashBlock.Text = player.Stack.ToString("N0") + " chips";
                player.BettingBlock.Text = player.Betting.ToString("N0");

                // Show / Hide betting label
                if (player.Betting == 0)
                    player.BettingBlock.Visibility = Visibility.Hidden;
                else
                    player.BettingBlock.Visibility = Visibility.Visible;

                // Mark current acting player
                player.Avatar.Fill = Brushes.White;
                if (MyGame.On && i == MyGame.Acting)
                {
                    ImageBrush brush = new ImageBrush();
                    brush.ImageSource = new BitmapImage(new Uri(@"assets/X.jpg", UriKind.Relative));
                    player.Avatar.Fill = brush;
                }

                // Draw player's cards in hand
                if (i == MyGame.MySeat && !Enumerable.SequenceEqual(player.Hand, new sbyte[] { -1, -1 }))
                {
                    player.DrawHand();
                    if (player.InHand)
                    {
                        player.Card0.Opacity = 1.0D;
                        player.Card1.Opacity = 1.0D;
                    }
                    else
                    {
                        player.Card0.Opacity = 0.5D;
                        player.Card1.Opacity = 0.5D;
                    }
                }
                else if (i != MyGame.MySeat)
                {
                    if (!MyGame.On)
                        player.Hand = new sbyte[] { -1, -1 };
                    if (player.InHand)
                        player.DrawHand();
                    else
                        player.UndrawHand();
                }
                else
                    player.UndrawHand();
            }

            // Update each pot's label
            for (int i = 0; i < MyGame.Pots.Length; i++)
            {
                if (MyGame.Pots[i] > 0 && MyWindow.PotBlocks[i].Visibility == Visibility.Hidden)
                    MyWindow.PotBlocks[i].Visibility = Visibility.Visible;
                else if (MyGame.Pots[i] == 0 && MyWindow.PotBlocks[i].Visibility == Visibility.Visible)
                    MyWindow.PotBlocks[i].Visibility = Visibility.Hidden;
                if (i == 0)
                    MyWindow.PotBlocks[i].Text = "Pot: " + MyGame.Pots[i].ToString();
                else
                    MyWindow.PotBlocks[i].Text = $"SP{i}: " + MyGame.Pots[i].ToString();
            }

            // Show community cards
            for (int i = 0; i < 5; i++)
            {
                int card = MyGame.Community[i];
                if (card == -1)
                {
                    UndrawCard(MyWindow.CommunityPanels[i]);
                    continue;
                }
                DrawCard(card, MyWindow.CommunityPanels[i]);
            }

            // Show action panel if it's turn
            MyWindow.actionPanel.Visibility = Visibility.Hidden;
            if (MyGame.Acting == MyGame.MySeat && MyGame.On)
            {
                Player self = MyGame.Players[MyGame.MySeat];
                uint minimumBet;
                uint maximumSpending = self.Betting + self.Stack;
                if (MyGame.HighestBet == 0)
                    minimumBet = MyGame.BigBlind;
                else
                    minimumBet = 2 * MyGame.HighestBet - MyGame.SecondHighestBet;
                if (self.Betting == MyGame.HighestBet)
                {
                    MyWindow.checkCallButton.Content = "Check";
                    if (maximumSpending > minimumBet)
                    {
                        MyWindow.betButton.Content = "Bet";
                        MyWindow.betButton.Visibility = Visibility.Visible;
                        MyWindow.betAmount.Text = minimumBet.ToString();
                        MyWindow.betAmount.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MyWindow.betButton.Content = "All in";
                        MyWindow.betButton.Visibility = Visibility.Visible;
                        MyWindow.betAmount.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    MyWindow.checkCallButton.Content = "Call";
                    if (maximumSpending <= MyGame.HighestBet || MyGame.NoRaising)
                    {
                        MyWindow.betButton.Visibility = Visibility.Hidden;
                        MyWindow.betAmount.Visibility = Visibility.Hidden;
                    }
                    else if (maximumSpending > minimumBet)
                    {
                        MyWindow.betButton.Content = "Bet";
                        MyWindow.betButton.Visibility = Visibility.Visible;
                        MyWindow.betAmount.Text = minimumBet.ToString();
                        MyWindow.betAmount.Visibility = Visibility.Visible;
                    }
                    else if (maximumSpending <= minimumBet)
                    {
                        MyWindow.betButton.Content = "All in";
                        MyWindow.betButton.Visibility = Visibility.Visible;
                        MyWindow.betAmount.Visibility = Visibility.Hidden;
                    }
                }
                MyWindow.actionPanel.Visibility = Visibility.Visible;
            }

            // If game is currently on, disable Start button, otherwise enable Start button and clear the UI as the game's state resets
            if (MyGame.On)
            {
                MyWindow.startButton.IsEnabled = false;
                MyWindow.startButton.Content = "START NEW GAME";
            }
            else
            {
                ClearUI();
                MyWindow.startButton.IsEnabled = true;
                MyWindow.startButton.Content = "START NEW GAME";
            }

            // Show blinds on title bar
            if (MyGame.SmallBlind == MyGame.BigBlind)
                MyWindow.Title = "Texas Hold'em Poker";
            else
                MyWindow.Title = $"Texas Hold'em Poker. Blind: {MyGame.SmallBlind}/{MyGame.BigBlind}";
        }
    }
}
