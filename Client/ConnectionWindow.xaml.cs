using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net;

namespace Poker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        string nameTextBoxDefault = "Name (max 20 characters)";
        string IPTextBoxDefault = "Server's IP address";
        string portTextBoxDefault = "Server's port";

        string myName;
        IPAddress ServerIP;
        int ServerPort;

        public ConnectionWindow()
        {
            InitializeComponent();
            SetDefaultValue(nameTextBox);
            SetDefaultValue(IPTextBox);
            SetDefaultValue(portTextBox);
            Client.AddElemToSendingProtocol();
            Client.AddElemToReceivingProtocol();

            //Testing stuffs
            
        }

        private bool CheckName()
        {
            string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            if (nameTextBox.Text == nameTextBoxDefault)
                return false;
            foreach (char c in nameTextBox.Text)
                if (!validChars.Contains(c))
                    return false;
            myName = nameTextBox.Text;
            return true;

        }

        private bool CheckIP()
        {
            int countDigits = 0;
            int countDots = 0;
            foreach (char c in IPTextBox.Text)
            {
                if (char.IsDigit(c))
                    countDigits += 1;
                else if (c == '.')
                    countDots += 1;
                else
                    return false;
            }
            if (countDigits == 0 || countDigits > 12 || countDots != 3 || IPTextBox.Text == "0.0.0.0")
                return false;
            string[] IPComponents = IPTextBox.Text.Split('.');
            foreach (string component in IPComponents)
                if (int.Parse(component) > 255)
                    return false;
            ServerIP = IPAddress.Parse(IPTextBox.Text);
            return true;
        }

        private bool CheckPort()
        {
            int port;
            try
            {
                port = int.Parse(portTextBox.Text);
            }
            catch
            {
                return false;
            }
            if (port < 0 || port > 65535)
                return false;
            ServerPort = port;
            return true;
        }

        private void SetDefaultValue(TextBox textBox)
        {
            string defaultValue;
            if (textBox == nameTextBox)
                defaultValue = nameTextBoxDefault;
            else if (textBox == IPTextBox)
                defaultValue = IPTextBoxDefault;
            else
                defaultValue = portTextBoxDefault;
            textBox.Text = defaultValue;
            textBox.Foreground = Brushes.Gray;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = (TextBox)e.OriginalSource;
            if (source.Foreground == Brushes.Black)
                return;
            source.Text = string.Empty;
            source.Foreground = Brushes.Black;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = (TextBox)e.OriginalSource;
            if (string.IsNullOrEmpty(source.Text))
                SetDefaultValue(source);
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            string errorMsg = "";
            if (!CheckName())
                errorMsg += "- Name can only contain alphanumeric characters. No space or special characters allowed.\n";
            if (!CheckIP())
                errorMsg += "- IP Address must be of type xxx.xxx.xxx.xxx where xxx is between 0 and 255 inclusive.\n";
            if (!CheckPort())
                errorMsg += "- Port must be between 0 and 65535 inclusive.\n";
            if (!string.IsNullOrEmpty(errorMsg))
            {
                errorMsg.Remove(errorMsg.Length - 1);
                MessageBox.Show(errorMsg, "Input Error");
                return;
            }
            goButton.Content = "Connecting...";
            goButton.IsEnabled = false;
            Client client = new Client(ServerIP, ServerPort);
            if (client.PlayerConnect(myName))
            {
                new MainWindow(myName, client).Show();
                Close();
            }
            else
            {
                MessageBox.Show("Cannot connect to server.", "Connection Error");
                goButton.Content = "Connect to Game";
                goButton.IsEnabled = true;
            }
        }
    }
}
