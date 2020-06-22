"""
Defines the game's rules
"""


def get_card_reading(values):
    """
    Read the values of a list of values of cards
    :values: list
    :return: str
    """
    val_text = list(map(str, range(11))) + ['J', 'Q', 'K', 'A']
    return ", ".join(map(str, [val_text[val] for val in values]))

def best_hand(hands):
    """
    Returns the best 5-card hand among all hands passed into argument
    :hands: list of lists of int
    :return: list
    """
    return list(max(hands, key=hand_ranking))

def hand_ranking(five_cards):
    """
    Returns a list representing the ranking and high cards (if any) of a hand, where the
    first element is the hand's value, the higher the better, e.g. straight flush = 8,
    then all elements afterwards are highest cards.
    :five_cards: list of ints
    :return: list
    """
    cards_val = []
    cards_col = []
    for card in five_cards:
        cards_val.append((card % 13) + 2)
        cards_col.append(card // 13)
    if cards_col == [cards_col[0]] * 5:
        flush = True
    else:
        flush = False

    # Start checking for hand's value

    if flush and sorted(cards_val) == list(range(min(cards_val), max(cards_val) + 1)):
        return [8, max(cards_val)]  # straight flush

    elif flush and sorted(cards_val) == [2, 3, 4, 5, 14]:
        return [8, 5]  # straight flush of A,2,3,4,5

    elif len(set(cards_val)) == 2:
        for val in set(cards_val):
            if cards_val.count(val) == 4:
                one = max(set(cards_val) - {val})
                return [7, val, one]  # four of a kind
            elif cards_val.count(val) == 3:
                two = max(set(cards_val) - {val})
                return [6, val, two]  # full house

    elif flush:
        return [5] + sorted(cards_val, reverse=True)  # flush

    elif sorted(cards_val) == list(range(min(cards_val), max(cards_val) + 1)):
        return [4, max(cards_val)]  # straight

    elif sorted(cards_val) == [2, 3, 4, 5, 14]:
        return [4, 5]  # straight of A,2,3,4,5

    elif len(set(cards_val)) == 3:
        two = set()
        for val in set(cards_val):
            if cards_val.count(val) == 3:
                one = sorted(set(cards_val) - {val}, reverse=True)
                return [3, val] + one  # three of a kind
            elif cards_val.count(val) == 2:
                two.add(val)
        return [2] + sorted(two, reverse=True) + list(set(cards_val) - two)  # two pairs

    elif len(set(cards_val)) == 4:
        for val in set(cards_val):
            if cards_val.count(val) == 2:
                return [1, val] + sorted(set(cards_val) - {val}, reverse=True)  # one pair

    else:
        return [0] + sorted(cards_val, reverse=True)  # high card


def ranking_reader(ranking):
    """
    Reads the meaning of a hand ranking after being processed by the hand_ranking method
    :ranking: list
    :return: string
    """
    if ranking[0] == 0:
        return f'High card, kickers {get_card_reading(ranking[1:])}'
    elif ranking[0] == 1:
        return f'One pair of {get_card_reading([ranking[1]])}, kickers {get_card_reading(ranking[2:])}'
    elif ranking[0] == 2:
        return f'Two pairs of {get_card_reading(ranking[1:3])}, kicker {get_card_reading([ranking[3]])}'
    elif ranking[0] == 3:
        return f'Three of a kind {get_card_reading([ranking[1]])}, kicker {get_card_reading(ranking[2:])}'
    elif ranking[0] == 4:
        return f'Straight of {get_card_reading([ranking[1]])} high'
    elif ranking[0] == 5:
        return f'Flush, kickers {get_card_reading(ranking[1:])}'
    elif ranking[0] == 6:
        return f'Full house {get_card_reading([ranking[1]])} full of {get_card_reading([ranking[2]])}'
    elif ranking[0] == 7:
        return f'Four of a kind {get_card_reading([ranking[1]])}, kicker {get_card_reading([ranking[2]])}'
    elif ranking[0] == 8:
        return f'Straight flush of {get_card_reading([ranking[1]])} high'