using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("Mouvement")]
    public float speed = 3f;
    public float detectionRange = 15f;
    [Tooltip("Distance à laquelle l'ennemi s'arrête (range d'attaque). Si <= 0, utilise EnemyCombat.attackRange.")]
    public float stoppingDistance = 1.8f;

    [Header("Animation")]
    [Tooltip("Animator du modèle ennemi. Si vide, recherché dans les enfants.")]
    public Animator animator;
    [Tooltip("Nom du paramètre bool 'Walking' / 'Run' dans l'Animator.")]
    public string movingBoolParam = "IsMoving";
    [Tooltip("Nom du paramètre float 'Speed' (utile pour blend tree).")]
    public string speedFloatParam = "Speed";
    [Tooltip("Trigger d'attaque (Animator). EnemyCombat l'appelle via TriggerAttackAnimation().")]
    public string attackTriggerParam = "Attack";
    [Tooltip("Trigger de mort.")]
    public string deathTriggerParam = "Die";

    Transform player;
    Rigidbody rb;
    EnemyStats stats;
    bool dying;

    // Hashes (perf)
    int hashMoving;
    int hashSpeed;
    int hashAttack;
    int hashDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<EnemyStats>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!string.IsNullOrEmpty(movingBoolParam)) hashMoving = Animator.StringToHash(movingBoolParam);
        if (!string.IsNullOrEmpty(speedFloatParam)) hashSpeed = Animator.StringToHash(speedFloatParam);
        if (!string.IsNullOrEmpty(attackTriggerParam)) hashAttack = Animator.StringToHash(attackTriggerParam);
        if (!string.IsNullOrEmpty(deathTriggerParam)) hashDeath = Animator.StringToHash(deathTriggerParam);

        if (stats != null) stats.OnDeath.AddListener(OnDie);
    }

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        var ps = Object.FindFirstObjectByType<PlayerStats>();
        if (ps != null) { player = ps.transform; return; }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (dying) return;
        if (player == null) { FindPlayer(); SetMoving(false, 0f); return; }

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange)
        {
            SetMoving(false, 0f);
            return;
        }

        // S'arrête à portée d'attaque
        if (distance <= stoppingDistance)
        {
            SetMoving(false, 0f);
            FaceTarget();
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 move = direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
        SetMoving(true, speed);
        FaceTarget();
    }

    void FaceTarget()
    {
        if (player == null) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(to, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.fixedDeltaTime);
    }

    void SetMoving(bool moving, float currentSpeed)
    {
        if (animator == null) return;
        if (hashMoving != 0) animator.SetBool(hashMoving, moving);
        if (hashSpeed != 0) animator.SetFloat(hashSpeed, moving ? currentSpeed : 0f);
    }

    public void TriggerAttackAnimation()
    {
        if (animator == null || hashAttack == 0) return;
        animator.SetTrigger(hashAttack);
    }

    void OnDie()
    {
        dying = true;
        if (animator != null && hashDeath != 0) animator.SetTrigger(hashDeath);
    }

    void OnDestroy()
    {
        if (stats != null) stats.OnDeath.RemoveListener(OnDie);
    }
}
