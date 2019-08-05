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

    //NEW
    public static System.Random repark_rnd = new System.Random(1024);
    public int repark_interval = repark_rnd.Next(0, 5);
    public float repark_timer = 0;
    //NEW

    public int pauseCounter;
    public enum DroneStatus
    {
        PARKED = 0,
        TAKEOFF = 1,
        TO_SHELF = 2,
        TO_HOVER = 3,
        LAND = 4,
        COLLIDE = 5
    }
    public DroneStatus status; // 0: parked, 1: takeoff, 2: to shelf, 3: to hover, 4: land, 5: collide
    public bool isCollided;

    public Drone(int droneId, Vector3 initPos)
    {
        
        this.droneId = droneId;
        this.parkingPos = this.curPos = initPos;
        this.hoverPos = this.parkingPos + hoverShift;
        this.status = 0;

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
        END_WHOLE_TRIP = 2,
        OTHER = -1
    }


    public MoveStatus Move(){
        MoveStatus flag = MoveStatus.OTHER;

        curPos = (status == DroneStatus.PARKED) ? curPos : curPos + direction * SPEED;

        // if drone is moving and drone reached the current destination
        if (status != DroneStatus.PARKED && Utility.IsLessThan(curPos - dstPos, epsilon))
        {
            //Debug.Log(status + " " + curPos + " " + dstPos + " " + hoverPos + " " + parkingPos + " " + eventPos);

            if (Utility.IsLessThan(dstPos - hoverPos, epsilon))
            {
                if (status == DroneStatus.TAKEOFF)  // end of takeoff
                {
                    //Debug.Log("Towards Event Drone: " + droneId);
                    status = DroneStatus.TO_SHELF;
                    dstPos = eventPos;
                }
                else if (status == DroneStatus.TO_HOVER)  // end of to_hover trip
                {
                    status = DroneStatus.LAND;
                    
                    dstPos = parkingPos;
                }
            }

            else if (Utility.IsLessThan(dstPos - eventPos, epsilon))
            {
                // cur_s = 2 --> 3
                // end of to_shelf trip
                //Debug.Log("3. DroneID " + droneId + " Reached Event");
                status = DroneStatus.TO_HOVER;
                dstPos = hoverPos;
                flag = MoveStatus.END_TO_SHELF;
            }
            else
            {
                // end of whole trip
                status = DroneStatus.PARKED;
                curPos = parkingPos;
                Debug.LogFormat("Event {0} successful by drone {1}", eventId, droneId);
                flag = MoveStatus.END_WHOLE_TRIP;
            }
        }
        gameObjectPointer.transform.position = curPos;

        return flag;
    }

}
