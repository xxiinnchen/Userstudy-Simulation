﻿#define IS_USER_STUDY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using TMPro;


using UnityEngine.SceneManagement;


public class TrafficControl : MonoBehaviour
{

    public bool FlightPathProvided = false;
    public bool FlightDebugTrip = false;
    public bool FlightDebugCol = false;
    public bool FlightPlanDebug = false;
    public bool ForceFollowCSV = false;

    public static string seed_filename = "Assets/Scripts/SEED.txt";
    public static StreamReader reader = new StreamReader(seed_filename);
    static string seed_string = reader.ReadLine();
    public static int SEED = strToInt(seed_string);
    public static bool use_seedfind = false;

    public static string csv_filename = "Assets/flightplan18_0.csv";
    public static StreamReader csv_reader = new StreamReader(csv_filename);
    public static List<List<int>> flightPlan = new List<List<int>>();
    public static int flightPlanIndex = 0;
    

    public static GameObject worldobject;
    

    public GameObject droneBaseObject;
    public GameObject eventBaseObject;

    public static int numDrones = 10;
    float EVENT_INTERVAL = Utility.EVENT_INTERVALS[numDrones];
    public int EXIT_TIME = 180;
    public float timeCounter = 0;
    public int MAX_SEED;

    // Drone and Event Dictionaries 
    public static Dictionary<int, Drone> dronesDict = new Dictionary<int, Drone>();
    public static Dictionary<int, Event> eventsDict = new Dictionary<int, Event>();
    public static Dictionary<int, float> eventColTimeDict = new Dictionary<int, float>();

    public OrderedSet<int> waitingEventsId = new OrderedSet<int>();
    public OrderedSet<List<int>> waitingEventsID_Flightplan = new OrderedSet<List<int>>();
    public HashSet<int> ongoingEventsId = new HashSet<int>();

    public OrderedSet<int> availableDronesId = new OrderedSet<int>();
    public HashSet<int> workingDronesId = new HashSet<int>();

    public static Vector3[] shelves = Utility.shelves;
    public static Vector3[] parkinglot = Utility.parking;


    // User Data variables
    public int systemError = 0;
    public int userError = 0;
    //public float timeCounter = 0;
    private float eventTimer = 0;
    private float lastPrint = 0;
    private int successEventCounter = 0;
    private int totalEventCounter = 0;
    private int flyingDroneCount = 0;
    private int each_trip_counter = 0;

    private float minuteCounter = 0; // time elapsed in that minute
    private int currMinCollisionCounter = 0; // number of collisions in the current minute
    

    // Functional Variables
    private float AVE_TIME;
    private System.Random rnd;

    public void ReadCSV()
    {
        string header = csv_reader.ReadLine();

        while (!csv_reader.EndOfStream)
        {
            string line = csv_reader.ReadLine();
            string[] values = line.Split(',');
           

            if(!String.IsNullOrEmpty(line))
            {
                //Debug.Log(line);
                List<int> values_int = new List<int>();

                foreach (string value in values)
                {
                    try {
                        int value_int = strToInt(value);
                        values_int.Add(value_int);
                    }
                    catch (FormatException e)
                    {
                        values_int.Add(-3);
                        continue;
                    }
                   
                    
                }
                flightPlan.Add(values_int);
            }
                
        }
    }
    

    public static float strToFloat(string str)
    {
        float numVal = -1;
        try
        {
            if (str=="none")
            {
                return -1;
            }

            numVal = float.Parse(str);
        }
        catch (FormatException e)
        {
            Debug.Log("Invalid Seed file." + e);
        }
        reader.Close();

        return numVal;
    }

    private List<string[]> rowData = new List<string[]>();

    public static int strToInt(string str)
    {
        int numVal= -1;
        try
        {
            if (str == "none")
            {
                return -1;
            }

            numVal = Int32.Parse(str);
        }
        catch (FormatException e)
        {
            Debug.Log("Invalid Seed file." + e);
        }
        reader.Close();
    
        return numVal;
    }


    public void printEvents(Drone availableDrone)
    {
        int tempStartingIndex = Array.IndexOf(Utility.parking, availableDrone.parkingPos);
        int tempTeleportIndex = Array.IndexOf(Utility.parking, availableDrone.spawnPos);
        int tempCurrEventNo = availableDrone.eventNo;
        int tempNextEvent = availableDrone.nextEvent;
        int tempCurrEventID = availableDrone.eventId;

        Debug.LogFormat("{0} : Drone {1} starting from {2} fly to {3} Spawning at {4} Next event {5}", tempCurrEventNo, availableDrone.droneId, tempStartingIndex, tempCurrEventID, tempTeleportIndex, tempNextEvent);
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

    //new init
    /// <summary>
    /// Initilize new drones with their positions given in flightplan.csv
    /// </summary>
    /// <param name="num"></param>
    public void initDronesWithPath(int num)
    {
        for (int i = 0; i < num; i++)
        {
            int droneInitIndex = flightPlan[i][2] + flightPlan[i][3] * 10;
            Drone newDrone = new Drone(i, parkinglot[droneInitIndex]);
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



    /// <summary>
    /// Check if the distance between line 1 given by two points and line 2 given by another set of points is less than some bound
    /// </summary>
    /// <param name="p1"></param> Point of line 1
    /// <param name="p2"></param> Another point of line 1
    /// <param name="p3"></param> Point of line 2
    /// <param name="p4"></param> Another point of line 2
    /// <returns></returns>
    public bool IsWithinCollisionBound(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, double bound)
    {
        Vector3 v1, v2, w;
        v1 = p2 - p1;
        v2 = p4 - p3;
        w = p4 - p1;

        Vector4 v1p, v2p, wp, identity;
        v1p = new Vector4(v1.x, v1.y, v1.z, 0);
        v2p = new Vector4(v2.x, v2.y, v2.z, 0);
        wp = new Vector4(w.x, w.y, w.z, 0);
        identity = new Vector4(0, 0, 0, 1);

        Matrix4x4 matrix_denominator = new Matrix4x4(v1p, v2p, wp, identity);
        double det = matrix_denominator.determinant;

        double nominator = Vector3.Cross(v1, v2).magnitude;

        double dist = det / nominator;

        if (dist < bound)
        {
            return true;
        } else
        {
            return false;
        }    
    }


    // Use this for initialization
    void Start()
    {
        Debug.Log("Retreiving Seed from SEED.txt: " + SEED);
        ReadCSV();
        AVE_TIME = Utility.AVGTIME;
        rnd = new System.Random(SEED);
        waitingEventsId.rnd = new System.Random(SEED);
        availableDronesId.rnd = new System.Random(SEED);

        worldobject = this.gameObject;
        dronesDict = new Dictionary<int, Drone>();

        if (FlightPathProvided)
        {
            initDronesWithPath(numDrones);
        } else
        {

            initDrones(numDrones);
        }
        
        initEvent(shelves.Length);

        //csv file header:
        /*
        string[] csvHeaderRow = new string[7];
        csvHeaderRow[0] = "Event No A";
        csvHeaderRow[1] = "Drone A"  ;
        csvHeaderRow[2] = "Event A"  ;
        csvHeaderRow[3] = "Event No B";
        csvHeaderRow[4] = "Drone B"  ;
        csvHeaderRow[5] = "Event B"  ;
        csvHeaderRow[6] = "Time"     ;

        rowData.Add(csvHeaderRow);
        */
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

            if ( (waitingEventsId.Count + ongoingEventsId.Count < shelves.Length - 1) || (waitingEventsID_Flightplan.Count + ongoingEventsId.Count < shelves.Length - 1) )
            {
                //  Debug.Log("New Event Created");

                if (FlightPathProvided)
                {
                    try
                    {
                        //Debug.Log("Check 1");
                        List<int> currentRow = flightPlan[flightPlanIndex];
                        List<int> temp_values = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 ,0 ,0, 0, 0, 0, 0 ,0 };


                        //Debug.Log("Check 2");
                        temp_values[0] = currentRow[0]; //eventID
                        temp_values[1] = currentRow[1]; //droneID
                        temp_values[2] = currentRow[2] + 10 * currentRow[3];        //startingLaunchpadIndexX + startingLaunchpadIndexY * 10
                        temp_values[3] = currentRow[4] + 10 * currentRow[5];          // shelfIndexX + shelfIndexY*10
                        temp_values[4] = currentRow[6];                             // followedBy
                        temp_values[5] = currentRow[7] + 10 * currentRow[8];        // teleportToLaunchPadIndexX + teleportToLaunchPadIndexY*10
                        temp_values[6] = currentRow[9];  //collideswithNum

                        //Debug.Log(currentRow[11]);
                        //eventColTimeDict.Add(currentRow[0], currentRow[11]);

                        //Debug.Log("Check 3");
                        //Debug.Log("Added Event: " + temp_values[0].ToString() + ":" + temp_values[1].ToString() + ":" + temp_values[2].ToString());
                        waitingEventsID_Flightplan.Add(temp_values);

                        flightPlanIndex++;
                        
                    }
                    catch (ArgumentOutOfRangeException err)
                    {
                        Debug.Log("### 1.INVALID EVENT." + err);
                        Debug.Log("flightPlanIndex" + flightPlanIndex.ToString());
                        Debug.Log("flightPlan" + flightPlan.Count.ToString());

                    }
                }
                else
                {
                    int newIdx = GenRandEvent();
                    waitingEventsId.Add(newIdx);
                }
            }
        }

        if (availableDronesId.Count > 0 && (waitingEventsId.Count > 0 || waitingEventsID_Flightplan.Count > 0 ))
        {

            if (FlightPathProvided)
            {
                try
                {
                    //new
                    List<int> e = waitingEventsID_Flightplan.Next();
                    int eventId = e[0];
                    int droneId = e[1];
                    int startingLaunchpadId = e[2];
                    int shelfId = e[3];
                    int nextEventId = e[4];
                    int teleportId = e[5];
                    int collisionDroneId = e[6];
                    bool droneFound = false;
                    //new
                

                    //Debug.Log("Check 4");

                    foreach (Drone availableDrone in dronesDict.Values)
                    {
                        int d = availableDrone.droneId;

                        if (availableDrone.nextEvent == totalEventCounter && availableDronesId.Contains(d))
                        {

                            availableDrone.eventId = shelfId;

                            availableDrone.AddEvent(eventsDict[shelfId]);
                            availableDrone.eventNo = totalEventCounter;
                            availableDrone.nextEvent = nextEventId;
                            availableDrone.collionDroneId = collisionDroneId;

                            if (teleportId>0) 
                                 availableDrone.spawnPos = Utility.parking[teleportId];

                            availableDronesId.Remove(d);
                            workingDronesId.Add(d);
                            waitingEventsID_Flightplan.Remove(e);
                            ongoingEventsId.Add(eventId); 

                            totalEventCounter++;

                            droneFound = true;

                            if (FlightPlanDebug)
                            {
                                printEvents(availableDrone);
                            }
                        }

                        if (droneFound)
                        {
                            break;
                        }
                    }


                    // For the first 30 events.

                    if (!droneFound && totalEventCounter<numDrones)
                    {

                        int d = availableDronesId.Next();
                        Drone availableDrone = dronesDict[d];

                        availableDrone.AddEvent(eventsDict[shelfId]);
                        availableDrone.eventNo = totalEventCounter;
                        availableDrone.nextEvent = nextEventId;
                        availableDrone.spawnPos = Utility.parking[teleportId];
                        availableDrone.collionDroneId = collisionDroneId;

                        availableDronesId.Remove(d);
                        workingDronesId.Add(d);
                        waitingEventsID_Flightplan.Remove(e);
                        ongoingEventsId.Add(eventId);

                        totalEventCounter++;

                        if (FlightPlanDebug)
                        {
                            printEvents(availableDrone);
                        }

                    }
                }
                catch (ArgumentOutOfRangeException err)
                {
                    Debug.Log("### 2.INVALID EVENT." + err);
                }

                //totalEventCounter++;

            }
            else
            {
                int e = waitingEventsId.Next();
                int d = Utility.IS_RND_TAKEOFF ? availableDronesId.NextRnd() : availableDronesId.Next();

                Drone availableDrone = dronesDict[d];

                availableDrone.AddEvent(eventsDict[e]);
                availableDrone.eventNo = totalEventCounter;
                availableDronesId.Remove(d);
                workingDronesId.Add(d);
                waitingEventsId.Remove(e);
                ongoingEventsId.Add(e);
                totalEventCounter++;
            }
        
        }

        // apply force meanwhile check collision 
        foreach (int i in workingDronesId)
        {
            Drone droneA = dronesDict[i];
            droneA.direction = Vector3.Normalize(droneA.dstPos - droneA.curPos);


            //Debug.Log(droneA.collionDroneId);

            if (ForceFollowCSV)
            {
                if (droneA.collionDroneId == -2)
                {
                    continue;
                }

                Drone droneB = dronesDict[droneA.collionDroneId];

                Vector3 delta = droneA.gameObjectPointer.transform.Find("pCube2").gameObject.transform.position - droneB.gameObjectPointer.transform.Find("pCube2").gameObject.transform.position;
                float dis = delta.magnitude;


                if (dis < 10.0f)
                {
                    //Debug.Log("2. DroneID " + droneA.droneId + " Drone Collision");
                    if (!droneA.isCollided && !droneB.isCollided)
                    {
                        //droneA.gameObjectPointer.SetActive(false);
                        //droneB.gameObjectPointer.SetActive(false);
                        userError++;

                        GameObject droneA_textHelperChild = droneA.gameObjectPointer.transform.Find("Text Helper").gameObject;
                        TextMeshPro droneA_textHelper = droneA_textHelperChild.GetComponent<TextMeshPro>();
                        droneA_textHelper.color = Color.red;

                        GameObject droneB_textHelperChild = droneB.gameObjectPointer.transform.Find("Text Helper").gameObject;
                        TextMeshPro droneB_textHelper = droneB_textHelperChild.GetComponent<TextMeshPro>();
                        droneB_textHelper.color = Color.red;

                        if (FlightDebugCol)
                        {
                            Debug.LogFormat("===== Drone {0}, Drone {1} | COLLISION  at POS {2}, {3}  | Status {4}, {5} =====", i, droneB.droneId, droneA.curPos.ToString("F2"), droneB.curPos, ToString(), droneA.status, droneB.status);
                            //Debug.LogFormat("+++++ Unity Time {0}", timeCounter); 
                        }

                        /*
                        string [] rowDataTemp = new String[7];
                        rowDataTemp[0] = droneA.eventNo.ToString();
                        rowDataTemp[1] = droneA.droneId.ToString();
                        rowDataTemp[2] = droneA.eventId.ToString();
                        rowDataTemp[3] = droneB.eventNo.ToString();
                        rowDataTemp[4] = droneB.droneId.ToString();
                        rowDataTemp[5] = droneB.eventId.ToString();
                        rowDataTemp[6] = timeCounter.ToString();                       
                        rowData.Add(rowDataTemp);
                        */
                    }

                    droneA.isCollided = true;
                    droneB.isCollided = true;
                }
                else
                {
                    systemError++;
                }
                
            }
            else
            {
                foreach (int j in workingDronesId)
                {
                    Drone droneB = dronesDict[j];

                    if (i == j)
                    {
                        continue;
                    }
                    //Debug.Log("Drone Collision Loop 2");


                    Vector3 delta = droneA.gameObjectPointer.transform.Find("pCube2").gameObject.transform.position - droneB.gameObjectPointer.transform.Find("pCube2").gameObject.transform.position;
                    float dis = delta.magnitude;

                    if (dis < Utility.INTERACT_DIM)
                    {
                        if (dis < Utility.BOUND_DIM)
                        {
                            //Debug.Log("2. DroneID " + droneA.droneId + " Drone Collision");
                            if (!droneA.isCollided && !droneB.isCollided)
                            {
                                //droneA.gameObjectPointer.SetActive(false);
                                //droneB.gameObjectPointer.SetActive(false);
                                userError++;

                                GameObject droneA_textHelperChild = droneA.gameObjectPointer.transform.Find("Text Helper").gameObject;
                                TextMeshPro droneA_textHelper = droneA_textHelperChild.GetComponent<TextMeshPro>();
                                droneA_textHelper.color = Color.red;

                                GameObject droneB_textHelperChild = droneB.gameObjectPointer.transform.Find("Text Helper").gameObject;
                                TextMeshPro droneB_textHelper = droneB_textHelperChild.GetComponent<TextMeshPro>();
                                droneB_textHelper.color = Color.red;

                                if (FlightDebugCol)
                                {
                                    Debug.LogFormat("===== Drone {0}, Drone {1} | COLLISION  at POS {2}, {3}  | Status {4}, {5} =====", i, j, droneA.curPos.ToString("F2"), droneB.curPos, ToString(), droneA.status, droneB.status);
                                }

                                /*
                                string [] rowDataTemp = new String[7];
                                rowDataTemp[0] = droneA.eventNo.ToString();
                                rowDataTemp[1] = droneA.droneId.ToString();
                                rowDataTemp[2] = droneA.eventId.ToString();
                                rowDataTemp[3] = droneB.eventNo.ToString();
                                rowDataTemp[4] = droneB.droneId.ToString();
                                rowDataTemp[5] = droneB.eventId.ToString();
                                rowDataTemp[6] = timeCounter.ToString();                       
                                rowData.Add(rowDataTemp);
                                */
                            }

                            droneA.isCollided = true;
                            droneB.isCollided = true;
                        }
                        else
                        {
                            systemError++;
                        }
                    }
                }
            }

            //Debug.Log("TEST");
            //Debug.Log(droneA.direction);
            //Debug.Log("TEST");
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

            Drone.MoveStatus moveStatus = currDrone.Move();

            if (moveStatus == Drone.MoveStatus.END_TO_SHELF)  
            {
                ongoingEventsId.Remove(currDrone.eventId);
                if (!currDrone.isCollided)
                {
                    successEventCounter++;
                    if (FlightDebugTrip)
                    {
                        Debug.LogFormat("Drone {0} | event {1} | COMPLETE, trip time: {2}", i, currDrone.eventId, currDrone.tripTime);
                    }

                } else
                {
                    if (FlightDebugTrip)
                    {
                        Debug.LogFormat("Drone {0} event {1} | CRASH, trip time: {2}", i, currDrone.eventId, currDrone.tripTime);
                    }
                }
                currDrone.tripTime = 0;
            }

            if (currDrone.status == Drone.DroneStatus.PARKED)
            {
                currDrone.isCollided = false;
                workingDronesId.Remove(i);

                if (currDrone.nextEvent != -1)
                {
                    availableDronesId.Add(i);
                }

                ongoingEventsId.Remove(currDrone.eventId);
            }
            currDrone.tripTime += Time.fixedDeltaTime;
        }
        // update counter

        timeCounter += Time.fixedDeltaTime;
        minuteCounter += Time.fixedDeltaTime;
        eventTimer += Time.fixedDeltaTime;


#if IS_USER_STUDY
        if (SEED <= MAX_SEED)
        {
            if (timeCounter >= EXIT_TIME)
            {
                Debug.Log("====================End of a 3 minute user study=============================");
                Debug.LogFormat("User Error: {0}", userError);
                //Debug.Log(SEED);
               // ResetSim();
               // ReloadScene();
                
                QuitGame();
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

        string filename = "Assets/Log/ONE-WAY/" + numDrones.ToString() + "/" + numDrones.ToString() + "_40Events.txt";
        string filename_success = "Assets/Log/ONE-WAY/" + numDrones.ToString() + "/" + numDrones.ToString() + "_40Events_success.txt";

        // write to log file
        StreamWriter fileWriter = new StreamWriter(filename, true);
        StreamWriter fileWriter_success = new StreamWriter(filename_success, true);

        fileWriter.WriteLine("CURRENT TIME: " + System.DateTime.Now);
        fileWriter.WriteLine("==========Basic Parameters==========");
        //fileWriter.WriteLine("Interface " + SceneManager.GetActiveScene().name);
        //fileWriter.WriteLine("FPS: " + 1 / Time.deltaTime);
        fileWriter.WriteLine("Drone speed: " + Utility.DRONE_SPEED);
        //fileWriter.WriteLine("Seed: " + SEED);
        fileWriter.WriteLine("Number of drones: " + numDrones);
        //fileWriter.WriteLine("Average time: " + AVE_TIME);
        fileWriter.WriteLine("Event interval: " + EVENT_INTERVAL);
        fileWriter.WriteLine("Number of events: " + totalEventCounter);

        fileWriter.WriteLine("==========User Study Data==========");
        fileWriter.WriteLine("Seed: " + SEED);
        fileWriter.WriteLine("System error: " + systemError);
        fileWriter.WriteLine("User error: " + userError);

        if ((userError) == 18)
        {
            fileWriter_success.WriteLine("==========User Study Data==========");
            fileWriter_success.WriteLine("Number of drones: " + numDrones);
            fileWriter_success.WriteLine("Seed: " + SEED);
            fileWriter_success.WriteLine("System error: " + systemError);
            fileWriter_success.WriteLine("User error: " + userError);
        }
        fileWriter.WriteLine("Number success events: " + successEventCounter);
        fileWriter.WriteLine(" ");

        fileWriter.Close();
        fileWriter_success.Close();

        //CSV file
        string[][] output = new string[rowData.Count][];

        for(int  i=0; i<output.Length; i++)
        {
            output[i] = rowData[i];
        }

        int length = output.GetLength(0);
        string delimiter = ",";

        StringBuilder sb = new StringBuilder();

        for (int index = 0; index < length; index++)
        {
            sb.AppendLine(string.Join(delimiter, output[index]));
        }

        string csv_filepath = "Assets/Log/ONE-WAY/" + numDrones.ToString() + "/" + numDrones.ToString() + "_40Events_" + seed_string + "seed_CSV.csv";

        StreamWriter outStream = System.IO.File.CreateText(csv_filepath);
        outStream.WriteLine(sb);
        outStream.Close();
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
