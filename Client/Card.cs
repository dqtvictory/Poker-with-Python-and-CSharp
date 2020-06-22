using System;

namespace Poker
{
    public static class Card
    {
        public static string ReadSuit(int cardIndex)
        {
            int suit = Math.DivRem(cardIndex, 13, out int value);
            string[] suits = { "♠", "♣", "♥", "♦" };
            return suits[suit];
        }

        public static string ReadValue(int cardIndex)
        {
            int suit = Math.DivRem(cardIndex, 13, out int value);
            value += 2;
            string[] values = { "J", "Q", "K", "A" };
            if (value <= 10)
                return value.ToString();
            return values[value - 11];
        }
    }
}
