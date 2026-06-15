using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class CleanerAI : MonsterAI
{
    [Header("Cleaner Settings")]
    public float playerSafeRadius = 6f;   // Won't steal and will escape if player is within this range
    public float scanInterval = 1f;

    private Transform targetLoot;
    private float scanTimer;
    private Animator anim;
    private MonsterState lastState = MonsterState.Idle;

    protected override void Start()
    {
        base.Start();
        anim = GetComponent<Animator>();
        if (anim != null) anim.Play("Idle");
    }

    protected override void Update()
    {
        base.Update();
        SyncAnimation();
    }

    protected override void UpdateIdle()
    {
        // 1. Check if player is too close - if so, escape
        if (player != null && Vector3.Distance(transform.position, player.position) < playerSafeRadius)
        {
            currentState = MonsterState.Patrol; // Patrol repurposed as Escape state
            return;
        }

        // 2. Scan for loot
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            FindLoot();
        }

        if (targetLoot != null)
        {
            currentState = MonsterState.Chase;
        }
    }

    protected override void UpdatePatrol()
    {
        // Patrol state is repurposed as Escape state for Cleaner
        if (player == null || agent == null)
        {
            currentState = MonsterState.Idle;
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer > playerSafeRadius * 1.5f)
        {
            // Successfully escaped
            currentState = MonsterState.Idle;
            agent.ResetPath();
            return;
        }

        // Run away from player
        Vector3 dirAway = (transform.position - player.position).normalized;
        Vector3 escapeTarget = transform.position + dirAway * 12f;
        if (NavMesh.SamplePosition(escapeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(hit.position);
        }
    }

    protected override void UpdateChase()
    {
        // Check if player is too close - if so, escape
        if (player != null && Vector3.Distance(transform.position, player.position) < playerSafeRadius)
        {
            currentState = MonsterState.Patrol; // Escape
            agent?.ResetPath();
            return;
        }

        if (targetLoot == null || !targetLoot.gameObject.activeInHierarchy)
        {
            currentState = MonsterState.Idle;
            agent?.ResetPath();
            return;
        }

        if (agent == null) return;
        agent.speed = patrolSpeed;
        agent.SetDestination(targetLoot.position);

        float distToLoot = Vector3.Distance(transform.position, targetLoot.position);
        if (distToLoot < 2f)
        {
            currentState = MonsterState.Attack; // Repurposed as Steal state
            agent.ResetPath();
        }
    }

    protected override void UpdateAttack()
    {
        // Repurposed as Steal state
        if (targetLoot == null || !targetLoot.gameObject.activeInHierarchy)
        {
            currentState = MonsterState.Idle;
            return;
        }

        // Only steal if player is not nearby
        bool playerNear = player != null && Vector3.Distance(transform.position, player.position) < playerSafeRadius;
        if (playerNear)
        {
            currentState = MonsterState.Patrol; // Escape
            return;
        }

        // Steal loot
        StealLoot();
    }

    private void FindLoot()
    {
        LootItem[] items = Object.FindObjectsByType<LootItem>(FindObjectsInactive.Exclude);
        if (items.Length == 0) { targetLoot = null; return; }

        // Pick closest active loot
        LootItem closest = null;
        float minDist = float.MaxValue;
        foreach (var item in items)
        {
            if (!item.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(transform.position, item.transform.position);
            if (d < minDist) { minDist = d; closest = item; }
        }
        targetLoot = closest?.transform;
    }

    private void StealLoot()
    {
        Debug.Log($"[Cleaner] Stole {targetLoot.name}!");
        Destroy(targetLoot.gameObject);
        targetLoot = null;
        currentState = MonsterState.Idle;
    }

    private void SyncAnimation()
    {
        if (anim == null) return;
        if (currentState != lastState)
        {
            lastState = currentState;
            switch (currentState)
            {
                case MonsterState.Idle:
                    anim.Play("Idle");
                    break;
                case MonsterState.Patrol:
                    anim.Play("Escape");
                    break;
                case MonsterState.Chase:
                    anim.Play("Idle");
                    break;
                case MonsterState.Attack:
                    anim.Play("Steal");
                    break;
            }
        }
        if (anim != null)
        {
            foreach (var param in anim.parameters)
            {
                if (param.name == "Speed")
                {
                    anim.SetFloat("Speed", agent != null ? agent.velocity.magnitude : 0f);
                    break;
                }
            }
        }
    }
}
