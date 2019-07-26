#define IS_USER_STUDY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

using UnityEngine.SceneManagement;


public class TrafficControl : MonoBehaviour
{
    public static string seed_filename = "Assets/Scripts/SEED.txt";
    public static StreamReader reader = new StreamReader(seed_filename);
    static string seed_string = reader.ReadLine();
    public static int SEED = strToInt(seed_string);

    public static GameObject worldobject;
    

    public GameObject droneBaseObject;
    public GameObject eventBaseObject;
   
    public int numDrones = 10;
    public int EVENT_INTERVAL = 1;
    public int EXIT_TIME = 180;
    public int MAX_SEED;

    // Drone and Event Dictionaries 
    public static Dictionary<int, Drone> dronesDict = new Dictionary<int, Drone>();
    public static Dictionary<int, Event> eventsDict = new Dictionary<int, Event>();

    public OrderedSet<int> waitingEventsId = new OrderedSet<int>();
    public HashSet<int> ongoingEventsId = new HashSet<int>();

    public OrderedSet<int> availableDronesId = new OrderedSet<int>();
    public HashSet<int> workingDronesId = new HashSet<int>();

    public static Vector3[] shelves = Utility.shelves;
    public static Vector3[] parkinglot = Utility.parking;


    // User Data variables

    public int systemError = 0;
    public int userError = 0;
    //private int timeCounter = 0;
    private float timeCounter = 0;
    private float eventTimer = 0;
    private float lastPrint = 0;
    private int cleanCounter = 0;
    private int successEventCounter = 0;
    private int totalEventCounter = 0;

    // Functional Variables
    private float AVE_TIME;
    private System.Random rnd;

    public static int strToInt(string str)
    {
        int numVal= -1;
        try
        {
            numVal = Int32.Parse(str);
        }
        catch (FormatException e)
        {
            Debug.Log("Invalid Seed file." + e);
        }
        Debug.Log("Retreiving Seed from SEED.txt: " + numVal);
        reader.Close();
    
        return numVal;
    }

    public int GenRandEvent()
    {
        int idx = -1;
        while (idx == -1)
        {
            idx = rnd.Next(shelves.Length);
            idx = waitingEventsId.Contains(idx) ? -1 : idx;
        }

        return idx;
    }

    public void initDrones(int num)
    {
        for (int i = 0; i < num; i++)
        {
            Drone newDrone = new Drone(i, parkinglot[i]);
            dronesDict.Add(i, newDrone);
            availableDronesId.Add(i);
        } 
    }

    public void initEvent(int num)
    {
        for (int i = 0; i < num; i++)
        {
            Event newEvent = new Event(i, shelves[i]);
            eventsDict.Add(i, newEvent);
        }
    }


    // Use this for initialization
    void Start()
    {
        AVE_TIME = Utility.AVGTIME;
        Debug.Log(SEED);
        rnd = new System.Random(SEED);
        waitingEventsId.rnd = new System.Random(SEED);
        availableDronesId.rnd = new System.Random(SEED);

        worldobject = this.gameObject;
        dronesDict = new Dictionary<int, Drone>();
        initDrones(numDrones);
        initEvent(shelves.Length);
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (timeCounter - lastPrint > 1)
        {
            lastPrint = timeCounter;
        }

        //Debug.LogFormat("{0} {1}", eventTimer, EVENT_INTERVAL);
        // Check if to generate new random event 
        if (eventTimer > EVENT_INTERVAL)
        {
            eventTimer = 0;
            //Debug.Log("New Event Attempt");

            if (waitingEventsId.Count + ongoingEventsId.Count < shelves.Length - 1)
            {
              //  Debug.Log("New Event Created");

                int newIdx = GenRandEvent();
                //Event newWaitingEvent = eventsDict[newIdx];
                Debug.Log("Adding to event" + newIdx);
                waitingEventsId.Add(newIdx);

                totalEventCounter++;
            }
        }

        if (availableDronesId.Count > 0 && waitingEventsId.Count > 0)
        {
            //Debug.Log("Assigning event to drone");

            int e = waitingEventsId.Next();
            int d = Utility.IS_RND_TAKEOFF ? availableDronesId.NextRnd() : availableDronesId.Next();
         

            Drone avalibleDrone = dronesDict[d];

            //if (avalibleDrone.takeoff_timer >= avalibleDrone.takeoff_interval)
            //{
            //    avalibleDrone.takeoff_timer = 0;
            //    avalibleDrone.AddEvent(eventsDict[e]);
            //    availableDronesId.Remove(d);
            //    workingDronesId.Add(d);
            //    waitingEventsId.Remove(e);
            //    ongoingEventsId.Add(e);
            //}
            //else {
            //    avalibleDrone.takeoff_timer += Time.fixedDeltaTime;
            //}
            avalibleDrone.AddEvent(eventsDict[e]);
            availableDronesId.Remove(d);
            workingDronesId.Add(d);
            waitingEventsId.Remove(e);
            ongoingEventsId.Add(e);


        }

        // apply force meanwhile check collision 
        foreach (int i in workingDronesId)
        {
            //Debug.Log("Drone Collision Loop 1");
            foreach (int j in workingDronesId)
            {
                // <Changed>
                if (i == j)
                {
                    continue;
                }
                //Debug.Log("Drone Collision Loop 2");
                Vector3 delta = dronesDict[i].gameObjectPointer.transform.Find("pCube2").gameObject.transform.position - dronesDict[j].gameObjectPointer.transform.Find("pCube2").gameObject.transform.position;
                float dis = delta.magnitude;

                //if (Utility.IsLessThan(delta, Utility.CUTOFF_INTERACT))
                if (dis < Utility.INTERACT_DIM)
                {
                    //if (Utility.IsLessThan(delta, Utility.COLLISION_BOUND))
                    if (dis < Utility.BOUND_DIM)
                    {
                        //Debug.Log("2. DroneID " + dronesDict[i].droneId + " Drone Collision");

                        userError++;
                        dronesDict[i].status = Drone.DroneStatus.COLLIDE;
                        dronesDict[j].status = Drone.DroneStatus.COLLIDE;
                    }
                    else
                    {
                        systemError++;
                    }
                }
                // </Changed>

            }
            // update direction
            dronesDict[i].direction = Vector3.Normalize(dronesDict[i].dstPos - dronesDict[i].curPos);
        }

        // check status
        // move every working drone
        for (int i = 0; i < numDrones; i++)
        {
            Drone currDrone = dronesDict[i];
            Drone.DroneStatus status = currDrone.status;

            if (status == Drone.DroneStatus.PARKED)
            {
                continue;
            }
            else if (status == Drone.DroneStatus.COLLIDE)
            {
                // Repark drone
                currDrone.curPos = currDrone.parkingPos;
                // check the event can be added back
                int eid = currDrone.eventId;
                waitingEventsId.Add(eid);

                //Event curEvent = eventsDict[currDrone.eventId];

                //NEW
                if (currDrone.repark_timer >= currDrone.repark_interval)
                {
                    currDrone.repark_timer = 0;
                    currDrone.status = Drone.DroneStatus.PARKED;
                } else
                {
                    currDrone.repark_timer += Time.fixedDeltaTime;
                }
                //NEW

                //currDrone.status = Drone.DroneStatus.PARKED;
            }

            Drone.MoveStatus moveStatus = currDrone.Move();


            if (moveStatus == Drone.MoveStatus.END_TO_SHELF)  // drone status 2 --> 3
            {
                //Debug.Log("0. DroneID " + dronesDict[i].droneId + " One -way trip success");
                //Event curEvent = eventsDict[dronesDict[i].eventId];
                ongoingEventsId.Remove(currDrone.eventId);
                waitingEventsId.Remove(currDrone.eventId);
            }
            else if (moveStatus == Drone.MoveStatus.END_WHOLE_TRIP)  // end of whole trip
            {
                //Debug.Log("1. DroneID " + currDrone.droneId + " Two-way trip success");
                ongoingEventsId.Remove(currDrone.eventId);
                waitingEventsId.Remove(currDrone.eventId);
                successEventCounter++;
            }

            if (currDrone.status == Drone.DroneStatus.PARKED)
            {
                workingDronesId.Remove(i);
                availableDronesId.Add(i);
                ongoingEventsId.Remove(currDrone.eventId);
            }
        }
        // update counter
        timeCounter += Time.fixedDeltaTime;
        eventTimer += Time.fixedDeltaTime;
        cleanCounter++;


#if IS_USER_STUDY
        if (SEED <= MAX_SEED)
        {
            if (timeCounter >= EXIT_TIME)
            {
                //Debug.Log("====================End of a 3 minute user study=============================");
                //Debug.Log(SEED);
                ResetSim();
                
                ReloadScene();
                

                //QuitGame();
            }
        } else
        {
            Debug.Log("???????????????????????????");
            QuitGame();
        }
#endif
    }


    void UpdateSeed()
    {
        StreamWriter writer = new StreamWriter(seed_filename, false);
        int newSeed = SEED + 1;
        writer.WriteLine(newSeed.ToString());

        writer.Close();
    }


    void ReloadScene()
    {
        UpdateSeed();

        

        seed_filename = "Assets/Scripts/SEED.txt";
        reader = new StreamReader(seed_filename);
        seed_string = reader.ReadLine();
        SEED = strToInt(seed_string);


        // Drone and Event Dictionaries 
        dronesDict = new Dictionary<int, Drone>();
        eventsDict = new Dictionary<int, Event>();

        waitingEventsId = new OrderedSet<int>();
        ongoingEventsId = new HashSet<int>();

        availableDronesId = new OrderedSet<int>();
        workingDronesId = new HashSet<int>();

        shelves = Utility.shelves;
        parkinglot = Utility.parking;


        // User Data variables

        systemError = 0;
        userError = 0;
        timeCounter = 0;
        eventTimer = 0;
        cleanCounter = 0;
        successEventCounter = 0;
        totalEventCounter = 0;

        //rnd = new System.Random(SEED);

        //UnityEditor.PrefabUtility.ResetToPrefabState(this.gameObject);
        UnityEditor.PrefabUtility.RevertObjectOverride(gameObject, UnityEditor.InteractionMode.AutomatedAction);

        SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
    }


    void ResetSim()
    {
        float successRate = successEventCounter / numDrones;

        string filename = "Assets/Log/10/" + numDrones + "_test1.txt";
        string filename_success = "Assets/Log/10/" + numDrones + "_Success1.txt";
        // write to log file
        StreamWriter fileWriter = new StreamWriter(filename, true);
        StreamWriter fileWriter_success = new StreamWriter(filename_success, true);

        fileWriter.WriteLine("CURRENT TIME: " + System.DateTime.Now);
        fileWriter.WriteLine("==========Basic Parameters==========");
        fileWriter.WriteLine("Interface " + SceneManager.GetActiveScene().name);
        fileWriter.WriteLine("FPS: " + 1 / Time.deltaTime);
        fileWriter.WriteLine("Drone speed: " + Utility.DRONE_SPEED);
        //fileWriter.WriteLine("Seed: " + SEED);
        fileWriter.WriteLine("Number of drones: " + numDrones);
        fileWriter.WriteLine("Average time: " + AVE_TIME);
        fileWriter.WriteLine("Event interval: " + EVENT_INTERVAL);
        fileWriter.WriteLine("Number of events: " + totalEventCounter);

        fileWriter.WriteLine("==========User Study Data==========");
        fileWriter.WriteLine("Seed: " + SEED);
        fileWriter.WriteLine("System error: " + systemError);
        fileWriter.WriteLine("User error: " + userError / 2);
        if ((userError / 2) == 18)
        {
            fileWriter_success.WriteLine("==========User Study Data==========");
            fileWriter_success.WriteLine("Number of drones: " + numDrones);
            fileWriter_success.WriteLine("Seed: " + SEED);
            fileWriter_success.WriteLine("System error: " + systemError);
            fileWriter_success.WriteLine("User error: " + userError / 2);
        }
        fileWriter.WriteLine("Number success events: " + successEventCounter);
        fileWriter.WriteLine(" ");

        fileWriter.Close();
    }



    void OnApplicationQuit()
    {
        float successRate = successEventCounter / numDrones;
        
        string filename = "Assets/Log/" + SceneManager.GetActiveScene().name + "_" + numDrones + "test4.txt"; 
        // write to log file
        StreamWriter fileWriter = new StreamWriter(filename, true);

        fileWriter.WriteLine("CURRENT TIME: " + System.DateTime.Now);
        fileWriter.WriteLine("==========Basic Parameters==========");
        fileWriter.WriteLine("Interface " + SceneManager.GetActiveScene().name);
        fileWriter.WriteLine("FPS: " + 1 / Time.deltaTime);
        fileWriter.WriteLine("Drone speed: " + Utility.DRONE_SPEED);
        //fileWriter.WriteLine("Seed: " + SEED);
        fileWriter.WriteLine("Number of drones: " + numDrones);
        fileWriter.WriteLine("Average time: " + AVE_TIME);
        fileWriter.WriteLine("Event interval: " + EVENT_INTERVAL);
        fileWriter.WriteLine("Number of events: " + totalEventCounter);

        fileWriter.WriteLine("==========User Study Data==========");
        fileWriter.WriteLine("Seed: " + SEED);
        fileWriter.WriteLine("System error: " + systemError);
        fileWriter.WriteLine("User error: " + userError / 2);
        fileWriter.WriteLine("Number success events: " + successEventCounter);
        fileWriter.WriteLine(" ");

        fileWriter.Close();


    }

    public void QuitGame()
    {
        // save any game data here
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }
}
