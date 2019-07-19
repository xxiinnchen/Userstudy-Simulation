using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Event
{
    public int shelfId;
    public Vector3 pos;
    public GameObject gameObjectPointer;

    public Event(int shelfId, Vector3 pos)
    {
        this.shelfId = shelfId;
        this.pos = pos;
        // create game object
        GameObject baseObject = TrafficControl.worldobject.GetComponent<TrafficControl>().eventBaseObject;
        gameObjectPointer = Object.Instantiate(baseObject, pos, Quaternion.identity);
        gameObjectPointer.name = string.Concat("Event", shelfId.ToString());
        gameObjectPointer.transform.parent = TrafficControl.worldobject.transform;
    }
}

