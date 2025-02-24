using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public enum Direction
    {
        None = -1,
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    [SerializeField] DirectionalPosition[] m_roomDirections;
    public DirectionalPosition[] RoomDirections { get { return m_roomDirections; } }

    [SerializeField] DirectionalPosition[] m_nonChangableRoomDirections;
    public DirectionalPosition[] NonChangableRoomDirections { get { return m_nonChangableRoomDirections; } }

    [SerializeField] Direction m_dirComeFrom = Direction.None;
    public Direction DirectionComingFrom { get { return m_dirComeFrom; } set { m_dirComeFrom = value; } }

    public List<Direction> AvaliableDirections()
    {
        List<Direction> list = new List<Direction>();

        foreach(DirectionalPosition dirPos in m_roomDirections)
        {
            if(dirPos.Direction != Direction.None) { list.Add(dirPos.Direction); }
        }

        return list;
    }

    public void RemoveDirection(Direction dir) 
    {
        for(int i = 0; i < m_roomDirections.Length; i++)
        {
            if (m_roomDirections[i].Direction == Direction.North && dir == Direction.South) { m_roomDirections[i].Direction = Direction.None; break; }
            else if (m_roomDirections[i].Direction == Direction.East && dir == Direction.West) { m_roomDirections[i].Direction = Direction.None; break; }
            else if (m_roomDirections[i].Direction == Direction.South && dir == Direction.North) { m_roomDirections[i].Direction = Direction.None; break; }
            else if (m_roomDirections[i].Direction == Direction.West && dir == Direction.East) { m_roomDirections[i].Direction = Direction.None; break; }
        }
    }

    public void RemoveDirectionNonRev(Direction dir)
    {
        for (int i = 0; i < m_roomDirections.Length; i++)
        {
            if (m_roomDirections[i].Direction == dir) { m_roomDirections[i].Direction = Direction.None; break; }
        }
    }

    public Vector3 GetDirectionPosition(Direction dir)
    {
        foreach(DirectionalPosition dirPos in m_nonChangableRoomDirections)
        {
            if(dirPos.Direction == dir) { return dirPos.Position; }
        }

        /*switch(dir)
        {
            case Direction.North:
                foreach(DirectionalPosition dirPos in m_nonChangableRoomDirections)
                {
                    if(dirPos.Direction == Direction.North)
                    {
                        return dirPos.Position;
                    }
                }
                break;
            case Direction.East:
                foreach (DirectionalPosition dirPos in m_nonChangableRoomDirections)
                {
                    if (dirPos.Direction == Direction.East)
                    {
                        return dirPos.Position;
                    }
                }
                break;
            case Direction.South:
                foreach (DirectionalPosition dirPos in m_nonChangableRoomDirections)
                {
                    if (dirPos.Direction == Direction.North)
                    {
                        return dirPos.Position;
                    }
                }
                break;
            case Direction.West:
                foreach (DirectionalPosition dirPos in m_nonChangableRoomDirections)
                {
                    if (dirPos.Direction == Direction.North)
                    {
                        return dirPos.Position;
                    }
                }
                break;
        }*/

        return Vector3.zero;
    }
}

[Serializable]
public struct DirectionalPosition
{
    public RoomInfo.Direction Direction;
    public Vector3 Position;
}