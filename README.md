# Poker-with-Python-and-CSharp


## HOW TO:

__Server side:__
- Must have Python 3. No extra dependencies or 3rd party libraries required
- Must have an open port for server's side communication. The OS must also allow traffic through this port. For connection through the Internet, NAT must be set to bridge connection from the local machine (via the said port) to the Internet (via an open port on the modem)
- Must support multi-threading to handle multiple client's connection at once
- To start the server, set the correct IPv4 __local network's__ address to the `SERVER_IP` variable in `Server.py` and a port to `PORT`. Leave the `BUFFER` variable unchanged to avoid bad surprises. Run `python Server.py`. Note that since almost no computer is connected directly to the Internet without going through a modem, server will not work if `SERVER_IP` is set to a public IP
- Once the server starts to listen to connections, clients can now jump in

__Client side:__
- Must have .NET Core v3 installed. If not, user will be prompted to download and install
- Windows 10
- Screen resolution 720p (1280 x 720) or better
- At least 1GB of RAM

__Build from source:__
- Must have Visual Studio 2019 installed
- Open the `Poker.sln` solution. If missing any packages, VS will prompt user to download
- Choose the Release scheme, then Build (Ctrl + B)


## Update 1:

__Changes made to the Server:__
- Name check functionality added. If a name is found duplicated, automatically adds a digit after the name to make it unique
- Automatically disconnect new players if the table is already full
- Check if game can start functionality added. If a GM requests to start a game while there aren't enough players or blinds are not set correctly, game will not start


__Changes made to the Client:__
- Interface is resized smaller to fit in lower-resolution screens but still looks decent on 4K monitors
- Chat box added with fully working chat between players (UTF-8 encoding)
- Cleaner UI: lower part of the window is added to hold player's action panel and the chat box, leaving the main part of the UI free from controls (buttons, text boxes...)
- Messages to display sent by the server used to be shown in a message box, which makes the UI stop being responsive while the message is being displayed. Messages are now shown directly in the chat box prefixed by the server's timestamp


__Bugs fixed:__
- If there is only one player left to call or fold while facing an all-in, this player will not be able to re-raise since there is no one left to call.
- Showdown now lasts for as long as the Game Master wants. Before, a showdown lasted for only not even a second, but is kept visually because of the message box's nature to block the UI. Since this is not anymore the case, showdown needs to last longer so that players can see others' hands until the Game Master tells the server to go on.


__Stuffs to do:__
- Better UI (animations, sounds, card highlight...)
- More GM functions (kick player, force a player to act, full control over game's state)
- Timer
- Avatar
- Other Poker rules (Stud, Pot-Limit Omaha, etc...)
- Better code refactoring
- Fix more bugs


## Initial Upload:

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