using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    //	Camera	array	that	holds	a	reference	to	every	camera	in	the	scene
    public Camera[] cameras;

    //	Current	camera	
    public int currentCameraIndex;

    // reference to manager
    AgentManager manager;

    //	Use	this	for	initialization
    void Start()
    {
        currentCameraIndex = 0;

        //	Turn	all	cameras	off,	except	the	first	default	one
        for (int i = 1; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }

        //	If	any	cameras	were	added	to	the	controller,	enable	the	first	one
        if (cameras.Length > 0)
        {
            cameras[0].gameObject.SetActive(true);
        }
    }

    //	Update	is	called	once	per	frame
    void Update()
    {

        //	Press	the	'C'	key	to	cycle	through	cameras	in	the	array
        if (Input.GetKeyDown(KeyCode.C))
        {
            //	Cycle	to	the	next	camera
            currentCameraIndex++;

            //	If	cameraIndex is	in	bounds,	set	this	camera	active	and	last	one	inactive
            if (currentCameraIndex < cameras.Length)
            {
                cameras[currentCameraIndex - 1].gameObject.SetActive(false);
                cameras[currentCameraIndex].gameObject.SetActive(true);
            }

            //	If	last	camera,	cycle	back	to	first	camera
            else
            {
                cameras[currentCameraIndex - 1].gameObject.SetActive(false);
                currentCameraIndex = 0;
                cameras[currentCameraIndex].gameObject.SetActive(true);
            }
        }
        
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 200, 100), cameras[currentCameraIndex].ToString());
    }
}
