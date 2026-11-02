using System.Collections;
using UnityEngine;

public class LaserActivate : MonoBehaviour
{

    [SerializeField] private ParticleSystem LaserParticle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        LaserParticle.Stop();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        StartCoroutine(ActiveLaser(other));
    }
    IEnumerator ActiveLaser(Collider2D other)
    {
        if(other.CompareTag("Player Final"))
        {
            // Ativa o laser
            LaserParticle.Play();
            yield return new WaitForSeconds(3f);
        }
        LaserParticle.Stop();
    }
}
