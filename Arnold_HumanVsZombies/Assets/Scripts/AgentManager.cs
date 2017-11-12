using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    // prefabs of human, zombie, sphere of truth, and obstacle
    public GameObject humanPrefab;
    public GameObject zombiePrefab;
    public GameObject sphereTruthPrefab;
    public GameObject purifierPrefab;
    public GameObject obstaclePrefab;

    // number of humans/zombies/obj
    public int humanCount;
    public int zombieCount;
    public int obstacleCount;

    // purple sphere
    GameObject sphereOfTruth;

    // human/zombie/obstacles lists
    public List<GameObject> allHumans = new List<GameObject>();
    public List<GameObject> allZombies = new List<GameObject>();
    public List<GameObject> allObstacles = new List<GameObject>();

    // list to hold purifiers
    public List<GameObject> allPurifiers = new List<GameObject>();

    // random x and z positions, y is set to terrain height
    float x;
    float z;
    float y;

    // the terrain
    public Terrain terrain;
    public float buffer = 4f;
    public Vector3 minPos;
    public Vector3 maxPos;
    public Vector3 centerPos;
    

    // collision stuff
    float distance;
    float acceptableDistance;

    // draw debug lines in game view
    public bool debug = false;

    // keep track of current camera for obstacle, zombie, and human adding purposes
    public Camera currentCamera;
    CameraScript cameraScript;

	// Use this for initialization
	void Start ()
    {
        // current game object
        GameObject current;

        // calculate acceptable distance
        acceptableDistance = Mathf.Pow(zombiePrefab.GetComponent<Renderer>().bounds.extents.x + humanPrefab.GetComponent<Renderer>().bounds.extents.x, 2);

        // terrain bounds
        minPos = terrain.terrainData.bounds.min;
        maxPos = terrain.terrainData.bounds.max;

        // apply buffer
        minPos.x += buffer;
        minPos.z += buffer;
        maxPos.x -= buffer;
        maxPos.z -= buffer;

        // center to seek when out of bounds
        centerPos = terrain.terrainData.bounds.center;
        centerPos.y = humanPrefab.transform.lossyScale.y / 2;

        // spawn purple sphere randomly
        x = Random.Range(minPos.x, maxPos.x);
        z = Random.Range(minPos.z, maxPos.z);
        y = terrain.SampleHeight(new Vector3(x, 0, z));
        sphereOfTruth = Instantiate(sphereTruthPrefab, new Vector3(x, y + (humanPrefab.transform.lossyScale.y / 2), z), Quaternion.identity);

        // instantiate humans
        for (int i = 0; i < humanCount; i++)
        {
            // randomly generate position
            x = Random.Range(minPos.x, maxPos.x);
            z = Random.Range(minPos.z, maxPos.z);
            y = terrain.SampleHeight(new Vector3(x, 0, z));

            // create and add human to list
            current = Instantiate(humanPrefab, new Vector3(x, y + (humanPrefab.transform.lossyScale.y / 2), z), Quaternion.identity);
            current.GetComponent<Vehicle>().position = new Vector3(x, y + (humanPrefab.transform.lossyScale.y / 2), z);
            current.GetComponent<Human>().radius = humanPrefab.transform.lossyScale.x / 2;

            allHumans.Add(current);
        }

        // instantiate zombies
        for (int i = 0; i < zombieCount; i++)
        {
            // randomly generate position
            x = Random.Range(minPos.x, maxPos.x);
            z = Random.Range(minPos.z, maxPos.z);
            y = terrain.SampleHeight(new Vector3(x, 0, z));

            // create and add zombie to list
            current = Instantiate(zombiePrefab, new Vector3(x, y + (zombiePrefab.transform.lossyScale.y / 2), z), Quaternion.identity);
            current.GetComponent<Vehicle>().position = new Vector3(x, y + (zombiePrefab.transform.lossyScale.y / 2), z);
            current.GetComponent<Zombie>().radius = zombiePrefab.transform.lossyScale.x / 2;

            allZombies.Add(current);
        }

        // instantiate obstacles
        for (int i = 0; i < obstacleCount; i++)
        {
            // randomly generate position
            x = Random.Range(minPos.x, maxPos.x);
            z = Random.Range(minPos.z, maxPos.z);
            y = terrain.SampleHeight(new Vector3(x, 0, z));

            // create and add obstacle to list
            current = Instantiate(obstaclePrefab, new Vector3(x, y + (obstaclePrefab.transform.lossyScale.y / 10), z), Quaternion.identity);
            current.GetComponent<Obstacle>().position = new Vector3(x, y + (obstaclePrefab.transform.lossyScale.y / 10), z);
            current.GetComponent<Obstacle>().radius = obstaclePrefab.transform.lossyScale.x / 2;

            allObstacles.Add(current);
        }

        // current camera
        cameraScript = FindObjectOfType<CameraScript>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        // change current camera
        currentCamera = cameraScript.cameras[cameraScript.currentCameraIndex];

        MoveSphere();
        CheckCollision();

        // turn debug lines on and off
        if (Input.GetKeyDown(KeyCode.D))
        {
            if(debug)
            {
                debug = false;
            }
            else
            {
                debug = true;
            }
        }

        // add humans, zombies, and obstacles
        AddHumans();
        AddZombies();
        AddObstacles();


        // Set the target of humans and purifiers
        SetHumanTarget();
        GetTarget();

        // possibly reset
        ResetMap();
	}

    // checks the distance to see which zombie is closest
    public List<GameObject> ClosestZombie(GameObject human, float acceptableDistance)
    {
        float distance;
        float closest = acceptableDistance + 1;
        List<GameObject> nearestZombies = new List<GameObject>();

        for (int i = 0; i < allZombies.Count; i++)
        {
            if(allZombies[i] != null)
            {
                distance = Vector3.Distance(allZombies[i].transform.position, human.transform.position);

                //checks if zombie is within flee range
                if (distance < acceptableDistance)
                {
                    if (distance < closest)
                    {
                        closest = distance;
                        nearestZombies.Add(allZombies[i]);
                    }
                }
            }
        }

        return nearestZombies;
    }

    public GameObject ClosestHuman(GameObject zombie)
    {
        float distance;
        float closest = -1;
        GameObject nearestHuman = null;

        // loop through humans and find closest one
        for (int i = 0; i < allHumans.Count; i++)
        {
            distance = Vector3.Distance(allHumans[i].transform.position, zombie.transform.position);

            if(closest != -1)
            {
                if(distance < closest)
                {
                    nearestHuman = allHumans[i];
                    closest = distance;
                }
            }
            else
            {
                nearestHuman = allHumans[i];
                closest = distance;
            }
        }


        return nearestHuman;
    }

    void MoveSphere()
    {
        if(sphereOfTruth != null)
        {
            float distance;

            // check if any humans are close to sphere
            for (int i = 0; i < allHumans.Count; i++)
            {
                distance = Vector3.Distance(sphereOfTruth.transform.position, allHumans[i].transform.position);

                if (distance < 2f)
                {
                    GameObject current;

                    // create purifier
                    current = Instantiate(purifierPrefab, allHumans[i].transform.position, Quaternion.identity);
                    current.GetComponent<Purifier>().position = allHumans[i].transform.position;
                    allPurifiers.Add(current);

                    // remove human
                    current = allHumans[i];
                    allHumans.Remove(allHumans[i]);
                    Destroy(current);

                    // spawn sphere randomly
                    x = Random.Range(minPos.x, maxPos.x);
                    z = Random.Range(minPos.z, maxPos.z);
                    y = terrain.SampleHeight(new Vector3(x, 0, z));

                    sphereOfTruth.transform.position = new Vector3(x, y + (humanPrefab.transform.lossyScale.y / 2), z);
                }
            }
        }
    }

    // sets human seek target if they are close to supplies
    void SetHumanTarget()
    {
        if (sphereOfTruth != null)
        {
            float distance;

            // check if any humans are close to sphere
            for (int i = 0; i < allHumans.Count; i++)
            {
                distance = Vector3.Distance(sphereOfTruth.transform.position, allHumans[i].transform.position);

                // sets human target to sphere
                if (distance < 10f)
                {
                    allHumans[i].GetComponent<Human>().humanTarget = sphereOfTruth;
                }
                else
                {
                    allHumans[i].GetComponent<Human>().humanTarget = null;
                }
            }
        }
    }

    // check if human was bitten
    void CheckCollision()
    {
        GameObject current;

        // reset distance
        distance = 0;

        // loop through all zombies
        for (int i = 0; i < allZombies.Count; i++)
        {
            if(allZombies[i] != null)
            {
                // loop through all humans
                for (int j = 0; j < allHumans.Count; j++)
                {
                    if(allHumans[j] != null)
                    {

                        // calculate actual distance
                        distance = Mathf.Pow(allZombies[i].transform.position.x - allHumans[j].transform.position.x, 2) + Mathf.Pow(allZombies[i].transform.position.z - allHumans[j].transform.position.z, 2);

                        // check for collision
                        if (distance < acceptableDistance + 1.5f)
                        {
                            // create zombie
                            current = Instantiate(zombiePrefab, allHumans[j].transform.position, Quaternion.identity);
                            current.GetComponent<Zombie>().position = allHumans[j].transform.position;
                            allZombies.Add(current);

                            // remove human
                            current = allHumans[j];
                            allHumans.Remove(allHumans[j]);
                            Destroy(current);
                        }

                        // reset distance
                        distance = 0;
                    }
                }
            }
        }

        // loop through all purifiers
        for (int i = 0; i < allPurifiers.Count; i++)
        {
            if(allPurifiers[i] != null)
            {
                // loop through all zombies
                for (int j = 0; j < allZombies.Count; j++)
                {
                    if (allZombies[j] != null)
                    {
                        distance = Mathf.Pow(allZombies[j].transform.position.x - allPurifiers[i].transform.position.x, 2) + Mathf.Pow(allZombies[j].transform.position.z - allPurifiers[i].transform.position.z, 2);

                        // check for collision
                        if (distance < acceptableDistance + 1.5f)
                        {
                            // create human
                            current = Instantiate(humanPrefab, allZombies[j].transform.position, Quaternion.identity);
                            current.GetComponent<Human>().position = allZombies[j].transform.position;
                            allHumans.Add(current);

                            // remove zombie
                            current = allZombies[j];
                            allZombies.Remove(allZombies[j]);
                            Destroy(current);

                            // purifier turns back into human
                            current = Instantiate(humanPrefab, allPurifiers[i].transform.position, Quaternion.identity);
                            current.GetComponent<Human>().position = allPurifiers[i].transform.position;
                            allHumans.Add(current);

                            // remove purifier
                            current = allPurifiers[i];
                            allPurifiers.Remove(allPurifiers[i]);
                            Destroy(current);
                            break;
                        }

                        // reset distance
                        distance = 0;
                    }
                }
            }
        }
    }

    // add zombies ob button press
    void AddZombies()
    {
        // store current zombie
        GameObject current;

        // spawn zombie on button press
        if(Input.GetKeyDown(KeyCode.Z))
        {
            // get mouse position
            Ray mouseRay = currentCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(mouseRay, out hit))
            {
                y = terrain.SampleHeight(new Vector3(hit.point.x, 0, hit.point.z));

                // create and add zombie to list
                current = Instantiate(zombiePrefab, new Vector3(hit.point.x, y + (zombiePrefab.transform.lossyScale.y / 2), hit.point.z), Quaternion.identity);
                current.GetComponent<Vehicle>().position = new Vector3(hit.point.x, y + (zombiePrefab.transform.lossyScale.y / 2), hit.point.z);
                current.GetComponent<Zombie>().radius = zombiePrefab.transform.lossyScale.x / 2;

                allZombies.Add(current);
            }
        }
    }

    void AddHumans()
    {
        // store current Human
        GameObject current;

        // spawn human on button press
        if (Input.GetKeyDown(KeyCode.H))
        {
            // get mouse position
            Ray mouseRay = currentCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit))
            {
                y = terrain.SampleHeight(new Vector3(hit.point.x, 0, hit.point.z));

                // create and add human to list
                current = Instantiate(humanPrefab, new Vector3(hit.point.x, y + (humanPrefab.transform.lossyScale.y / 2), hit.point.z), Quaternion.identity);
                current.GetComponent<Vehicle>().position = new Vector3(hit.point.x, y + (humanPrefab.transform.lossyScale.y / 2), hit.point.z);
                current.GetComponent<Human>().radius = humanPrefab.transform.lossyScale.x / 2;

                allHumans.Add(current);
            }
        }
    }

    void AddObstacles()
    {
        // store current Human
        GameObject current;

        // spawn human on button press
        if (Input.GetKeyDown(KeyCode.O))
        {
            // get mouse position
            Ray mouseRay = currentCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit))
            {
                y = terrain.SampleHeight(new Vector3(hit.point.x, 0, hit.point.z));

                // create and add human to list
                current = Instantiate(obstaclePrefab, new Vector3(hit.point.x, y + (obstaclePrefab.transform.lossyScale.y / 10), hit.point.z), Quaternion.identity);
                current.GetComponent<Obstacle>().position = new Vector3(hit.point.x, y + (obstaclePrefab.transform.lossyScale.y / 10), hit.point.z);
                current.GetComponent<Obstacle>().radius = obstaclePrefab.transform.lossyScale.x / 2;

                allObstacles.Add(current);
            }
        }
    }

    // Get purifier's target if purifier is active

    void GetTarget()
    {

        float distance;
        float closest = -1;
        GameObject nearestZombie = null;

        //loop through purifiers
        for(int j = 0; j < allPurifiers.Count; j++)
        {
            if(allPurifiers[j] != null)
            {
                // loop through humans and find closest one
                for (int i = 0; i < allZombies.Count; i++)
                {
                    if(allZombies[i] != null)
                    {
                        distance = Vector3.Distance(allZombies[i].transform.position, allPurifiers[j].transform.position);
                        if (closest != -1)
                        {
                            if (distance < closest)
                            {
                                nearestZombie = allZombies[i];
                                closest = distance;
                            }
                        }
                        else
                        {
                            nearestZombie = allZombies[i];
                            closest = distance;
                        }
                    }
                }
                allPurifiers[j].GetComponent<Purifier>().purifierTarget = nearestZombie;
            }
        }
    }

    // reset everything on key press
    void ResetMap()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            //clear lists
            foreach(GameObject human in allHumans)
            {
                Destroy(human);
            }
            foreach (GameObject zombie in allZombies)
            {
                Destroy(zombie);
            }
            foreach (GameObject obstacle in allObstacles)
            {
                Destroy(obstacle);
            }
            allHumans.Clear();
            allObstacles.Clear();
            allZombies.Clear();

            // destroy sphere if it exists
            if(sphereOfTruth != null)
            {
                Destroy(sphereOfTruth);
            }

            Start();
        }
    }
}
