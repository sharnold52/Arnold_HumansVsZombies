using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie : Vehicle
{
    // target
    public GameObject zombieTarget;

    // weights
    public float seekWeight;
    public float avoidWeight;

    // reference to agent manager
    AgentManager manager;

    // max force
    public float maxForce = 1f;

    // ultimate force for calculating steering forces
    public Vector3 ultimateForce;

    // obstacle avoidance
    float safeDistance;

    // materials for debugging
    public Material redLine;
    public Material blueLine;
    public Material greenLine;
    public Material blackLine;


    // Use this for initialization
    public override void Start()
    {
        base.Start();
        manager = FindObjectOfType<AgentManager>();

        // instantiate safe distance based on speed
        safeDistance = maxSpeed / 2f;
    }


    public override void CalcSteeringForces()
    {
        Vector3 boundsForce = CheckBounds();
        // check for bounds
        if (boundsForce == Vector3.zero)
        {
            // find object to seek
            zombieTarget = manager.ClosestHuman(gameObject);

            // Add the seek force to ultimate force, weighted by the seek weight
            if (manager.allHumans.Count != 0)
            {
                ultimateForce += Pursue(zombieTarget) * seekWeight;

                // also align with other nearby zombies
                ultimateForce += Separate(manager.allZombies);
                ultimateForce += Align(manager.allZombies);
            }
            else
            {
                ultimateForce += Wander();

                // also separate from other nearby zombies
                ultimateForce += Separate(manager.allZombies);
            }
        }
        else
        {
            ultimateForce += boundsForce;
        }

        // check for obstacles
        for (int i = 0; i < manager.allObstacles.Count; i++)
        {
            ultimateForce += AvoidObstacle(manager.allObstacles[i], safeDistance) * avoidWeight;
        }

        // clamp the ultimate force by the maximum force
        ultimateForce = Vector3.ClampMagnitude(ultimateForce, maxForce);

        // Apply the ultimate force to the Zombie's acceleration
        ApplyForce(ultimateForce);
        
        // ultimate force that's zeroed out
        ultimateForce = Vector3.zero;
    }

    // draw debug lines
    private void OnRenderObject()
    {
        if (manager.debug)
        {
            // set the material
            greenLine.SetPass(0);

            // draws one line
            GL.Begin(GL.LINES);
            GL.Vertex(position);
            GL.Vertex(position + direction);
            GL.End();

            // set second material
            Vector3 perp = Vector3.zero;
            perp.x = direction.z;
            perp.z = -direction.x;
            blueLine.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Vertex(position);
            GL.Vertex(position + perp);
            GL.End();

            // future position
            redLine.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Vertex(position + velocity);
            GL.Vertex(position + direction);
            GL.End();

            if (zombieTarget != null)
            {
                // draw line to target
                blackLine.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Vertex(position);
                GL.Vertex(zombieTarget.transform.position);
                GL.End();
            }
            
        }
    }
}
