using UnityEngine;
using UnityEngine.Android;
using System.Collections;

public class Begginforblue : MonoBehaviour
{
    
    void Start()
    {
        StartCoroutine(RequestBluetoothPermissionsSequentially());
    }

    IEnumerator RequestBluetoothPermissionsSequentially()
    {
        string[] perms = {
            "android.permission.BLUETOOTH_SCAN",
            "android.permission.BLUETOOTH_CONNECT",
            "android.permission.BLUETOOTH_ADVERTISE"
        };

        foreach (var perm in perms)
        {
            if (!Permission.HasUserAuthorizedPermission(perm))
            {
                bool waiting = true;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += (p) => { Debug.Log($"{p} granted"); waiting = false; };
                callbacks.PermissionDenied += (p) => { Debug.Log($"{p} denied"); waiting = false; };
                Permission.RequestUserPermission(perm, callbacks);

                yield return new WaitUntil(() => !waiting);
            }
        }
    }
}
