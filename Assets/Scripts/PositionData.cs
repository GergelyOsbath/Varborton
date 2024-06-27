using System;
using UnityEngine;

public class PositionData
{
    public bool Following = false;
    public int PersonIndex;
    public Vector2 Position;
    public DateTime LastSeen = DateTime.Now;
    
    /// <summary>
    /// Update the position, and also the last seen time
    /// </summary>
    /// <param name="newPosition"></param>
    public void UpdatePosition(Vector2 newPosition)
    {
        Position = newPosition;
        LastSeen = DateTime.Now;
    }

    /// <summary>
    /// Change the Following state to true
    /// </summary>
    /// <returns></returns>
    public PositionData SetFollowing()
    {
        Following = true;
        return this;
    }
}