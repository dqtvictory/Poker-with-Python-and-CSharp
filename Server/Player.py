"""
Handles everything related to a player, including his actions
"""

from datetime import datetime


# Protocols for sending message to player
SEND_PROTOCOL = {
    'disconnect': "-1",
    'hand': "00",
    'game': "01",
    'message': "02",
    'name': "03",
    'showdown': "04",
    'announcement': "05",
    'end_of_msg': "$",
}


class Player:
    def __init__(self, name, sock):
        self.name = name
        self.sock = sock
        self.stack = 200
        self.betting = 0
        self.in_hand = False
        self.all_in = False
        self.hand = []
        self.gm = False
        if name == "TrungDam":
            self.hire()

    def __repr__(self):
        return self.name

    def modify_stack(self, amount):
        self.stack += amount
        self.stack = max(0, self.stack)

    def bet(self, bet_amount):
        self.stack -= bet_amount
        self.betting += bet_amount
        if self.stack == 0:
            self.all_in = True

    def check(self):
        pass

    def call(self, call_amount):
        self.bet(call_amount)

    def fold(self):
        self.in_hand = False

    def shove(self):
        self.bet(self.stack)

    def hire(self):
        self.gm = True

    def fire(self):
        self.gm = False

    def send_to_client(self, protocol, msg):
        if msg != "":
            msg = " " + msg
        # If a text message is sent to player, it should include a time stamp
        if protocol == 'message':
            msg = f" [{datetime.now().strftime('%H:%M:%S')}] " + msg

        data = SEND_PROTOCOL[protocol] + msg + SEND_PROTOCOL['end_of_msg']
        self.sock.sendall(data.encode())
        print(f"Sent to {self.name}: {data[:-1]}")

    def disconnect(self):
        self.send_to_client('disconnect', "")
        self.sock.close()