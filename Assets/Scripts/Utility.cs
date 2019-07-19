﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Utility : MonoBehaviour
{
    // Unity constant
    public static float DELTATIME; // Used to calculate AvgTime for a drone trip
    public static float AVGTIME; // Average time to run Utility:Awake()
    public static bool IS_RND_TAKEOFF = true; //Used to choose random drone or next in interable upon assigning an event in TafficControl: Update()

    // game logic constant
    public const int EXIT_TIME = 6000;  // the total time that the game is running

    // drone logic constant
    public static readonly float BOUND_DIM = 1.4f, INTERACT_DIM = 2.3f, REPLAN_DIM = 2.2f;
    public static Vector3 COLLISION_BOUND = new Vector3(BOUND_DIM, BOUND_DIM, BOUND_DIM);
    public static Vector3 CUTOFF_INTERACT = new Vector3(INTERACT_DIM, INTERACT_DIM, INTERACT_DIM);
    public static Vector3 CUTOFF_REPLAN = new Vector3(REPLAN_DIM, REPLAN_DIM, REPLAN_DIM);

    public static float INTERACT_TIME = 1f;
    public static float DRONE_SPEED;

    private static Vector3 ShelfBasePos = new Vector3(26.57f, 20.41f, 1.93f); // left-bottom corner of the shelf
    private static Vector3 ParkingBasePos = new Vector3(26.51615f, 17.572f, 20.04752f);
    private static float horizonInterval = -2.26f;
    private static float verticalInterval = 1.7f;
    private static float parkingInterval = 2.4f;

    public static Vector3[] shelves = InitShelves(ShelfBasePos, horizonInterval, verticalInterval, 2, 5);
    public static Vector3[] parking = InitParkingLot(ParkingBasePos, parkingInterval, parkingInterval, 2, 5);


    public static Vector3[] InitShelves(Vector3 basePos, float horizonInterval, float verticalInterval, int numLayer, int itemPerLayer)
    {
        Vector3[] shelve = new Vector3[numLayer * itemPerLayer];
        for (int i = 0; i < numLayer; i++)
        {
            for (int j = 0; j < itemPerLayer; j++)
            {
                int curIdx = i * itemPerLayer + j;
                Vector3 curPos = new Vector3(basePos[0] + j * horizonInterval, basePos[1] + i * verticalInterval, basePos[2]);
                shelve[curIdx] = curPos;
            }
        }
        return shelve;
    }


    public static Vector3[] InitParkingLot(Vector3 basePos, float horizonInterval, float verticalInterval, int numLayer, int itemPerLayer)
    {
        Vector3[] ParkingLot = new Vector3[numLayer * itemPerLayer];
        for (int i = 0; i < numLayer; i++)
        {
            for (int j = 0; j < itemPerLayer; j++)
            {
                int curIdx = i * itemPerLayer + j;
                Vector3 curPos = new Vector3(basePos.x - j * horizonInterval, basePos.y + i * verticalInterval, basePos.z);
                ParkingLot[curIdx] = curPos;

            }
        }
        return ParkingLot;
    }


    public static bool IsLessThan(Vector3 a, Vector3 b)
    {
        return a.magnitude < b.x;
    }

    public static bool IsMoreThan(Vector3 a, Vector3 b)
    {
        return a.magnitude >= b.x;
    }

    public static float CalDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a[0] - b[0], 2.0f) + Mathf.Pow(a[1] - b[1], 2.0f) + Mathf.Pow(a[2] - b[2], 2.0f));
    }

    public static void DeleteChild(GameObject go, string name)
    {
        foreach (Transform child in go.transform)
        {
            if (child.gameObject.name == name)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }

    void Awake()
    {
        //Debug.Log("Utility: Awake()");
        DELTATIME = Time.deltaTime;
        DRONE_SPEED = (INTERACT_DIM - BOUND_DIM) / INTERACT_TIME * DELTATIME;
     
    }
}
