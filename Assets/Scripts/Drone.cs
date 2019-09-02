using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone {

    // Drone Position Variables 
    public Vector3 parkingPos;
    public Vector3 hoverPos;
    public Vector3 eventPos;
    public Vector3 curPos; //Current position of the drone
    public Vector3 dstPos; //current destination position
    public Vector3 direction; // Direction for drone to fly in
    public Vector3 epsilon = new Vector3(0.1f, 0.1f, 0.1f);
    public static Vector3 hoverShift = new Vector3(0f, 1.5f, 0f);


    // Drone properties
    public GameObject gameObjectPointer;
    public float SPEED;
    public int droneId;
    public int eventId;

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
        this.parkingPos = this.curPos = initPos;
        this.hoverPos = this.parkingPos + hoverShift;
        this.status = DroneStatus.PARKED;
        this.isCollided = false;

        // create game object
        GameObject baseObject = TrafficControl.worldobject.GetComponent<TrafficControl>().droneBaseObject;
        gameObjectPointer = Object.Instantiate(baseObject, initPos, Quaternion.identity);
        gameObjectPointer.GetComponent<DroneProperties>().classPointer = this;

        gameObjectPointer.name = string.Concat("Drone", droneId.ToString());
        gameObjectPointer.layer = 2;
        gameObjectPointer.transform.parent = TrafficControl.worldobject.transform;

    }

    public void AddEvent(Event e)
    {
        //Debug.Log("Drone " + droneId + " Event " + e.shelfId + " at " + e.pos);
        status = DroneStatus.TAKEOFF;
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
                }
            }

            else if (Utility.IsLessThan(dstPos - eventPos, epsilon)) // Drone reached the event
            //else
            {
                status = DroneStatus.PARKED;
                curPos = parkingPos;
                flag = MoveStatus.END_TO_SHELF;
            }
        }
        gameObjectPointer.transform.position = curPos;

        return flag;
    }

}
