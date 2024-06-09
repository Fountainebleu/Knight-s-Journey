using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SpawmManage : MonoBehaviour
{
    [SerializeField] private Enemy Enemy;
    [SerializeField] public Transform SpawndTarget;
    bool canSpawn;
 
    void Update()
    {
        if (!PlayerController.Instance.pState.alive)
        {
            canSpawn = true;
        }
        else if (PlayerController.Instance.pState.alive && canSpawn)
        {
            Instantiate(Enemy, new Vector2(SpawndTarget.position.x, SpawndTarget.position.y), Quaternion.identity);
            canSpawn = false;
        }
    }
}
