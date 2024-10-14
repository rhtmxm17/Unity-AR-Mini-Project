using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ARSceneManager : MonoBehaviour
{
    [SerializeField] ARSessionOrigin sessionOrigin;
    [SerializeField] AROcclusionManager occlusionManager;
    [SerializeField] ARPlaneSelector planeSelector;
    [SerializeField] BoardRectSelector boardRectSelector;
    [SerializeField] BoardPolygonSelector boardPolygoSelector;
    [SerializeField] BoardModifyer boardModifyer;
    [SerializeField] Button editButton;

    [SerializeField] UnitDatatable unitDatatable;

    private ARBoard board = null;
    private ARPlaneManager planeManager;
    private ARTrackedImageManager trackedImageManager;
    public bool ActiveOcclusion
    {
        set
        {
            occlusionManager.enabled = value;
        }
    }

    private void Start()
    {
        planeManager = sessionOrigin.GetComponent<ARPlaneManager>();
        trackedImageManager = sessionOrigin.GetComponent<ARTrackedImageManager>();

        // testcode
        planeSelector.OnPlaneSelected.AddListener(boardPolygoSelector.EnterSelectMode);

        if (false)
        {
            // 감지된 평면중 하나를 선택 완료시 보드 영역 선택 진입
            planeSelector.OnPlaneSelected.AddListener(boardRectSelector.EnterSelectMode);

            // 보드 영역 선택 완료시 처리
            boardRectSelector.OnBoardCreated.AddListener(board =>
            {
                this.board = board;
                ActiveOcclusion = false;
                editButton.gameObject.SetActive(true);
                editButton.onClick.AddListener(EnterModifyMode);

                // 보드보다 먼저 확인된 이미지가 있다면 보드 등록
                foreach (var image in trackedImageManager.trackables)
                {
                    image.GetComponent<TrackedImageUnit>().SetBoard(board);
                }
            });
        }

        unitDatatable.Initialize();

        StartCoroutine(StartScene());

        trackedImageManager.trackedImagesChanged += SetBoardToImageUnit;
    }

    private IEnumerator StartScene()
    {
        yield return null;

        planeSelector.EnterSelectMode();
    }

    private void SetBoardToImageUnit(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var image in args.added)
        {
            Debug.Log($"[ARSceneManager]이미지:{image.referenceImage.name} 감지됨");
            if (board != null)
            {
                image.GetComponent<TrackedImageUnit>().SetBoard(board);
            }
        }
    }

    private void EnterModifyMode()
    {
        if (board == null)
        {
            Debug.LogWarning("보드가 준비되지 않은 상태에서 편집 모드로 진입 시도됨");
            return;
        }

        boardModifyer.EnterModifyMode(board);
        editButton.onClick.RemoveListener(EnterModifyMode);
        editButton.onClick.AddListener(ExitModifyMode);
    }

    private void ExitModifyMode()
    {
        boardModifyer.ExitModifyMode();
        editButton.onClick.RemoveListener(ExitModifyMode);
        editButton.onClick.AddListener(EnterModifyMode);
    }
}
