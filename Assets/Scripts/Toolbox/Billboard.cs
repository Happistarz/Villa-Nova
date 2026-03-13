using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera mainCamera;
    
    private void Start()
    {
        if (!mainCamera) 
            mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (!mainCamera) return;
        
        var lookDirection = transform.position - mainCamera.transform.position;
        lookDirection.y = 0;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}
