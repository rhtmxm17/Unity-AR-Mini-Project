using EPOOutline;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImage))]
public class TrackedImageUnit : MonoBehaviour
{
    [SerializeField] Outlinable outlineQuad;
    private ARTrackedImage trackedImage;

    private void Awake()
    {
        trackedImage = GetComponent<ARTrackedImage>();
    }

    private void Start()
    {
        outlineQuad.transform.localScale = trackedImage.size;
        StartCoroutine(UpdateOutlineColor());
    }

    private IEnumerator UpdateOutlineColor()
    {
        YieldInstruction period = new WaitForSeconds(1f);
        while (true)
        {
            switch (trackedImage.trackingState)
            {
                case TrackingState.None:
                    outlineQuad.OutlineParameters.Color = Color.red;
                    break;
                case TrackingState.Limited:
                    outlineQuad.OutlineParameters.Color = Color.yellow;
                    break;
                case TrackingState.Tracking:
                    outlineQuad.OutlineParameters.Color = Color.blue;
                    break;
            }
            yield return period;
        }
    }
}
