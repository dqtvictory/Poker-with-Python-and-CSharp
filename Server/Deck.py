"""
Handles all operations related to cards in a deck for a game
"""

from secrets import choice


class Deck:
    def __init__(self):
        self.cards = []
        self.get_deck()

    def get_deck(self):
        """
        Generate a crypto-secured randomly shuffled deck of 52 cards
        :return: None
        """
        ordered_deck = [i for i in range(52)]
        for _ in range(52):
            card = choice(ordered_deck)
            self.cards.append(card)
            ordered_deck.remove(card)

    def deal_cards(self, n):
        """
        Deal n cards
        :return: list
        """
        return [self.cards.pop() for _ in range(n)]
