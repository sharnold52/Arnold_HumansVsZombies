using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Vehicle : MonoBehaviour {
    // what object is it seeking or fleeing
    public GameObject target;

    // position, direction, velocity of game object
    public Vector3 position;
    public Vector3 direction;
    public Vector3 velocity;
    private Vector3 force; //aka acceleration, etc.

    // Inspector visible/adjustable
    public float deltaForce = .0001f;
    public float deltaAngle = 1f;
    public float maxSpeed = 1;
    public float drag = .95f;

    //New force to affect vehicle environment
    public float mass = 2f;
    public float coef = .2f;
    public float radius = 1f;

    // wander stuff
    public float wanderDistance = 2f;
    public float wanderRadius = 1f;
    float wanderAngle;
    float maxAngle;

    // reference to manager script
    AgentManager theManager;

    // Use this for initialization
    public virtual void Start()
    {
        radius = gameObject.GetComponent<Renderer>().bounds.extents.x;

        direction = new Vector3(0, 0, 0);
        velocity = new Vector3(0, 0, 0);
        force = new Vector3(0, 0, 0);

        theManager = FindObjectOfType<AgentManager>();

        // wander stuff
        maxAngle = 2 * Mathf.PI;
    }

    // Update is called once per frame
    public void Update()
    {
        // calculate steering forces
        CalcSteeringForces();
        UpdatePosition();

        // change game object's physical position
        gameObject.transform.position = position;

        // calculate the rotation of player image
        direction = velocity.normalized;
        gameObject.transform.rotation = Quaternion.LookRotation(direction);


        // draw debug line
        Debug.DrawLine(position, position + velocity, Color.green);
    }

    // obstacle avoidance
    public Vector3 AvoidObstacle(GameObject obstacle, float safeDistance)
    {
        // force to avoid any obstacle
        Vector3 steeringForce = Vector3.zero;

        // check if obstacle is not within the safe distance, return if true
        if ((Vector3.Distance(position, obstacle.transform.position) - radius - obstacle.GetComponent<Obstacle>().radius) > safeDistance)
        {
            return steeringForce;
        }

        // check if obstacle is behind agent, return if true
        Vector3 VtoC = obstacle.transform.position - position;
        if (Vector3.Dot(VtoC, direction) < 0)
        {
            return steeringForce;
        }

        // set up right vector
        Vector3 right = Vector3.zero;
        right.x = direction.z;
        right.z = -direction.x;
        right = right.normalized;

        // calculate length of the projection of VtoC on right using dot product
        float length = Vector3.Dot(VtoC, right);

        //check if obstacle is in the way
        if (Mathf.Abs(length) < radius + obstacle.GetComponent<Obstacle>().radius + 6)
        {
            Vector3 desiredVelocity = Vector3.zero;

            // is object on left or right?
            // right so turn left
            if (length > 0)
            {
                desiredVelocity = -right;
            }
            // left so turn right
            else
            {
                desiredVelocity = right;
            }

            // maxspeed desired velocity
            desiredVelocity = desiredVelocity.normalized;
            desiredVelocity *= maxSpeed;

            steeringForce = desiredVelocity - velocity;
        }

        return steeringForce;
    }

    // align direction movement with neighbors
    public Vector3 Align(List<GameObject> vehicles)
    {
        // neighbors to align with
        float neighborDist = 2 * radius + 4;

        Vector3 sum = Vector3.zero;
        Vector3 steeringForce = Vector3.zero;
        float count = 0;

        for (int i = 0; i < vehicles.Count; i++)
        {
            if (vehicles[i] != null)
            {
                // get distance
                float d = Vector3.Distance(position, vehicles[i].transform.position);

                // is it too close?
                if (d > 0 && d < neighborDist)
                {
                    sum += vehicles[i].GetComponent<Vehicle>().velocity;
                    count += 1f;
                }

                // calculate steering force if neighbors nearby
                if (count > 0)
                {
                    sum = sum / count;
                    sum = sum.normalized * maxSpeed;
                    steeringForce = sum - velocity;
                }
            }
        }

        return steeringForce;
    }

    // separates agent from neighbors
    public Vector3 Separate(List<GameObject> vehicles)
    {
        Vector3 sum = Vector3.zero;
        Vector3 steeringForce = Vector3.zero;
        float count = 0;

        for (int i = 0; i < vehicles.Count; i++)
        {
            // check to make sure we aren't comparing vehicle to itself
            if (gameObject != vehicles[i])
            {
                if (vehicles[i] != null)
                {
                    // get distance
                    float d = Vector3.Distance(position, vehicles[i].transform.position);

                    // is it too close?
                    if (d > 0 && d < radius + vehicles[i].GetComponent<Vehicle>().radius + 2)
                    {
                        Vector3 diff = position - vehicles[i].transform.position;
                        diff = diff.normalized;

                        // increment count and add to sum
                        sum += diff;
                        count += 1f;
                    }
                }
            }
        }

        // get average
        if (count > 0)
        {
            sum = sum / count;

            // calculate steering force
            sum = sum.normalized * maxSpeed;
            steeringForce = sum - velocity;
        }


        return steeringForce;
    }

    // Makes agent wander randomly
    public Vector3 Wander()
    {
        // randomly generate angle
        wanderAngle = Random.Range(0, maxAngle);

        // calculate x and z
        float x = Mathf.Cos(wanderAngle) * wanderRadius;
        float z = Mathf.Sin(wanderAngle) * wanderRadius;

        // project wander circle in front of agent
        Vector3 centerPoint = velocity;
        centerPoint = position + centerPoint.normalized;
        Vector3 target = new Vector3(centerPoint.x + x, centerPoint.y, centerPoint.z + z);

        Vector3 steeringForce = Seek(target);

        return steeringForce;
    }

    //Seek the target's position - return steering force
    public Vector3 Seek(Vector3 targetPos)
    {
        Vector3 desiredVelocity = targetPos - position;

        //want to go toward THERE at max speed
        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        //Reynold's rule for steering
        Vector3 steeringForce = desiredVelocity - velocity;


        // debug lines
        Debug.DrawLine(position, position + desiredVelocity, Color.blue);
        Debug.DrawLine(position + velocity, position + desiredVelocity, Color.black);

        return steeringForce;
    }

    // move toward predicted position of target
    public Vector3 Pursue(GameObject target)
    {
        //vectors
        Vector3 steeringForce;

        // check if future position is behind agent, seek if true
        Vector3 VtoC = (target.transform.position + target.GetComponent<Vehicle>().velocity) - position;
        if (Vector3.Dot(VtoC, direction) < 0)
        {
            steeringForce = Seek(target.transform.position);
        }
        else
        {
            Vector3 desiredVelocity = (target.GetComponent<Vehicle>().position + target.GetComponent<Vehicle>().velocity) - position;

            //want to go toward THERE at max speed
            desiredVelocity = desiredVelocity.normalized * maxSpeed;

            //Reynold's rule for steering
            steeringForce = desiredVelocity - velocity;

            // debug lines
            Debug.DrawLine(position, position + desiredVelocity, Color.blue);
            Debug.DrawLine(position + velocity, position + desiredVelocity, Color.black);
        }

        return steeringForce;
    }

    // flee the target's position
    public Vector3 Flee(Vector3 targetPos)
    {
        Vector3 desiredVelocity = position - targetPos;

        //want to go toward THERE at max speed
        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        //Reynold's rule for steering
        Vector3 steeringForce = desiredVelocity - velocity;

        // debug lines
        Debug.DrawLine(position, position + desiredVelocity, Color.blue);
        Debug.DrawLine(position + velocity, position + desiredVelocity, Color.black);

        return steeringForce;
    }

    // evade the target's predicted position
    public Vector3 Evade(GameObject target)
    {
        Vector3 desiredVelocity = position - (target.GetComponent<Vehicle>().position + target.GetComponent<Vehicle>().velocity);

        //want to go toward THERE at max speed
        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        //Reynold's rule for steering
        Vector3 steeringForce = desiredVelocity - velocity;

        // debug lines
        Debug.DrawLine(position, position + desiredVelocity, Color.blue);
        Debug.DrawLine(position + velocity, position + desiredVelocity, Color.black);

        return steeringForce;
    }

    public Vector3 CheckBounds()
    {
        Vector3 steeringForce = Vector3.zero;

        // boundary check
        if (theManager.maxPos.x < (position.x + (2 * velocity.x)))
        {
            // calculate seek force
            steeringForce = Seek(new Vector3(theManager.centerPos.x, theManager.centerPos.y, position.z));
        }
        else if (theManager.minPos.x > (position.x + (2 * velocity.x)))
        {
            // calculate seek force
            steeringForce = Seek(new Vector3(theManager.centerPos.x,theManager.centerPos.y, position.z));
        }
        if (theManager.maxPos.z < (position.z + (2 * velocity.z)))
        {
            // calculate seek force
            steeringForce = Seek(new Vector3(position.x, theManager.centerPos.y, theManager.centerPos.z));
        }
        else if (theManager.minPos.z > (position.z + (2 * velocity.z)))
        {
            // calculate seek force
            steeringForce = Seek(new Vector3(position.x, theManager.centerPos.y, theManager.centerPos.z));
        }

        return steeringForce;
    }

    //applies a force and causes it to accumulate
    public void ApplyForce(Vector3 aforce)
    {
        force += aforce / mass; //accumulate force
    }

    void ApplyFriction(float coefficient)
    {
        Vector3 friction = -1 * velocity.normalized; //could normalize velocity instead
        friction *= coefficient; //coefficient of friction

        // friction applied 
        if (force.magnitude > friction.magnitude)
        {
            force += friction;
        }
        else
        {
            force = Vector3.zero;
            velocity *= 0.9f;
        }
    }
    void UpdatePosition()
    {
        velocity += force * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        //update position vector based on velocity
        position += velocity * Time.deltaTime;
    }
    
    // overridden by child classes ---- calculates steering forces
    public abstract void CalcSteeringForces();
}
