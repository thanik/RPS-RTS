using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoAttackDetection : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER)
        {
            NetworkObject newTargetNetObj = collision.gameObject.GetComponentInParent<NetworkObject>();
            NetworkObject owner = GetComponentInParent<NetworkObject>();
            if (owner.currentAction == NetworkObjectAction.GUARD)
            {
                if (owner.clientOwnerID != newTargetNetObj.clientOwnerID)
                {
                    owner.objectIDTarget = newTargetNetObj.objectID;
                }

            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER)
        {
            NetworkObject newTargetNetObj = collision.gameObject.GetComponentInParent<NetworkObject>();
            NetworkObject owner = GetComponentInParent<NetworkObject>();
            if (owner.currentAction == NetworkObjectAction.GUARD)
            {
                if (owner.clientOwnerID != newTargetNetObj.clientOwnerID && owner.objectIDTarget != newTargetNetObj.objectID)
                {
                    owner.objectIDTarget = newTargetNetObj.objectID;
                }

            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER)
        {
            NetworkObject newTargetNetObj = collision.gameObject.GetComponentInParent<NetworkObject>();
            NetworkObject owner = GetComponentInParent<NetworkObject>();
            if (owner.currentAction == NetworkObjectAction.GUARD)
            {
                if (owner.objectIDTarget == newTargetNetObj.objectIDTarget)
                {
                    owner.objectIDTarget = -1;
                }

            }
        }
    }
}
