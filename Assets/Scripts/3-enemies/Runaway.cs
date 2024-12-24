using UnityEngine;

public class Runaway : TargetRunaway
{
    [Tooltip("The object that we are running away from")]
    [SerializeField] Transform targetObject = null;

    public Vector3 TargetObjectPosition()
    {
        return targetObject.position;
    }

    private void Update()
    {
        SetTarget(targetObject.position);
    }
}
