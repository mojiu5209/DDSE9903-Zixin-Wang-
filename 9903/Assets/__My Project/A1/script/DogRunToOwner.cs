using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class DogRunToOwner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("拖入 EZPZ Player Flat Screen WASD。")]
    [SerializeField] private Transform owner;

    [Tooltip("DOG 根物体上的 NavMeshAgent。")]
    [SerializeField] private NavMeshAgent dogAgent;

    [Tooltip("Doggo_Graphics 上的 Animator。")]
    [SerializeField] private Animator dogAnimator;

    [Header("Movement")]
    [SerializeField] private float runSpeed = 4f;

    [Tooltip("距离主人多近时停下。")]
    [SerializeField] private float stopDistance = 1.2f;

    [Tooltip("转向速度。")]
    [SerializeField] private float angularSpeed = 720f;

    [Header("Animation")]
    [Tooltip("狗狗移动时使用的 Bool 参数。")]
    [SerializeField] private string walkingBoolName = "IsWalking";

    [Tooltip("到达主人身边后播放的 Trigger。没有就留空。")]
    [SerializeField] private string reunionTrigger = "LOOK";

    [Header("Events")]
    public UnityEvent onDogReachedOwner;

    private bool isRunningToOwner;
    private bool hasReachedOwner;

    private void Awake()
    {
        if (dogAgent == null)
        {
            dogAgent = GetComponent<NavMeshAgent>();
        }
    }

    // 给公园 Trigger 调用
    public void StartRunningToOwner()
    {
        if (hasReachedOwner || isRunningToOwner)
        {
            return;
        }

        if (owner == null)
        {
            Debug.LogWarning(
                "DogRunToOwner: Owner has not been assigned."
            );
            return;
        }

        if (dogAgent == null)
        {
            Debug.LogWarning(
                "DogRunToOwner: NavMeshAgent has not been assigned."
            );
            return;
        }

        if (!dogAgent.isOnNavMesh)
        {
            Debug.LogWarning(
                "DogRunToOwner: DOG is not on a NavMesh. " +
                "Move DOG onto the baked blue NavMesh area."
            );
            return;
        }

        dogAgent.speed = runSpeed;
        dogAgent.angularSpeed = angularSpeed;
        dogAgent.stoppingDistance = stopDistance;
        dogAgent.isStopped = false;

        isRunningToOwner = true;

        if (dogAnimator != null &&
            !string.IsNullOrEmpty(walkingBoolName))
        {
            dogAnimator.SetBool(
                walkingBoolName,
                true
            );
        }
    }

    private void Update()
    {
        if (!isRunningToOwner ||
            hasReachedOwner ||
            dogAgent == null ||
            owner == null)
        {
            return;
        }

        if (!dogAgent.isOnNavMesh)
        {
            return;
        }

        // 主人走动时，狗狗会持续更新目标位置
        dogAgent.SetDestination(owner.position);

        if (!dogAgent.pathPending &&
            dogAgent.remainingDistance <=
            dogAgent.stoppingDistance + 0.1f)
        {
            ReachOwner();
        }
    }

    private void ReachOwner()
    {
        hasReachedOwner = true;
        isRunningToOwner = false;

        if (dogAgent != null &&
            dogAgent.isOnNavMesh)
        {
            dogAgent.isStopped = true;
            dogAgent.ResetPath();
        }

        if (dogAnimator != null)
        {
            if (!string.IsNullOrEmpty(walkingBoolName))
            {
                dogAnimator.SetBool(
                    walkingBoolName,
                    false
                );
            }

            if (!string.IsNullOrEmpty(reunionTrigger))
            {
                dogAnimator.SetTrigger(
                    reunionTrigger
                );
            }
        }

        onDogReachedOwner?.Invoke();
    }
}