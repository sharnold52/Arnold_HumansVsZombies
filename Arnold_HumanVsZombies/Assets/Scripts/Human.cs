using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Human : Vehicle
{
    // targets
    public GameObject humanTarget;
    List<GameObject> zombieChasers;

    // reference to agent manager
    AgentManager manager;

    // weights for seek and flee
    public float seekWeight = 1f;
    public float fleeWeight = 3f;
    public float avoidWeight = 4f;

    // max force
    public float maxForce = 1f;

    // distances for determining zombie fleeing
    public float acceptableDistance = 6f;

    // ultimate force for calculating steering forces
    public Vector3 ultimateForce;

    // obstacle avoidance
    float safeDistance;

    // materials for debugging
    public Material purpleLine;
    public Material blueLine;
    public Material greenLine;

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
            // if human is seeking something, go after that, else wander
            if (humanTarget != null)
            {
                ultimateForce += (Seek(humanTarget.transform.position) * seekWeight);
            }
            else
            {
                ultimateForce += Wander();
            }

            // If zombie is too close, add the Flee force to ultimate force
            // which is weighted by the flee force
            zombieChasers = manager.ClosestZombie(gameObject, acceptableDistance);
            if (zombieChasers != null)
            {
                Vector3 evadeForce = Vector3.zero;
                for (int i = 0; i < zombieChasers.Count; i++)
                {
                    //flee if zombie is too close, otherwise evade
                    float distance = Vector3.Distance(zombieChasers[i].transform.position, position);
                    float futurePosition = Vector3.Distance(zombieChasers[i].transform.position, zombieChasers[i].transform.position + zombieChasers[i].GetComponent<Zombie>().velocity);
                    if (distance > futurePosition)
                    {
                        evadeForce += Evade(zombieChasers[i]);
                    }
                    else
                    {
                        evadeForce += Flee(zombieChasers[i].transform.position);
                    }
                }
                ultimateForce += evadeForce * fleeWeight;
            }
        }
        else
        {
            ultimateForce += boundsForce;
        }

        // check for obstacles
        for(int i = 0; i < manager.allObstacles.Count; i++)
        {
            ultimateForce += AvoidObstacle(manager.allObstacles[i], safeDistance) * avoidWeight;
        }

        // separate from other humans
        ultimateForce += Separate(manager.allHumans);
        ultimateForce += Align(manager.allHumans);

        // clamp the ultimate force to Maximum force
        ultimateForce = Vector3.ClampMagnitude(ultimateForce, maxForce);

        // Apply the ultimate force to the Human's acceleration
        ApplyForce(ultimateForce);
        
        // ultimate force that's zeroed out
        ultimateForce = Vector3.zero;
    }

    // draw debug lines
    private void OnRenderObject()
    {
        if(manager.debug)
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
            purpleLine.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Vertex(position + velocity);
            GL.Vertex(position + direction);
            GL.End();
        }
    }
}
