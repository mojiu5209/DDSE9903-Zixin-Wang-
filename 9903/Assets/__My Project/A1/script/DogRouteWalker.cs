using System.Collections;
using UnityEngine;

[System.Serializable]
public class DogRouteStop
{
    [Tooltip("狗要走到的位置。")]
    public Transform routePoint;

    [Tooltip("到达后触发的 Animator Trigger，例如 LOOK、Sniff、First。")]
    public string animationTrigger;

    [Tooltip("该停留动画播放多久。")]
    public float animationDuration = 2f;

    [Tooltip("到达路线点后，动画开始前停几秒。")]
    public float pauseBeforeAnimation = 0f;
}

[System.Serializable]
public class DogNightRoute
{
    [Tooltip("例如 Night 1 Route。")]
    public string routeName = "Night Route";

    [Tooltip("这一晚狗经过的所有路线点。")]
    public DogRouteStop[] routeStops;
}

public class DogRouteWalker : MonoBehaviour
{
    [Header("Night Routes")]
    [Tooltip("每个 Element 代表一晚不同的路线。")]
    [SerializeField] private DogNightRoute[] nightRoutes;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float arriveDistance = 0.08f;

    [Header("Animation")]
    [Tooltip("拖入 Doggo_Graphics 上的 Animator。")]
    [SerializeField] private Animator dogAnimator;

    [Tooltip("Animator 里的 Bool 参数名称。")]
    [SerializeField] private string walkingBoolName = "IsWalking";

    private Coroutine routeRoutine;
    private bool isWalkingRoute;
    private bool isPlayingStopAnimation;

    public bool IsWalkingRoute
    {
        get
        {
            return isWalkingRoute && !isPlayingStopAnimation;
        }
    }

    private void Start()
    {
        SetWalkingAnimation(false);
    }

    // 第 0 条路线 = 第 1 晚
    // 第 1 条路线 = 第 2 晚
    // 第 2 条路线 = 第 3 晚
    public void BeginRoute(int routeIndex)
    {
        if (nightRoutes == null || nightRoutes.Length == 0)
        {
            Debug.LogWarning(
                "DogRouteWalker: No night routes have been assigned."
            );
            return;
        }

        if (routeIndex < 0 || routeIndex >= nightRoutes.Length)
        {
            Debug.LogWarning(
                "DogRouteWalker: Invalid route index: " + routeIndex
            );
            return;
        }

        DogNightRoute selectedRoute = nightRoutes[routeIndex];

        if (selectedRoute.routeStops == null ||
            selectedRoute.routeStops.Length == 0)
        {
            Debug.LogWarning(
                "DogRouteWalker: This night route has no route stops."
            );
            return;
        }

        StopRoute();

        routeRoutine = StartCoroutine(
            FollowRouteRoutine(selectedRoute)
        );
    }

    public void StopRoute()
    {
        if (routeRoutine != null)
        {
            StopCoroutine(routeRoutine);
            routeRoutine = null;
        }

        isWalkingRoute = false;
        isPlayingStopAnimation = false;

        SetWalkingAnimation(false);
    }

    private IEnumerator FollowRouteRoutine(DogNightRoute route)
    {
        isWalkingRoute = true;

        for (int stopIndex = 0;
             stopIndex < route.routeStops.Length;
             stopIndex++)
        {
            DogRouteStop currentStop =
                route.routeStops[stopIndex];

            if (currentStop == null ||
                currentStop.routePoint == null)
            {
                continue;
            }

            yield return StartCoroutine(
                WalkToPoint(currentStop.routePoint)
            );

            isPlayingStopAnimation = true;
            SetWalkingAnimation(false);

            if (currentStop.pauseBeforeAnimation > 0f)
            {
                yield return new WaitForSeconds(
                    currentStop.pauseBeforeAnimation
                );
            }

            if (dogAnimator != null &&
                !string.IsNullOrEmpty(
                    currentStop.animationTrigger
                ))
            {
                dogAnimator.SetTrigger(
                    currentStop.animationTrigger
                );
            }

            if (currentStop.animationDuration > 0f)
            {
                yield return new WaitForSeconds(
                    currentStop.animationDuration
                );
            }

            isPlayingStopAnimation = false;
        }

        isWalkingRoute = false;
        isPlayingStopAnimation = false;
        routeRoutine = null;

        SetWalkingAnimation(false);
    }

    private IEnumerator WalkToPoint(Transform targetPoint)
    {
        SetWalkingAnimation(true);

        while (true)
        {
            Vector3 targetPosition = targetPoint.position;
            targetPosition.y = transform.position.y;

            Vector3 direction =
                targetPosition - transform.position;

            float distance = direction.magnitude;

            if (distance <= arriveDistance)
            {
                transform.position = targetPosition;
                break;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
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