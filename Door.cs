using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Door : MonoBehaviour
{
    public bool _IsLocked { get; private set; }
    public bool _IsOpen { get; private set; }

    private Coroutine _doorCoroutine;

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.H))
            if (_IsLocked)
                UnlockDoor();
            else
                LockDoor();

        if (Input.GetKeyDown(KeyCode.G))
            if (_IsOpen)
                CloseDoor();
            else
                OpenDoor();*/
    }
    public void OpenDoor()
    {
        if (_IsLocked || _IsOpen) return;

        GameManager._Instance.CoroutineCall(ref _doorCoroutine, DoorCoroutine(true), this);
    }
    public void CloseDoor()
    {
        if (!_IsOpen) return;

        GameManager._Instance.CoroutineCall(ref _doorCoroutine, DoorCoroutine(false), this);
    }
    public void LockDoor()
    {
        if (_IsLocked || _IsOpen) return;

        _IsLocked = true;
        GetComponent<NavMeshObstacle>().enabled = true;
    }
    public void UnlockDoor()
    {
        if (!_IsLocked) return;

        _IsLocked = false;
        GetComponent<NavMeshObstacle>().enabled = false;
    }

    private IEnumerator DoorCoroutine(bool isOpening)
    {
        _IsOpen = isOpening;

        float time = 0f;
        float targetAngle = isOpening ? -90f : 0f;
        while (time < 2f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0f, targetAngle, 0f), Time.deltaTime * 2.5f);
            time += Time.deltaTime;
            yield return null;
        }
    }

}
