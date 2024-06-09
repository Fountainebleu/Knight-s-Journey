using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    [SerializeField] Transform player;
    [SerializeField] private float aheadDistance;
    [SerializeField] private float cameraSpead;
    [SerializeField] private float zControl;
    private float lookAhead;

    void Update()
    {
        transform.position = new Vector3(player.position.x + lookAhead, player.position.y, player.position.z - zControl);
        lookAhead = Mathf.Lerp(lookAhead, aheadDistance * player.localScale.x, Time.deltaTime * cameraSpead);
    }
}
