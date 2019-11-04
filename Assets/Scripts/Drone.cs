﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using TMPro;

public class Drone {

    // Drone Position Variables 
    public Vector3 parkingPos;
    public Vector3 hoverPos;
    public Vector3 eventPos;
    public Vector3 spawnPos;
    public Vector3 curPos; //Current position of the drone
    public Vector3 dstPos; //current destination position
    public Vector3 direction; // Direction for drone to fly in
    public Vector3 epsilon = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 hoverShift = new Vector3(0f, 1.5f, 0f);


    // Drone properties
    public GameObject gameObjectPointer;
    public float SPEED;
    public int droneId;
    public int eventId;
    public int eventNo;
    public int collionDroneId = -2;
    public int nextEvent = -2;
    public float tripTime = 0;
    public bool safe = false;
    public float collidesAtTime;

    ////NEW
    //public static System.Random repark_rnd = new System.Random(1024);
    //public int repark_interval = repark_rnd.Next(0, 5);
    //public float repark_timer = 0;
    ////NEW

    public int pauseCounter;
    public enum DroneStatus
    {
        PARKED = 0,
        TAKEOFF = 1,
        TO_SHELF = 2,
        DELAY = 3,
        //COLLIDE = 3
    }
    public DroneStatus status; // 0: parked, 1: takeoff, 2: to shelf, 3: collide
    public bool isCollided;

    public Drone(int droneId, Vector3 initPos)
    {
        
        this.droneId = droneId;
        this.parkingPos = initPos;
        this.curPos = initPos;
        //this.hoverPos = this.parkingPos + hoverShift;
        this.status = DroneStatus.PARKED;
        this.isCollided = false;

        // create game object
        GameObject baseObject = TrafficControl.worldobject.GetComponent<TrafficControl>().droneBaseObject;
        gameObjectPointer = UnityEngine.Object.Instantiate(baseObject, initPos, Quaternion.identity);
        gameObjectPointer.GetComponent<DroneProperties>().classPointer = this;

        gameObjectPointer.name = string.Concat("Drone", droneId.ToString());
        gameObjectPointer.layer = 2;
        gameObjectPointer.transform.parent = TrafficControl.worldobject.transform;

        try
        {
            GameObject textHelperChild = this.gameObjectPointer.transform.Find("Text Helper").gameObject;
            TextMeshPro textHelper = textHelperChild.GetComponent<TextMeshPro>();
            textHelper.SetText(this.droneId.ToString());
        }
        catch (NullReferenceException e)
        {
            Debug.Log(e);
            Debug.Log("Shit");
        }

    }


    public void AddEvent(Event e)
    {
        //Debug.LogFormat("Drone {0} takeoff at position {1}", droneId, parkingPos);
        //Debug.Log("Drone " + droneId + " Event " + e.shelfId + " at " + e.pos);
        status = DroneStatus.TAKEOFF;
        this.hoverPos = this.parkingPos + hoverShift;
        dstPos = hoverPos; 
        eventPos = e.pos;
        direction = Vector3.Normalize(dstPos - parkingPos);
        eventId = e.shelfId;
    }

    /// <summary>
    /// Update the status of drone and Move
    /// </summary>
    /// <returns> 
    /// drone paused: 0; 
    /// end of to_shelf trip: 1;
    /// end of whole trip: 2;
    /// otherwise: -1 
    /// </returns>
    public enum MoveStatus
    {
        PAUSED = 0,
        END_TO_SHELF = 1,
        OTHER = -1
    }


    public MoveStatus Move(){
        MoveStatus flag = MoveStatus.OTHER;

        //Debug.Log("Drone " + droneId.ToString() + " Speed " + SPEED.ToString());

        curPos = (status == DroneStatus.PARKED) ? curPos : curPos + direction * SPEED;

        // if drone is moving(drone not parked)
        // and drone reached the current destination
        if (status != DroneStatus.PARKED && Utility.IsLessThan(curPos - dstPos, epsilon))
        {
            if (Utility.IsLessThan(dstPos - hoverPos, epsilon)) // drone has finished takeoff
            {
                if (status == DroneStatus.TAKEOFF)  // end of takeoff
                {
                    status = DroneStatus.TO_SHELF;
                    dstPos = eventPos;
                    hoverPos = parkingPos + hoverShift;
                }
            }

            else if (Utility.IsLessThan(dstPos - eventPos, epsilon)) // Drone reached the event
            {
                status = DroneStatus.PARKED;
                parkingPos = spawnPos;
                curPos = parkingPos;
                hoverPos = parkingPos + hoverShift;
                flag = MoveStatus.END_TO_SHELF;


                GameObject textHelperChild = this.gameObjectPointer.transform.Find("Text Helper").gameObject;
                TextMeshPro textHelper = textHelperChild.GetComponent<TextMeshPro>();
                textHelper.color = Color.white;
            }
        }
        gameObjectPointer.transform.position = curPos;

        return flag;
    }

    public void TriggerCollision(Collider other)
    {


        try
        {
            GameObject droneB_gameObject = GameObject.Find(other.gameObject.name);
            DroneProperties droneB_droneProperties = droneB_gameObject.GetComponent<DroneProperties>();
            Drone droneB = droneB_droneProperties.classPointer;

            if (droneB.droneId == collionDroneId)
            {
                GameObject droneA_textHelperChild = this.gameObjectPointer.transform.Find("Text Helper").gameObject;
                TextMeshPro droneA_textHelper = droneA_textHelperChild.GetComponent<TextMeshPro>();
                droneA_textHelper.color = Color.red;
                //Debug.LogFormat("Collision for {0} and {1}", droneId, droneB.droneId);
                TrafficControl.worldobject.GetComponent<TrafficControl>().userErrorColliders+= 0.5f;
            }
        }
        catch (NullReferenceException)
        {
            Debug.LogFormat("Unable to find {0}", other.name);
        }

    }

}
