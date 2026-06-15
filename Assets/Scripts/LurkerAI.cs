using UnityEngine;

public class LurkerAI : MonsterAI
{
    [Header("Lurker Settings")]
    public float soundSensitivity = 12f;
    public float attackDamage = 20f;
    public float attackCooldown = 2f;
    public float attackRange = 2f;

    private float attackTimer;
    private float soundCheckInterval = 0.3f;
    private float soundCheckTimer;
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
        soundCheckTimer -= Time.deltaTime;
        if (soundCheckTimer <= 0f)
        {
            soundCheckTimer = soundCheckInterval;
            if (CheckForNoise()) currentState = MonsterState.Chase;
        }
    }

    protected override void UpdatePatrol() { }

    protected override void UpdateChase()
    {
        if (player == null || agent == null) return;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) < attackRange)
        {
            currentState = MonsterState.Attack;
            attackTimer = 0f; // Attack immediately on reach
        }

        // Give up if player is far and not making noise
        if (!CheckForNoise() && Vector3.Distance(transform.position, player.position) > detectionRange * 1.5f)
        {
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
            DealDamage();
        }
    }

    private void DealDamage()
    {
        if (player == null) return;
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.TakeDamage(attackDamage);
            Debug.Log($"[Lurker] Hit player for {attackDamage} damage.");
        }
    }

    private bool CheckForNoise()
    {
        if (player == null) return false;
        CharacterController cc = player.GetComponent<CharacterController>();
        bool movingFast = cc != null && cc.velocity.magnitude > 3f;
        return movingFast && Vector3.Distance(transform.position, player.position) < soundSensitivity;
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
                    anim.Play("Idle");
                    break;
                case MonsterState.Chase:
                    anim.Play("Idle");
                    break;
                case MonsterState.Attack:
                    anim.Play("BurstAttack");
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
