using Godot;
using System;

public enum CardSuit {Fins, Teefs, Tails, Scales}
public enum CardRank
{
    Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Puffer, Butter, Whale, Shark 
}

public class Card
{
    public CardRank Rank {get;set;}
    public CardSuit Suit {get;set;}

    public Card(CardRank rank, CardSuit suit)
    {
        this.Rank = rank;
        this.Suit = suit;
    }
}
