"""
Handles the connection between server and clients, and network protocols, etc...
"""

import socketserver
from Game import Game


SERVER_IP = "127.0.0.1"
PORT = 11000
BUFFER = 1024  # Buffer size (in bytes) to receive message over the network

# Protocols for parsing message from client
RECV_PROTOCOL = {
    'disconnect': "-1",
    'connected': "00",
    'start': "01",
    'blind': "02",
    'action': "03",
    'chat': "04",
    'stack': "05",
    'request_state': "06",
    'end_of_msg': "$",
}


class ServerReqHandler(socketserver.BaseRequestHandler):
    def __init__(self, request, client_address, server):
        super().__init__(request, client_address, server)
        self.player = None

    def handle(self):
        print(self.client_address, "connected.")
        while True:
            msg = ""
            msg_received = False
            while not msg_received:
                data_received = self.request.recv(BUFFER).decode()
                i = data_received.find(RECV_PROTOCOL['end_of_msg'])
                if i == -1:
                    msg += data_received
                else:
                    msg += data_received[:i]
                    msg_received = True
            print(f">> RECEIVED FROM {self.client_address}: {msg}")
            status = self.parse_message(msg)
            if status == -1:
                self.server.shutdown()
                break

    def parse_message(self, msg):
        """
        Get the meaning from the message sent by the client to take proper actions
        :msg: str
        :return: int
        """
        protocol = msg[:2]
        try:
            command = msg[3:]
        except:
            pass

        if protocol == RECV_PROTOCOL['disconnect']:
            # Client requests to disconnect. Example: -1
            game.remove_player(self.player)
            self.request.close()

        elif protocol == RECV_PROTOCOL['connected']:
            # First message received from client when connected. Example: 00 TrungDam
            # If name already exists, changes it so it's unique
            name = command
            name_changed = False
            i = 0
            while not game.check_name_exist(name):
                name += str(i)
                i += 1
                name_changed = True
            player = game.add_player(name, self.request, name_changed)
            if player:
                self.player = player

        elif protocol == RECV_PROTOCOL['start']:
            # Game master's order to start a new game. Example: 01
            if self.player.gm and not game.on:
                if game.can_start():
                    game.new_game()
                else:
                    self.player.send_to_client('message', "Game cannot start. Not enough player to play, or blind not set.")

        elif protocol == RECV_PROTOCOL['blind']:
            # Game master's order to set small & big blinds. Example: 02 50:100
            if self.player.gm:
                SEPERATOR = ':'
                i = command.find(SEPERATOR)
                sb = int(command[:i])
                bb = int(command[i+1:])
                game.set_blinds(sb, bb)

        elif protocol == RECV_PROTOCOL['action']:
            # A player's action during a game. Example: 03 3 0
            ACTIONS = {
                1: "fold",
                2: "check",
                3: "call",
                4: "shove",
                5: "bet",
            }
            i = int(command[0])
            amt = int(command[2:])
            if i == 5:
                amt -= self.player.betting
            game.act(self.player, ACTIONS[i], amt)

        elif protocol == RECV_PROTOCOL['chat']:
            # A player's chat message
            echo_message = f"{self.player}: {command}"
            game.chat.update_chat(echo_message)
            game.send_all('message', echo_message)

        elif protocol == RECV_PROTOCOL['stack']:
            # Game master's order to set a player's stack. Example: 02 1 -15
            if self.player.gm:
                info = command.split(' ')
                game.players[int(info[0])].modify_stack(int(info[1]))
                game.send_game_state()

        elif protocol == RECV_PROTOCOL['request_state']:
            # Game master's request of game's state, often occurs after announcement of winners
            if self.player.gm:
                game.send_game_state()

        return int(protocol)


if __name__ == "__main__":
    with socketserver.ThreadingTCPServer((SERVER_IP, PORT), ServerReqHandler) as server:
        print("Server started. Listening on", server.server_address)
        game = Game()
        print("New game created. Waiting for players...")
        server.serve_forever()
    print("Server terminated.")