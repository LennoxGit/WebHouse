﻿namespace WebHouse_Client.Logic;

//Die Farrben die eine Karte haben kann und die auf den ChapterCards angezeigt werden
public enum CardColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Pink,
    White
}

public static class CardColorConverter
{
    public static Color GetColor(this CardColor cardColor)
    {
        //Gibt den Farben ihre RGB Werte
        return cardColor switch
        {
            CardColor.Red => Color.FromArgb(255, 228, 3, 9),
            CardColor.Green => Color.FromArgb(255, 114,177, 29),
            CardColor.Blue => Color.FromArgb(255, 0, 159, 220),
            CardColor.Yellow => Color.FromArgb(255, 255, 243, 0),
            CardColor.Pink => Color.FromArgb(255, 243, 77, 146),
            CardColor.White => Color.White,
            _ => throw new ArgumentOutOfRangeException(nameof(cardColor), cardColor, null)
        };
    }
}