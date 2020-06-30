"""
Handles the main game's flow
"""

from itertools import cycle, combinations
import random
from time import sleep
from Deck import Deck
from Player import Player
from Chat import Chat
from Rules import hand_ranking, best_hand, ranking_reader


MAX_PLAYERS = 6


class Game:
    def __init__(self):
        self.players = [None] * MAX_PLAYERS  # List of players
        self.pots = [0]  # List of pots, including main and side pots
        self.pot_players = [[]]  # List of corresponding players in each pot
        self.deck = None  # Current game's deck
        self.round = 0  # 0: pre-flop, 1: flop, 2: turn, 3: river, 4: showdown
        self.dealer = 0  # Index of player with the dealer's chip
        self.highest_bet = 0  # Current highest bet on the table
        self.second_highest_bet = 0  # Second highest bet on the table
        self.acting = 0  # Index of player having to act now
        self.last_to_act = 0  # Index of last player to act
        self.community = []  # List of community cards
        self.sb = 0  # Small blind
        self.bb = 0  # Big blind
        self.on = False  # Game on
        self.chat = Chat()  # Chat object for debugging


    def set_blinds(self, sb, bb):
        """
        Sets the small & big blinds
        :sb: int
        :bb: int
        :return: None
        """
        self.sb = sb
        self.bb = bb
        self.chat.update_chat(f"Blinds set to {sb}/{bb}")


    def add_player(self, name, player_socket, name_changed):
        """
        Adds a player to the list of players
        :name: str
        :player_socket: socket object corresponding to a player
        :name_changed: bool
        :return: the Player object created
        """
        player = Player(name, player_socket)

        # Check if the table is full, the player is then disconnected
        for p in self.players:
            if not p:
                break
        else:
            self.remove_player(player)
            return None

        self.chat.update_chat(f"{player} has been added.")
        
        # Assign a random seat
        while True:
            i = random.choice(range(MAX_PLAYERS))
            if not self.players[i]:
                self.player_sit(player, i)
                break
        if name_changed:
            # If player's name is changed because the chosen name is taken, send the new name to the player
            player.send_to_client('name', name)
            sleep(1)
        self.send_game_state()
        if name_changed:
            # Notify the new name to the player
            player.send_to_client('message', f"You picked an existed name. Your new name is now {name}.")
        return player


    def remove_player(self, player):
        """
        Removes a player from the list of players
        :player: Player object
        :return: None
        """
        if player in self.players:
            self.player_stand(player)
        player.disconnect()
        self.chat.update_chat(f"{player} has been removed.")
        self.send_game_state()

    def check_name_exist(self, name):
        """
        Check if a name already exists
        :name: str
        :return: bool
        """
        for player in self.players:
            if not player:
                continue
            if name == player.name:
                return False
        return True

    def player_sit(self, player, seat):
        """
        Sit a player. The seat number is the player's index in the players list
        :player: Player object
        :seat: int
        :return: None
        """
        self.players[seat] = player
        self.chat.update_chat(f"{player} took seat #{seat}.")


    def player_stand(self, player):
        """
        Make a player stand up
        :player: Player object
        :return: None
        """
        seat = self.players.index(player)
        self.players[seat] = None
        self.chat.update_chat(f"{player} stood up.")


    def can_start(self):
        """
        Check if current game state allows starting a new game
        """
        count_players_with_chip = 0
        for player in self.players:
            if not player or not player.stack:
                continue
            count_players_with_chip += 1
        if self.sb == 0 or self.bb == 0 or count_players_with_chip <= 1:
            return False
        return True


    def new_game(self):
        """
        Starts a new game
        :return: None
        """
        self.deck = Deck()
        self.round = 0
        self.highest_bet = self.bb
        self.second_highest_bet = 0
        self.community = []
        self.pot_players = [[]]

        # Cycle through the players list to find the next dealer, deal 2 cards, and other housekeeping stuffs
        num_players = 0
        first_player_seat = -1
        next_dealer_found = False
        playing = []
        for i, player in enumerate(self.players):
            if not player:
                continue
            if not player.stack:
                player.in_hand = False
                player.hand = []
                continue
            if first_player_seat == -1:
                first_player_seat = i
            playing.append(i)
            player.in_hand = True
            player.all_in = False
            player.hand = self.deck.deal_cards(2)
            num_players += 1
            if i > self.dealer:
                self.dealer = i
                next_dealer_found = True
            # Send player's hand over the network
            msg = ""
            for card in player.hand:
                if card < 10:
                    msg += "0"
                msg += str(card)
            player.send_to_client('hand', msg)
        if not next_dealer_found:
            self.dealer = first_player_seat

        if num_players == 2:
            sb_pos = self.acting = self.dealer
            playing.remove(sb_pos)
            bb_pos = playing[0]
        else:
            cyc = cycle(playing)
            for i in cyc:
                if i == self.dealer:
                    sb_pos = next(cyc)
                    bb_pos = next(cyc)
                    self.acting = next(cyc)
                    break

        self.last_to_act = bb_pos
        sb_player = self.players[sb_pos]
        bb_player = self.players[bb_pos]
        if self.sb >= sb_player.stack:
            sb_player.shove()
        else:
            sb_player.bet(self.sb)
        if self.bb >= bb_player.stack:
            bb_player.shove()
        else:
            bb_player.bet(self.bb)
        self.on = True
        self.send_game_state()


    def gather_chips(self):
        """
        Gathers everyone's chips into the middle at the end of each round
        :return: None
        """
        in_hand = [player for player in self.players if player and player.in_hand]

        list_bet = sorted(set([player.betting for player in in_hand if player.betting > 0]))
        for i in range(len(list_bet)):
            diff = list_bet[i]
            if i > 0:
                self.pots.append(0)
                self.pot_players.append([])
                diff = list_bet[i] - list_bet[i - 1]
            pot_players = []
            for player in self.players:
                if not player or player.betting == 0:
                    continue
                if player in in_hand and player.betting >= diff:
                    pot_players.append(player)
                into_pot = min(player.betting, diff)
                self.pots[-1] += into_pot
                player.betting -= into_pot
            self.pot_players[-1] = pot_players
        if len(self.pot_players[-1]) == 1:
            player = self.pot_players[-1][0]  # The only one player in a side pot, i.e. he should get a refund
            pot_money = self.pots[-1]
            player.stack += pot_money
            del self.pot_players[-1]
            del self.pots[-1]


    def end_game(self):
        """
        Handles the game's ending
        :return: None
        """
        if self.round == 4:  # Hands showdown, i.e. after the river
            num_pots = len(self.pots)
            left_over = 0
            results = []
            for i in range(1, num_pots + 1):
                self.pots[-i] += left_over  # Left-over is passed on to the next side-pot
                winners = []
                winning_hand = [0]
                for player in self.pot_players[-i]:
                    seven_cards = self.community + player.hand
                    player_best = hand_ranking(best_hand(combinations(seven_cards, 5)))
                    if player_best > winning_hand:
                        winning_hand = player_best
                        winners = [player]
                    elif player_best == winning_hand:
                        winners.append(player)
                for winner in winners:
                    winner.stack += self.pots[-i] // len(winners)

                pot_results = [self.pots[-i] // len(winners), winners, winning_hand]
                results.insert(0, pot_results)
                left_over = self.pots[-i] % len(winners)

            self.pots = [left_over]  # Finally if there is any left-over, it is passed on to the next game

        else:  # Game ending before showdown means that all but one player have folded
            winning_amount = sum(self.pots)
            for player in self.players:
                if not player:
                    continue
                if player.in_hand:
                    winner = player  # The sole winner has been determined
                winning_amount += player.betting
                player.betting = 0
            winner.stack += winning_amount
            results = [[winning_amount, [winner], None]]

        self.pots = [0]
        self.on = False
        self.announce_winners(results)


    def act(self, actor, action, amt):
        """
        Handles an action from a player (called actor)
        :actor: the Player object currently acting
        :action: string, the action itself
        :amt: int, amount of chips in case action == "bet"
        :return: None
        """
        bet_diff = self.highest_bet - self.second_highest_bet
        end_round = False
        in_hand = []
        not_all_in = []
        actor_index = self.players.index(actor)
        no_raising = False

        for i, player in enumerate(self.players):
            if player and player.in_hand:
                in_hand.append(i)
                if not player.all_in:
                    not_all_in.append(i)

        if action in ["fold", "check", "call"] and self.acting == self.last_to_act:
            end_round = True

        if action == "fold":
            actor.fold()
            in_hand.remove(actor_index)
            if not end_round:
                find_next_acting = cycle(not_all_in)
                for i in find_next_acting:
                    if i == actor_index:
                        self.acting = next(find_next_acting)
                        break
            not_all_in.remove(actor_index)
            for pot in self.pot_players:
                if actor in pot:
                    pot.remove(actor)
            self.chat.update_chat(f"{actor} folded.")

        elif action == "check":
            actor.check()
            find_next_acting = cycle(not_all_in)
            if not end_round:
                for i in find_next_acting:
                    if i == actor_index:
                        self.acting = next(find_next_acting)
                        break
            self.chat.update_chat(f"{actor} checked.")

        elif action == "call":
            # NOTE: must implement in client's UI that when everyone but one player has gone all-in, this player can
            # only fold or call, he can never shove

            call_amount = min(self.highest_bet - actor.betting, actor.stack)
            actor.call(call_amount)
            call_all_in = ""
            if not end_round:
                find_next_acting = cycle(not_all_in)
                for i in find_next_acting:
                    if i == actor_index:
                        self.acting = next(find_next_acting)
                        break
            if actor.all_in:
                not_all_in.remove(actor_index)
                call_all_in = "ALL IN "
            self.chat.update_chat(f"{actor} called {call_all_in}{call_amount} chips.")

        elif action == "shove":
            actor.shove()
            find_next_acting = cycle(not_all_in)
            for i in find_next_acting:
                if i == actor_index:
                    self.acting = next(find_next_acting)
                    break
            find_last_to_act = cycle(reversed(not_all_in))
            for i in find_last_to_act:
                if i == actor_index:
                    self.last_to_act = next(find_last_to_act)
                    break
            not_all_in.remove(actor_index)
            if actor.betting - self.highest_bet >= bet_diff:
                self.second_highest_bet, self.highest_bet = self.highest_bet, actor.betting
            else:
                self.highest_bet = actor.betting
            self.chat.update_chat(f"{actor} shoved ALL IN {actor.betting} chips.")

        elif action == "bet":
            actor.bet(amt)
            find_next_acting = cycle(not_all_in)
            for i in find_next_acting:
                if i == actor_index:
                    self.acting = next(find_next_acting)
                    break
            find_last_to_act = cycle(reversed(not_all_in))
            for i in find_last_to_act:
                if i == actor_index:
                    self.last_to_act = next(find_last_to_act)
                    break
            self.second_highest_bet, self.highest_bet = self.highest_bet, actor.betting
            self.chat.update_chat(f"{actor} raised {amt} chips to the total of {actor.betting}.")

        # After an action, the following lines of code handle the game's state

        if len(not_all_in) == 1:  # Testing
            no_raising = True

        if len(in_hand) == 1:  # If everyone folds and only 1 player is left, game over
            self.end_game()

        elif end_round and self.round <= 3:
            self.gather_chips()
            self.round += 1
            self.highest_bet = self.second_highest_bet = 0
            if len(not_all_in) >= 2:
                # When the next round starts, decide who goes first and goes last
                for i in not_all_in:
                    if i > self.dealer:
                        self.acting = i
                        break
                else:
                    self.acting = not_all_in[0]
                find_last_to_act = cycle(reversed(not_all_in))
                for i in find_last_to_act:
                    if i == self.acting:
                        self.last_to_act = next(find_last_to_act)
                        break
            else:
                # When everyone has gone all in, go to show down
                self.draw_the_rest()
                self.end_game()
                # self.send_game_state()  # REVIEW
                return

            if self.round == 4:  # If it's showdown, now it's time to find the winner(s)
                self.showdown()
                self.end_game()
                return  # REVIEW
            elif self.round == 1:  # Preflop -> Flop
                self.community = self.deck.deal_cards(3)
                self.chat.update_chat("Flop: " + str(self.community))
            elif self.round == 2:  # Flop -> Turn
                self.community.append(self.deck.deal_cards(1)[0])
                self.chat.update_chat("Turn: " + str(self.community[3]))
            elif self.round == 3:  # Turn -> River
                self.community.append(self.deck.deal_cards(1)[0])
                self.chat.update_chat("River: " + str(self.community[4]))

        self.send_game_state(no_raising)


    def announce_winners(self, results):
        """
        Announce the winner(s) and their winning
        :results: list, input from the end_game function
        :return: None
        """
        msg = ""
        for i, result in enumerate(results):
            winning_amount, winners, winning_hand = result
            win = "win" if len(winners) == 1 else "split"
            if i == 0:
                pot_name = "main pot"
            else:
                pot_name = "side pot " + str(i)
            if i == 0 and len(results) == 1 and winning_hand == None:  # Only one winner because everyone else folds
                msg = f"{winners[0]} wins {winning_amount} chips"
            else:
                msg += f"{', '.join(map(str, winners))} {win} {winning_amount} chips from the {pot_name} with {ranking_reader(winning_hand)}\n"
        sleep(0.5)  # Sleep for 0.5 sec to make sure every previous message is properly parsed and executed by clients before sending the message
        self.send_all('announcement', msg)
        

    def send_all(self, protocol, msg):
        """
        Send a message over the network to all players
        :protocol: str, keys in Player.sp
        :msg: str, message to be sent over the network
        :return: None
        """
        for player in self.players:
            if player:
                player.send_to_client(protocol, msg)
        

    def draw_the_rest(self):
        """
        Draw the rest of the cards in the community
        """
        self.showdown()
        if self.round == 1:  # Pre-flop all-in
            self.community += self.deck.deal_cards(3)  # Draw 3 flop cards
            self.send_game_state()
            self.round += 1
            sleep(1)
        if self.round == 2:  # Flop all-in
            self.community += self.deck.deal_cards(1)  # Draw turn card
            self.send_game_state()
            self.round += 1
            sleep(1)
        if self.round == 3:  # Turn all-in
            self.community += self.deck.deal_cards(1)  # Draw river card
            self.send_game_state()
            self.round += 1
            sleep(1)


    def showdown(self):
        """
        Show cards of everyone still playing
        """
        players_info = []
        for i, player in enumerate(self.players):
            if not player or not player.in_hand:
                continue
            msg = f"{i}:"
            for card in player.hand:
                if card < 10:
                    msg += "0"
                msg += str(card)
            players_info.append(msg)
        msg = ' '.join(players_info)
        self.send_all('showdown', msg)


    def send_game_state(self, *args):
        """
        Shortcut to send game's state to all players
        """
        if args and args[0]:
            msg = self.game_info(True)
        else:
            msg = self.game_info()
        sleep(0.5)  # Sleep for 0.5 sec to make sure every previous message is properly parsed and executed by clients before sending the current message
        self.send_all('game', msg)


    def game_info(self, *args):
        """
        Generate a string containing all info of current game to send over the network
        :return: str
        """
        # Message example: ON(1) PL(0:TrungDam:100:10:1,1:_,2:_) PT(100:50) DL(0) AC(0) CM(0:33:15)
        msg = ""

        # Game on or off
        msg += f"ON({int(self.on)}) "

        # Blinds
        msg += f"BL({self.sb}:{self.bb}) "

        # Players currently sitting
        players_info = []
        for i, player in enumerate(self.players):
            if player:
                players_info.append(f"{i}:{player.name}:{player.stack}:{player.betting}:{int(player.in_hand)}")
            else:
                players_info.append(f"{i}:_")
        msg += f"PL({','.join(players_info)}) "

        # Highest & 2nd highest bet
        msg += f"BT({self.highest_bet}:{self.second_highest_bet}) "
        
        # Main and side pots
        msg += f"PT({':'.join(map(str, self.pots))}) "

        # Dealer's chip and action position
        msg += f"DL({self.dealer}) "
        msg += f"AC({self.acting}) "

        # Community
        if len(self.community) == 0:
            msg += "CM() "
        else:
            msg += f"CM({':'.join(map(str, self.community))}) "
        
        # Test functionality (no betting)
        if (args and args[0]):
            msg += f"NR(1)"
        else:
            msg += f"NR(0)"
        return msg            