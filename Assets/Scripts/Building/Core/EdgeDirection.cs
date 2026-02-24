using System;

[Flags]
public enum EdgeDirection
{
    None = 0,
    
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    Forward = 16,
    Back = 32,
    
    UpLeft = Up | Left,
    UpRight = Up | Right,
    UpForward = Up | Forward,
    UpBack = Up | Back,
    DownLeft = Down | Left,
    DownRight = Down | Right,
    DownForward = Down | Forward,
    DownBack = Down | Back,
    LeftForward = Left | Forward,
    LeftBack = Left | Back,
    RightForward = Right | Forward,
    RightBack = Right | Back,
    
    UpLeftForward = Up | Left | Forward,
    UpLeftBack = Up | Left | Back,
    UpRightForward = Up | Right | Forward,
    UpRightBack = Up | Right | Back,
    DownLeftForward = Down | Left | Forward,
    DownLeftBack = Down | Left | Back,
    DownRightForward = Down | Right | Forward,
    DownRightBack = Down | Right | Back
}
