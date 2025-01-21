using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnDynamicNetworkObject : NetworkBehaviour
{
    public NetworkObject networkObjectToSpawnPrefab;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            spawnNewDynamicNetworkObject();
        }
    }
    [ContextMenu("spawnNewNetworkObject")]
    public void spawnNewDynamicNetworkObject()
    {
        spawnObjectNowServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void spawnObjectNowServerRpc(ulong clientIDwhoSpawned)
    {
        NetworkObject objectInstantiated = Instantiate(networkObjectToSpawnPrefab);
        objectInstantiated.SpawnWithOwnership(clientIDwhoSpawned);
    }
}
