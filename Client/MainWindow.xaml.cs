using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Poker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Game MyGame;
        Client myClient;
        public StackPanel[] CommunityPanels;
        public TextBlock[] PotBlocks;

        public MainWindow(string myName, Client myClient)
        {
            InitializeComponent();
            if (myName == "TrungDam")
                gmPanel.Visibility = Visibility.Visible;
            MyGame = new Game(myName);
            this.myClient = myClient;
            myClient.MyGame = MyGame;

            CommunityPanels = new StackPanel[] { community0, community1, community2, community3, community4 };
            PotBlocks = new TextBlock[] { pot0, pot1, pot2, pot3, pot4 };
            Drawing.MyGame = MyGame;
            Drawing.MyWindow = this;

            TextBlock[] nameBlocks = { nameP0, nameP1, nameP2, nameP3, nameP4, nameP5 };
            TextBlock[] cashBlocks = { cashP0, cashP1, cashP2, cashP3, cashP4, cashP5 };
            TextBlock[] bettingBlocks = { bettingP0, bettingP1, bettingP2, bettingP3, bettingP4, bettingP5 };
            Ellipse[] dealerChips = new Ellipse[] { dealer0, dealer1, dealer2, dealer3, dealer4, dealer5 };
            Ellipse[] avatars = new Ellipse[] { avatarP0, avatarP1, avatarP2, avatarP3, avatarP4, avatarP5 };
            StackPanel[] cards0 = new StackPanel[] { card0P0, card0P1, card0P2, card0P3, card0P4, card0P5 };
            StackPanel[] cards1 = new StackPanel[] { card1P0, card1P1, card1P2, card1P3, card1P4, card1P5 };

            for (byte i = 0; i < Game.MAX_PLAYERS; i++)
            {
                Player newPlayer = new Player(nameBlocks[i], cashBlocks[i], bettingBlocks[i], dealerChips[i], avatars[i], cards0[i], cards1[i]);
                MyGame.AddPlayer(newPlayer, i);
            }
            myClient.StartListening();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myClient.Disconnect();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            myClient.SendToServer("start", "");
        }

        private void blindsButton_Click(object sender, RoutedEventArgs e)
        {
            int sb = int.Parse(sbTextBox.Text);
            int bb = int.Parse(bbTextBox.Text);
            if (sb == 0 || bb == 0 || (double)sb > (double)bb / 2)
            {
                MessageBox.Show("Blinds not valid. Try again.");
            }
            else
            {
                myClient.SendToServer("blind", $"{sb}:{bb}");
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string[] text = commandTextBox.Text.Split(' ', 2);
            string protocol = text[0];
            string message = text[1];
            myClient.SendToServer(protocol, message);
        }

        private void betButton_Click(object sender, RoutedEventArgs e)
        {
            CheckBetAmountInput();
            bool all_in;
            string msg;
            if (betButton.Content.ToString() == "All in")
            {
                msg = "Go all in?";
                all_in = true;
            }
            else
            {
                msg = $"Bet {betAmount.Text} chips?";
                all_in = false;
            }
            MessageBoxResult result = MessageBox.Show(msg, "Bet confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                if (all_in)
                    myClient.SendToServer("action", "4 0");
                else
                    myClient.SendToServer("action", $"5 {betAmount.Text}");
            }                
        }

        private void checkCallButton_Click(object sender, RoutedEventArgs e)
        {
            if (checkCallButton.Content.ToString() == "Check")
                myClient.SendToServer("action", "2 0");
            else
                myClient.SendToServer("action", "3 0");
        }

        private void foldButton_Click(object sender, RoutedEventArgs e)
        {
            myClient.SendToServer("action", "1 0");
        }

        private void betAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Player self = MyGame.Players[MyGame.MySeat];
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void betAmount_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckBetAmountInput();
        }

        private void CheckBetAmountInput()
        {
            Player self = MyGame.Players[MyGame.MySeat];
            uint minimumBet;
            uint maximumSpending = self.Betting + self.Stack;

            if (MyGame.HighestBet == 0)
                minimumBet = MyGame.BigBlind;
            else
                minimumBet = Math.Min(self.Stack, 2 * MyGame.HighestBet - MyGame.SecondHighestBet);
            if (uint.Parse(betAmount.Text) < minimumBet)
                betAmount.Text = minimumBet.ToString();
            else if (uint.Parse(betAmount.Text) > maximumSpending)
                betAmount.Text = maximumSpending.ToString();
            if (betAmount.Text == maximumSpending.ToString())
                betButton.Content = "All in";
            else
                betButton.Content = "Bet";
        }
    }
}