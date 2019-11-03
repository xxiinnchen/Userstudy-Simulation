using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneProperties : MonoBehaviour
{

    public Drone classPointer;

    void Start()
    {
        //Debug.Log("DroneProperties: Start()");
        classPointer.SPEED = Utility.DRONE_SPEED;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "drone")
        {
            classPointer.TriggerCollision(other);
        }
    }
}
