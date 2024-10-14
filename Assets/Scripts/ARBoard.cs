using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(Collider))]
public class ARBoard : MonoBehaviour
{
    [SerializeField] float maxDistance = 0.1f;

    private Collider boardCollider;

    private void Awake()
    {
        boardCollider = GetComponent<Collider>();
    }

    public bool ImageIsOnBoard(ARTrackedImage image)
    {
        // Board에 대한 Raycast 실패시
        if (false == boardCollider.Raycast(new Ray(image.transform.position + maxDistance * boardCollider.transform.up, -boardCollider.transform.up), out _, 2f * maxDistance))
        {
            Debug.Log("[BoardManager]보드 영역에 놓여있지 않음");
            return false;
        }

        // 기울기가 너무 다를 경우
        if (0.8 > Vector3.Dot(image.transform.up, boardCollider.transform.up))
        {
            Debug.Log($"[BoardManager]보드와 기울기가 크게 다름 {image.transform.up}");
            return false;
        }

        return true;
    }
}
