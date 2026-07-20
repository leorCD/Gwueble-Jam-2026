using Godot;
using System;

public enum CardSuit {Spades, Hearts, Diamonds, Clubs}
public enum CardRank
{
    Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, 
    Jack, Queen, King
}

public class Card
{
    public int Value {get;set;}
    public CardSuit Suit {get;set;}

    public Card(int value, CardSuit suit)
    {
        this.Value = value;
        this.Suit = suit;
    }
}
