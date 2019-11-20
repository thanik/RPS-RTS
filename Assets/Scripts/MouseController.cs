using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public float scrollSpeed = 25;
    public int scrollWidth = 15;
    public float minPositionX = -12f;
    public float maxPositionX = 12f;
    public float minPositionY = -9f;
    public float maxPositionY = 9f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        float xpos = Input.mousePosition.x;
        float ypos = Input.mousePosition.y;
        Vector3 movement = new Vector3(0, 0, 0);

        //horizontal camera movement
        if (xpos >= 0 && xpos < scrollWidth)
        {
            movement.x -= scrollSpeed;
        }
        else if (xpos <= Screen.width && xpos > Screen.width - scrollWidth)
        {
            movement.x += scrollSpeed;
        }

        //vertical camera movement
        if (ypos >= 0 && ypos < scrollWidth)
        {
            movement.y -= scrollSpeed;
        }
        else if (ypos <= Screen.height && ypos > Screen.height - scrollWidth)
        {
            movement.y += scrollSpeed;
        }

        //make sure movement is in the direction the camera is pointing
        //but ignore the vertical tilt of the camera to get sensible scrolling
        movement = Camera.main.transform.TransformDirection(movement);

        //calculate desired camera position based on received input
        Vector3 origin = Camera.main.transform.position;
        Vector3 destination = origin;
        destination.x += movement.x;
        destination.y += movement.y;

        if (destination.x < minPositionX)
        {
            destination.x = minPositionX;
        }
        if (destination.x > maxPositionX)
        {
            destination.x = maxPositionX;
        }

        if (destination.y < minPositionY)
        {
            destination.y = minPositionY;
        }
        if (destination.y > maxPositionY)
        {
            destination.y = maxPositionY;
        }

        //if a change in position is detected perform the necessary update
        if (destination != origin)
        {
            Camera.main.transform.position = Vector3.MoveTowards(origin, destination, Time.deltaTime * scrollSpeed);
        }
    }
}
