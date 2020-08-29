using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public Transform target;

    // Start is called before the first frame update
    void Start()
    {
        //What I'm going follow?
        if(target == null)
		{
            Debug.LogWarning("Follow Camera doesn't have a target.");
		}
    }

    // Update is called once per frame
    void Update()
    {
        //Where am I?

        //How do i get there?

        //when the camera moves to the player I can't see the player anymore.
        transform.position = target.position + new Vector3(0, 0, -10);
    }
}
