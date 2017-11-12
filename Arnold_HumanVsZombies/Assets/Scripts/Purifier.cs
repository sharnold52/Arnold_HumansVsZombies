using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Purifier : Vehicle
{
    // target of purifier
    public GameObject purifierTarget;

    // reference to agent manager
    AgentManager manager;

    // weights for seek and flee
    public float seekWeight = 1f;
    public float avoidWeight = 4f;

    // max force
    public float maxForce = 1f;

    // ultimate force for calculating steering forces
    public Vector3 ultimateForce;

    // obstacle avoidance
    float safeDistance;

    // Use this for initialization
    public override void Start()
    {
        base.Start();
        manager = FindObjectOfType<AgentManager>();

        // instantiate safe distance based on speed
        safeDistance = maxSpeed * 0.75f;
    }

    public override void CalcSteeringForces()
    {
        Vector3 boundsForce = CheckBounds();
        // check for bounds
        if (boundsForce == Vector3.zero)
        {
            if (purifierTarget != null)
            {
                ultimateForce += (Pursue(purifierTarget) * seekWeight);
            }
            else
            {
                ultimateForce += Wander();
            }
        }
        else
        {
            ultimateForce += boundsForce;
        }

        // avoid obstacles
        for (int i = 0; i < manager.allObstacles.Count; i++)
        {
            ultimateForce += AvoidObstacle(manager.allObstacles[i], safeDistance) * avoidWeight;
        }

        // clamp the ultimate force to Maximum force
        ultimateForce = Vector3.ClampMagnitude(ultimateForce, maxForce);

        // Apply the ultimate force to the Human's acceleration
        ApplyForce(ultimateForce);

        // ultimate force that's zeroed out
        ultimateForce = Vector3.zero;
    }
    
}
