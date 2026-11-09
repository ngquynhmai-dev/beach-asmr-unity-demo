using System.Collections;
using UnityEngine;

public class WinterDoor : MonoBehaviour
{
    public float openAngle = 95f;
    public float rotationSpeed = 4f;
    private Quaternion closedRotation;
    private bool isOpen;
    private Coroutine motion;

    private void Start()
    {
        closedRotation = transform.localRotation;
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        if (motion != null) StopCoroutine(motion);
        motion = StartCoroutine(AnimateDoor());
    }

    private IEnumerator AnimateDoor()
    {
        Quaternion target = isOpen ? closedRotation * Quaternion.Euler(0f, openAngle, 0f) : closedRotation;
        while (Quaternion.Angle(transform.localRotation, target) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * rotationSpeed);
            yield return null;
        }
        transform.localRotation = target;
    }
}