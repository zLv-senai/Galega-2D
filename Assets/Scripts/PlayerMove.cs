using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour, IDamageable
{
    public int vida = 100;
    public GameObject tiroPrefab;
    [SerializeField] private Transform gun;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Shoot();
    }

    // Update is called once per frame
    void Update()
    {
        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    public void TakeDamage(int dano)
    {
        vida -= dano;
        Debug.Log(vida);
    }

    private void Shoot()
    {
        if(tiroPrefab != null && gun != null)
        {
        Debug.Log("Fireeeeee!!!");
        Instantiate(tiroPrefab, gun.position, gun.rotation);
        }
    }
}
