using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKController : MonoBehaviour
{
    private MultiAimConstraint _headAim;
    private Transform _headTarget;

    private void Awake()
    {
        _headAim = transform.Find("HeadAim").GetComponent<MultiAimConstraint>();
        _headTarget = _headAim.transform.Find("HeadTarget");
    }

    private void Update()
    {
        if (WorldHandler._Instance._Player._IsStrafing)
        {
            _headAim.weight = Mathf.Lerp(_headAim.weight, 1f, Time.deltaTime * 2f);
            _headTarget.position = Vector3.Lerp(_headTarget.position, WorldHandler._Instance._Player._LookAtForCam.transform.position, Time.deltaTime * 2f);
        }
        else
        {
            _headAim.weight = Mathf.Lerp(_headAim.weight, 0f, Time.deltaTime * 2f);
        }
    }
}
