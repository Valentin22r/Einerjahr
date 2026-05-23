using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float speed = 3f;
    public float detectionRange = 10f;

    private Transform player;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectionRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;

            Vector3 move = direction * speed * Time.fixedDeltaTime;

            rb.MovePosition(rb.position + move);

            transform.LookAt(player);
            Vector3 tmp = transform.localEulerAngles;
            tmp.z = 0;
            tmp.x = 0;
            transform.localEulerAngles = tmp;
        }
    }
}