# Poker-with-Python-and-CSharp
A long but rewarding personal project

Server side is coded in pure Python

Client side is coded with C# using Visual Studio 2019 whose UI is rendered in Windows Presentation Foundation (WPF)'s XAML files.

First, one should start the server by executing `python Server.py` in the `Server` folder. By default, the server's IP address and port is `127.0.0.1:11000`. The server should start listening to connections.

Once the server has been started, the clients can start connecting to the server by entering in the form the player's name and the server's IP and port.

By default, if the player's name is "TrungDam", the form recognizes that it is the Game Master (GM) therefore enables the GM's panel to control the game from the form.

Before starting the first game, the GM should set the blinds so that the game would work correctly. He should also make sure that there are at least 2 players present in the game, with chips in hand. Restrictions will be applied in future updates to prevent a "untrained" GM from messing up the game's parameters incorrectly.

Once the game is started, the server handles all of the game's flow and logic. Each action taken by a player is processed by the server to make changes to the game's state. The server then generate a string containing all information of the current game and send it to all players. This message is received and parsed by the clients to make changes to their own version of the game's state, which should be in sync with the server's version at all time. After making changes to the game's state, the form is responsible for updating the UI with the new information.

Coming in future updates:
- Name check to prevent name duplicate (should be very easy, but not implemented at the moment)
- Better UI (appearance, animations, sounds, responsiveness, card highlight...)
- More GM functions and restrictions
- Other Poker rules (Stud, Pot-Limit Omaha, etc...)
- Chat
- Avatar
