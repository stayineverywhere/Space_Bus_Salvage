using UnityEngine;

public class WatcherAI : MonsterAI
{
    [Header("Watcher Settings")]
    public float lookAtDotThreshold = 0.85f;  // How directly player must look at watcher (~32°)
    public float attackRange = 2.5f;
    public float attackDamage = 25f;
    public float attackCooldown = 1.5f;

    private float attackTimer;
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
        // Watcher ONLY triggers when the player is in its vision cone
        if (IsPlayerInVisionCone())
        {
            currentState = MonsterState.Chase;
        }
    }

    protected override void UpdatePatrol() { }

    protected override void UpdateChase()
    {
        if (player == null || agent == null) return;

        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < attackRange)
        {
            currentState = MonsterState.Attack;
            attackTimer = 0f; // Attack immediately on reach
        }
        else if (dist > detectionRange * 2f)
        {
            // Lost the player
            currentState = MonsterState.Idle;
            agent.ResetPath();
        }
    }

    protected override void UpdateAttack()
    {
        attackTimer -= Time.deltaTime;

        if (player == null)
        {
            currentState = MonsterState.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange * 1.5f)
        {
            currentState = MonsterState.Chase;
            return;
        }

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            pm?.TakeDamage(attackDamage);
            Debug.Log($"[Watcher] Struck player for {attackDamage} damage.");
        }
    }

    private bool IsPlayerInVisionCone()
    {
        if (player == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dirToPlayer);
        
        // Dot of 0.5 means a 120-degree field of view (60 degrees left/right)
        if (dot > 0.5f)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out hit, detectionRange))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    return true;
                }
            }
        }
        return false;
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
                    anim.Play("Patrol");
                    break;
                case MonsterState.Chase:
                    anim.Play("Chase");
                    break;
                case MonsterState.Attack:
                    anim.Play("Idle");
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
