using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering.Universal;

public class LevelGenerator : MonoBehaviour
{
    #region Variables
    //Singleton
    private static LevelGenerator m_instance;
    public static LevelGenerator Instance { get { return m_instance; } }

    [Header("Level Seed", order = 1)]
    
    [SerializeField] private int m_levelSeed = 1;
    public int GetLevelSeed { get { return m_levelSeed; } }

    private System.Random m_randomGenerator;

    [Header("Level Generation Type", order = 2)]
    [SerializeField] private int m_maxBranchSize = 10;
    public int MaxBranchySize { get { return m_maxBranchSize; } set { m_maxBranchSize = value; } }
    private int m_offBranchChance = 50;
    public int OffBranchChance { get { return m_offBranchChance; } set { m_offBranchChance = value; } }

    [Header("Floor Variables", order = 3)]
    [SerializeField] private GameObject m_floorPrefab;
    [SerializeField] private Transform m_floorParent;
    private List<GameObject> m_floors = new List<GameObject>();
    public int GetFloorCount { get { return m_floors.Count; } }
    private List<Vector3> m_floorsPos = new List<Vector3>();

    //Other
    //enum Direction
    //{
    //    Up,
    //    Right,
    //    Down,
    //    Left
    //}
    //Direction[] defaultDir = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
    int branchTermination = 0;
    bool m_allowConnections = false;
    public bool AllowConnections { get { return m_allowConnections; } set { m_allowConnections = value; } }

    bool m_manualNOBFB = false;
    public bool ManualNOBFB { get { return m_manualNOBFB; } set { m_manualNOBFB = value; } }

    int m_numberOfBranchesFromBase = 1;
    public int NumberOfBranchesFromBase { get { return m_numberOfBranchesFromBase; } set { m_numberOfBranchesFromBase = value; } }

    List<GameObject> m_prefabList = new List<GameObject>();
    List<GameObject> m_northPrefabs = new List<GameObject>();
    List<GameObject> m_eastPrefabs = new List<GameObject>();
    List<GameObject> m_southPrefabs = new List<GameObject>();
    List<GameObject> m_westPrefabs = new List<GameObject>();

    #endregion

    void Awake()
    {
        m_instance = this;

        GenerateSeed();
    }

    void Start()
    {
        m_prefabList = Resources.LoadAll<GameObject>("Prefabs/Rooms").ToList();

        foreach(GameObject prefab in m_prefabList)
        {
            RoomInfo locations = prefab.GetComponent<RoomInfo>();

            foreach(DirectionalPosition dirLocal in locations.RoomDirections)
            {
                switch(dirLocal.Direction)
                {
                    case RoomInfo.Direction.North:
                        if(!m_northPrefabs.Contains(prefab)) { m_northPrefabs.Add(prefab); }                        
                        break;
                    case RoomInfo.Direction.East:
                        if(!m_eastPrefabs.Contains(prefab)) { m_eastPrefabs.Add(prefab); }
                        break;
                    case RoomInfo.Direction.South:
                        if(!m_southPrefabs.Contains(prefab)) { m_southPrefabs.Add(prefab); }
                        break;
                    case RoomInfo.Direction.West:
                        if(!m_westPrefabs.Contains(prefab)) { m_westPrefabs.Add(prefab); }
                        break;
                }
            }
        }
    }

    public void GenerateSeed()
    {
        //Generates our Level Seed
        m_levelSeed = new System.Random(System.DateTime.Now.Millisecond).Next();
        m_randomGenerator = new System.Random(m_levelSeed);
    }

    //Branchy Generation
    //public void GenerateBranchyLevel() 
    //{
    //    int bOrW = 0;

    //    //Init Floor
    //    Vector3 baseBranch = new Vector3(0, 0, 0);
    //    InitFloor(baseBranch, m_prefabList[0]/*, ref bOrW*/);

    //    int numofBranchesFromBase = m_randomGenerator.Next(2, 5);
    //    int currentBranchesFromBase = 0;

    //    List<Direction> pickedDirs = new List<Direction>();

    //    while(currentBranchesFromBase < numofBranchesFromBase)
    //    {
    //        Vector3 newPos = Vector3.zero;
    //        Direction[] avDirs = defaultDir;
    //        Direction currentDir = Direction.Up;
    //        int currentBranchSize = 0;

    //        //Remove Directions Already Gone
    //        foreach (Direction dir in pickedDirs) { avDirs = Array.FindAll(avDirs, i => i != dir).ToArray(); }

    //        //Init 2 Floors
    //        GameObject prefab = PickDirection(ref newPos, avDirs, ref currentDir);
    //        InitFloor(newPos, prefab/*, ref bOrW*/);

    //        PushDirection(ref newPos, prefab, currentDir);
    //        InitFloor(newPos, prefab/*, ref bOrW*/);

    //        currentBranchSize += 2;

    //        pickedDirs.Add(currentDir);

    //        while (currentBranchSize < m_maxBranchSize)
    //        {
    //            avDirs = Array.FindAll(defaultDir, i => i != ReverseDirection(currentDir)).ToArray();

    //            //Init 2 Floors in Directions
    //            prefab = PickDirection(ref newPos, avDirs, ref currentDir);

    //            if (newPos.x == int.MaxValue) { branchTermination++; break; }

    //            InitFloor(newPos, prefab/*, ref bOrW*/);

    //            PushDirection(ref newPos, prefab, currentDir);
    //            InitFloor(newPos, prefab/*, ref bOrW*/);
                
    //            currentBranchSize += 2;

    //            if (m_randomGenerator.Next(0, 100) > 50)
    //            {
    //                avDirs = Array.FindAll(defaultDir, i => i != ReverseDirection(currentDir)).ToArray();

    //                OffBranching(1, newPos, avDirs/*, ref bOrW*/);
    //            }
    //        }

    //        currentBranchesFromBase++;
    //    }

    //    Debug.Log("Number of Branch Terminations: " + branchTermination);
    //}

    public void GenerateLevel()
    {
        //Init Floor
        
        GameObject baseBranch = InitFloor(Vector3.zero, m_prefabList[0]/*, ref bOrW*/);

        int numofBranchesFromBase = 0;

        if (m_manualNOBFB) { numofBranchesFromBase = m_numberOfBranchesFromBase; }
        else { numofBranchesFromBase = m_randomGenerator.Next(2, 5); }

        int currentBranchesFromBase = 0;

        //List<Direction> avDirsFromBase = new List<Direction>() { Direction.Up, Direction.Right, Direction.Left, Direction.Down};
        while(currentBranchesFromBase < numofBranchesFromBase)
        {
            Vector3 newPos = Vector3.zero;
            GameObject lastPrefab = baseBranch;
            bool fromBase = true;
            int dirIndex = -1;
            int currentBranchSize = 1;

            int passes = 0;

            while(currentBranchSize <= m_maxBranchSize)
            {
                if(passes == 10) { branchTermination++; break; }

                if (NewPickDir(ref newPos, ref lastPrefab, ref fromBase)) 
                { 
                    currentBranchSize += 2;

                    if (m_randomGenerator.Next(1, 100) <= m_offBranchChance)
                    {
                        OffBranching(1, newPos, lastPrefab);
                    }
                }
                else { passes++; }  
            }

            currentBranchesFromBase++;
        }

        Debug.Log($"Number of Branch Terminations: {branchTermination}");
    }

    bool NewPickDir(ref Vector3 lastVector, ref GameObject lastPrefab, ref bool fromBase)
    {
        bool posValid = false;
        int passes = 0;

        while(!posValid)
        {
            if(passes == 100) { branchTermination++;  break; }

            Vector3 newPos = lastVector;

            int dir = m_randomGenerator.Next(0, 4);

            GameObject pickedPrefab = null;
            RoomInfo roomInfo = null;

            switch(dir)
            {
                case 0:
                    pickedPrefab = m_northPrefabs[m_randomGenerator.Next(0, m_northPrefabs.Count)];
                    roomInfo = pickedPrefab.GetComponent<RoomInfo>();

                    if(roomInfo.AvaliableDirections().Contains(RoomInfo.Direction.North))  
                    {
                        Vector3 posfromLast = lastPrefab.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.North);
                        Vector3 posFromPre = roomInfo.GetDirectionPosition(RoomInfo.Direction.North);

                        Vector3 preCalc = posfromLast + posFromPre;

                        newPos += preCalc;
                    }
                    break;
                case 1:
                    pickedPrefab = m_eastPrefabs[m_randomGenerator.Next(0, m_eastPrefabs.Count)];
                    roomInfo = pickedPrefab.GetComponent<RoomInfo>();

                    if (roomInfo.AvaliableDirections().Contains(RoomInfo.Direction.East)) 
                    {
                        Vector3 posfromLast = lastPrefab.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.East);
                        Vector3 posFromPre = roomInfo.GetDirectionPosition(RoomInfo.Direction.East);

                        Vector3 preCalc = posfromLast + posFromPre;

                        newPos += preCalc;
                    }
                    break;
                case 2:
                    pickedPrefab = m_southPrefabs[m_randomGenerator.Next(0, m_southPrefabs.Count)];
                    roomInfo = pickedPrefab.GetComponent<RoomInfo>();

                    if (roomInfo.AvaliableDirections().Contains(RoomInfo.Direction.South)) 
                    {
                        Vector3 posfromLast = lastPrefab.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.South);
                        Vector3 posFromPre = roomInfo.GetDirectionPosition(RoomInfo.Direction.South);

                        Vector3 preCalc = posfromLast + posFromPre;

                        newPos += preCalc;
                    }
                    break;
                case 3:
                    pickedPrefab = m_westPrefabs[m_randomGenerator.Next(0, m_westPrefabs.Count)];
                    roomInfo = pickedPrefab.GetComponent<RoomInfo>();

                    if (roomInfo.AvaliableDirections().Contains(RoomInfo.Direction.West)) 
                    {
                        Vector3 posfromLast = lastPrefab.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.West);
                        Vector3 posFromPre = roomInfo.GetDirectionPosition(RoomInfo.Direction.West);

                        Vector3 preCalc = posfromLast + posFromPre;

                        newPos += preCalc;
                    }
                    break;
            }

            if (!m_floorsPos.Contains(newPos))
            {
                if(!m_allowConnections)
                {
                    Vector3 tempPos = newPos;

                    PushDirection(ref tempPos, pickedPrefab, (RoomInfo.Direction)dir);

                    if (!m_floorsPos.Contains(tempPos))
                    {
                        if (fromBase)
                        {
                            lastPrefab.GetComponent<RoomInfo>().RemoveDirectionNonRev((RoomInfo.Direction)dir);
                            fromBase = false;
                        }

                        lastPrefab = InitFloor(newPos, pickedPrefab);
                        lastPrefab.GetComponent<RoomInfo>().RemoveDirection((RoomInfo.Direction)dir);
                        lastPrefab.GetComponent<RoomInfo>().DirectionComingFrom = (RoomInfo.Direction)dir;


                        GameObject pushedObj = InitFloor(tempPos, pickedPrefab);
                        pushedObj.GetComponent<RoomInfo>().RemoveDirection((RoomInfo.Direction)dir);
                        pushedObj.GetComponent<RoomInfo>().DirectionComingFrom = (RoomInfo.Direction)dir;

                        lastVector = tempPos;

                        posValid = true;

                        continue;
                    }
                }
                else
                {
                    if (fromBase)
                    {
                        lastPrefab.GetComponent<RoomInfo>().RemoveDirectionNonRev((RoomInfo.Direction)dir);
                        fromBase = false;
                    }

                    lastPrefab = InitFloor(newPos, pickedPrefab);
                    lastPrefab.GetComponent<RoomInfo>().RemoveDirection((RoomInfo.Direction)dir);
                    lastPrefab.GetComponent<RoomInfo>().DirectionComingFrom = (RoomInfo.Direction)dir;

                    lastVector = newPos;

                    posValid = true;

                    continue;
                }
            } 

            passes++;
        }

        if(passes == 100) { return false; }
        else { return true; }
    }

    public void ClearLevel()
    {
        for (int i = 0; i < m_floors.Count;)
        {
            if (Application.isPlaying) { Destroy(m_floors[i]); }
            else if (Application.isEditor) { DestroyImmediate(m_floors[i]); }
            m_floors.RemoveAt(i);
        }

        m_floorsPos.Clear();
    }

    ////Utility
    //Direction ReverseDirection(Direction directionToReverse)
    //{
    //    switch (directionToReverse)
    //    {
    //        case Direction.Up:
    //            directionToReverse = Direction.Down;
    //            break;
    //        case Direction.Right:
    //            directionToReverse = Direction.Left;
    //            break;
    //        case Direction.Down:
    //            directionToReverse = Direction.Up;
    //            break;
    //        case Direction.Left:
    //            directionToReverse = Direction.Right;
    //            break;
    //    }

    //    return directionToReverse;
    //}

    /*GameObject PickDirection(ref Vector3 lastVector, Direction[] validDirections, ref Direction outDir)
    {
        bool posValid = false;
        int passes = 0;

        while (!posValid)
        {
            Vector3 newPos = lastVector;
            int dir = m_randomGenerator.Next(0, 4);
            int randPrefab = 0;
            GameObject chosenPrefab = null;

            if (validDirections.Contains((Direction)dir))
            {
                switch (dir)
                {
                    case 0:
                        {
                            randPrefab = m_randomGenerator.Next(0, m_southPrefabs.Count);

                            foreach(DirectionalLocation dirLocation in m_southPrefabs[randPrefab].GetComponent<EntryLocations>().GetEntryLocations)
                            {
                                if(dirLocation.PositionDirection == DirectionalLocation.Direction.South)
                                {
                                    newPos += dirLocation.Position;
                                    chosenPrefab = m_southPrefabs[randPrefab];
                                    break;
                                }
                            }

                            break;
                        }
                    case 1:
                        {
                            randPrefab = m_randomGenerator.Next(0, m_westPrefabs.Count);

                            foreach (DirectionalLocation dirLocation in m_westPrefabs[randPrefab].GetComponent<EntryLocations>().GetEntryLocations)
                            {
                                if(dirLocation.PositionDirection == DirectionalLocation.Direction.West)
                                {
                                    newPos += dirLocation.Position;
                                    chosenPrefab = m_westPrefabs[randPrefab];
                                    break;
                                }
                            }

                            break;
                        }
                    case 2:
                        {
                            randPrefab = m_randomGenerator.Next(0, m_northPrefabs.Count);

                            foreach(DirectionalLocation dirLocation in m_northPrefabs[randPrefab].GetComponent<EntryLocations>().GetEntryLocations)
                            {
                                if(dirLocation.PositionDirection == DirectionalLocation.Direction.North)
                                {
                                    newPos += dirLocation.Position;
                                    chosenPrefab = m_northPrefabs[randPrefab];
                                    break;
                                }
                            }
                            break;
                        }
                    case 3:
                        {
                            randPrefab = m_randomGenerator.Next(0, m_eastPrefabs.Count);

                            foreach(DirectionalLocation dirLocation in m_eastPrefabs[randPrefab].GetComponent<EntryLocations>().GetEntryLocations)
                            {
                                if(dirLocation.PositionDirection == DirectionalLocation.Direction.East)
                                {
                                    newPos += dirLocation.Position;
                                    chosenPrefab = m_eastPrefabs[randPrefab];
                                    break;
                                }
                            }
                            break;
                        }
                }

                if (!m_floorsPos.Contains(newPos))
                {
                    if(!m_allowConnections)
                    {
                        Vector3 tempPos = newPos;

                        PushDirection(ref tempPos, chosenPrefab, (Direction)dir);

                        if (!m_floorsPos.Contains(tempPos))
                        {
                            lastVector = newPos;
                            outDir = (Direction)dir;
                            posValid = true;
                        }
                    }
                    else
                    {
                        lastVector = newPos;
                        outDir = (Direction)dir;
                        posValid = true;
                    }
                }
            }

            passes++;
            if (passes > 100)
            {
                lastVector.x = int.MaxValue;
                break;
            }

            return chosenPrefab;
        }

        return null;
    }*/

    void PushDirection(ref Vector3 pos, GameObject lastObject, RoomInfo.Direction dir)
    {
        switch(dir)
        {
            case RoomInfo.Direction.North:
                pos += (lastObject.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.North) * 2);
                break;
            case RoomInfo.Direction.East:
                pos += (lastObject.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.East) * 2);
                break;
            case RoomInfo.Direction.South:
                pos += (lastObject.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.South) * 2);
                break;
            case RoomInfo.Direction.West:
                pos += (lastObject.GetComponent<RoomInfo>().GetDirectionPosition(RoomInfo.Direction.West) * 2);
                break;
        }
    }

    GameObject InitFloor(Vector3 pos, GameObject selectedPrefab/*, ref int bOrW*/)
    {
        GameObject obj = Instantiate(selectedPrefab, pos, Quaternion.identity, m_floorParent);

        /*Color col;

        if (bOrW == 0) { col = Color.white; bOrW = 1; }
        else { col = Color.black; bOrW = 0; }

        obj.GetComponent<MeshRenderer>().material.color = col;*/

        m_floors.Add(obj);
        m_floorsPos.Add(pos);

        return obj;
    }

    void OffBranching(int currentOffBranch, Vector3 lastPos, GameObject lastPrefab)
    {
        Vector3 newPos = lastPos;
        GameObject newObj = lastPrefab;
        bool fromBase = true;

        int newBranchSize = 1;
        int passes = 0;

        while(newBranchSize <= Mathf.FloorToInt(m_maxBranchSize / currentOffBranch))
        {
            if(passes == 10) { branchTermination++; break; }

            if(NewPickDir(ref newPos, ref newObj, ref fromBase)) 
            { 
                newBranchSize += 2;

                if (m_randomGenerator.Next(1, 101) >= m_offBranchChance && m_offBranchChance != 0)
                {
                    OffBranching(currentOffBranch + 1, newPos, newObj);
                }
            }
            else { passes++; }

        }
    }

    /*void OffBranching(int currentOffBranch, Vector3 lastBranch, Direction[] avDirs*//*, ref int bOrW*//*)
    {
        Vector3 newPos = lastBranch;
        Direction currentDir = Direction.Up;

        GameObject prefab = PickDirection(ref newPos, avDirs, ref currentDir);

        if (newPos.x == int.MaxValue) { branchTermination++; return; }

        InitFloor(newPos, prefab*//*, ref bOrW*//*);

        PushDirection(ref newPos, prefab, currentDir);
        InitFloor(newPos, prefab*//*, ref bOrW*//*);
        int newBranchSize = 2;

        while (newBranchSize < Mathf.FloorToInt(m_maxBranchSize / currentOffBranch))
        {
            avDirs = Array.FindAll(defaultDir, i => i != ReverseDirection(currentDir)).ToArray();

            prefab = PickDirection(ref newPos, avDirs, ref currentDir);
            if (newPos.x == int.MaxValue) { branchTermination++; break; }

            InitFloor(newPos, prefab*//*, ref bOrW*//*);

            PushDirection(ref newPos, prefab, currentDir);
            InitFloor(newPos, prefab*//*, ref bOrW*//*);

            newBranchSize += 2;

            if (m_randomGenerator.Next(1, 100) > 50)
            {
                avDirs = Array.FindAll(defaultDir, i => i != ReverseDirection(currentDir)).ToArray();

                OffBranching(currentOffBranch + 1, newPos, avDirs*//*, ref bOrW*//*);
            }
        }
    }*/

    //2116218693
    //2017
}