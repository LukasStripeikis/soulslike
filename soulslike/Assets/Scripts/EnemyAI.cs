using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private int maxStamina;
    private float stamina;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stamina = maxStamina;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position += new Vector3(0.1f, 0, 0) * Mathf.Sin(Time.time);
    }
}
