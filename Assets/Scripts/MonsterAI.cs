using UnityEngine;
using UnityEngine.AI;

public abstract class MonsterAI : MonoBehaviour
{
    public enum MonsterState { Idle, Patrol, Chase, Attack }
    public MonsterState currentState = MonsterState.Idle;

    protected NavMeshAgent agent;
    protected Transform player;

    [Header("Base AI Settings")]
    public float detectionRange = 15f;
    public float chaseSpeed = 5f;
    public float patrolSpeed = 2f;

    private bool _initialized = false;

    protected virtual void Start()
    {
        StartCoroutine(InitializeAgent());
    }

    private System.Collections.IEnumerator InitializeAgent()
    {
        // Auto-add NavMeshAgent if missing on this prefab
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 1.2f;
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.angularSpeed = 200f;
            agent.acceleration = 10f;
            Debug.Log($"[MonsterAI] Auto-added NavMeshAgent to {name}.");
        }

        // Wait up to 2 seconds for the NavMesh to be baked before enabling AI
        float waited = 0f;
        while (waited < 2f)
        {
            UnityEngine.AI.NavMeshHit hit;
            bool onMesh = UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas);
            if (onMesh)
            {
                // Snap to NavMesh surface
                transform.position = hit.position;
                break;
            }
            waited += Time.deltaTime;
            yield return null;
        }

        // Final check — if still not on NavMesh after 2s, disable AI gracefully
        {
            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Debug.LogWarning($"[MonsterAI] {name} could not find a NavMesh after 2s. AI disabled.");
                agent.enabled = false;
                enabled = false;
                yield break;
            }
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        _initialized = true;
        Debug.Log($"[MonsterAI] {name} initialized on NavMesh.");
    }

    protected virtual void Update()
    {
        if (!_initialized) return;

        // Re-acquire player reference if lost (e.g. scene reload)
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        switch (currentState)
        {
            case MonsterState.Idle:   UpdateIdle();   break;
            case MonsterState.Patrol: UpdatePatrol(); break;
            case MonsterState.Chase:  UpdateChase();  break;
            case MonsterState.Attack: UpdateAttack(); break;
        }
    }

    protected abstract void UpdateIdle();
    protected abstract void UpdatePatrol();
    protected abstract void UpdateChase();
    protected abstract void UpdateAttack();

    protected bool CanSeePlayer()
    {
        if (player == null) return false;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange) return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, (player.position - transform.position).normalized, out hit, detectionRange))
        {
            return hit.transform.CompareTag("Player");
        }
        return false;
    }
}
