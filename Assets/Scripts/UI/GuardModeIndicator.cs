using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardModeIndicator : MonoBehaviour
{
    // Start is called before the first frame update
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

        spriteRenderer.enabled = (GetComponentInParent<NetworkObject>().currentAction == NetworkObjectAction.GUARD);

    }
}
