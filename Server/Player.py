"""
Handles everything related to a player, including his actions
"""

# Protocols for sending message to player
MSG_PROTOCOL = {
    'disconnect': "-1",
    'end_of_msg': "$",
    'hand': "00",
    'game': "01",
    'message': "02",
    'blind': "03",
    'showdown': "04",
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

    def disconnect(self):
        self.send_to_client('disconnect', "")
        self.sock.close()

    def modify_stack(self, amount):
        self.stack += amount
        self.stack = max(0, self.stack)

    def bet(self, bet_amount):
        self.stack -= bet_amount
        self.betting += bet_amount

    def check(self):
        pass

    def call(self, call_amount):
        self.bet(call_amount)

    def fold(self):
        self.in_hand = False

    def shove(self):
        self.all_in = True
        self.bet(self.stack)

    def hire(self):
        self.gm = True

    def fire(self):
        self.gm = False

    def send_to_client(self, protocol, msg):
        if msg != "":
            msg = " " + msg
        data = MSG_PROTOCOL[protocol] + msg + MSG_PROTOCOL['end_of_msg']
        self.sock.sendall(data.encode())
        print(f"Sent to {self.name}: {data[:-1]}")