using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ImageOnBoardTester : MonoBehaviour
{
    [SerializeField] ARTrackedImageManager trackedImageManager;
    [SerializeField] BoardManager boardManager;

    private void Start()
    {
        trackedImageManager.trackedImagesChanged += RegistCheckTarget;
    }

    private void RegistCheckTarget(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var image in args.added)
        {
            Debug.Log($"이미지:{image.referenceImage.name} 감지됨");
            StartCoroutine(CheckImageIsOnBoardRoop(image));
        }
    }

    private IEnumerator CheckImageIsOnBoardRoop(ARTrackedImage image)
    {
        YieldInstruction period = new WaitForSeconds(1f);
        while (true)
        {
            Debug.Log($"이미지:{image.referenceImage.name} | 추적상태:{image.trackingState} | 보드위:{boardManager.ImageIsOnBoard(image)}");
            yield return period;
        }
    }
}
