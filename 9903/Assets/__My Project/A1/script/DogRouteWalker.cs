using System.Collections;
using UnityEngine;

public class DogRouteWalker : MonoBehaviour
{
    [Header("Route Points")]
    [Tooltip("按顺序拖入狗要经过的 Empty 路线点。")]
    [SerializeField] private Transform[] routePoints;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float arriveDistance = 0.08f;

    [Tooltip("每个路线点到达后停留几秒。")]
    [SerializeField] private float waitAtEachPoint = 0f;

    [Header("Animation")]
    [SerializeField] private Animator dogAnimator;

    [Tooltip("Animator 中控制走路的 Bool 参数名称。")]
    [SerializeField] private string walkingBoolName = "IsWalking";

    [Header("Route Settings")]
    [Tooltip("勾选后，跑到最后一个点会重新从第一个点开始。")]
    [SerializeField] private bool loopRoute = false;

    [Tooltip("勾选后，游戏开始就自动走。夜晚剧情建议取消勾选。")]
    [SerializeField] private bool playOnStart = false;

    private Coroutine routeRoutine;
    private bool isWalkingRoute;

    private void Start()
    {
        SetWalkingAnimation(false);

        if (playOnStart)
        {
            BeginRoute();
        }
    }

    // 在 NightDogTransition 或 CityTimeController 中调用。
    public void BeginRoute()
    {
        if (routePoints == null || routePoints.Length == 0)
        {
            Debug.LogWarning(
                "DogRouteWalker: Please assign route points."
            );
            return;
        }

        StopRoute();

        routeRoutine = StartCoroutine(FollowRouteRoutine());
    }

    public void StopRoute()
    {
        if (routeRoutine != null)
        {
            StopCoroutine(routeRoutine);
            routeRoutine = null;
        }

        isWalkingRoute = false;
        SetWalkingAnimation(false);
    }

    private IEnumerator FollowRouteRoutine()
    {
        isWalkingRoute = true;

        int pointIndex = 0;

        while (isWalkingRoute)
        {
            Transform targetPoint = routePoints[pointIndex];

            if (targetPoint == null)
            {
                pointIndex++;
                continue;
            }

            yield return StartCoroutine(
                WalkToPoint(targetPoint)
            );

            if (waitAtEachPoint > 0f)
            {
                SetWalkingAnimation(false);

                yield return new WaitForSeconds(
                    waitAtEachPoint
                );
            }

            pointIndex++;

            if (pointIndex >= routePoints.Length)
            {
                if (loopRoute)
                {
                    pointIndex = 0;
                }
                else
                {
                    break;
                }
            }
        }

        isWalkingRoute = false;
        routeRoutine = null;
        SetWalkingAnimation(false);
    }

    private IEnumerator WalkToPoint(Transform targetPoint)
    {
        SetWalkingAnimation(true);

        while (true)
        {
            Vector3 targetPosition = targetPoint.position;

            // 保持狗原本的高度，避免上下漂浮。
            targetPosition.y = transform.position.y;

            Vector3 direction =
                targetPosition - transform.position;

            float distance = direction.magnitude;

            if (distance <= arriveDistance)
            {
                transform.position = targetPosition;
                break;
            }

            Vector3 flatDirection = direction.normalized;

            Quaternion targetRotation = Quaternion.LookRotation(
                flatDirection,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (dogAnimator == null ||
            string.IsNullOrEmpty(walkingBoolName))
        {
            return;
        }

        dogAnimator.SetBool(walkingBoolName, isWalking);
    }
}