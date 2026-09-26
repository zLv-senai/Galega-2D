using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour, IDamageable
{
    public int vida = 100;
    public int level = 1;
    private int xp = 0;
    public float velocidade = 5.0f;
    public GameObject tiroPrefab;
    public GameManager gameManager;
    [SerializeField] private Transform gun;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        if(gameManager.gameState == GameManager.GameState.OnPlay)
        {
            Shoot();
            Movimento();   
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
            if(Mouse.current.leftButton.wasPressedThisFrame)
            {
            Instantiate(tiroPrefab, gun.position, Quaternion.Euler(0, 0, 180));
            }
        }
    }

    public void GanharXp (int xpGanho)
    {
        xp = xp + xpGanho;

        if (xp >= 100)
        {
            level = level + 1;
            xp = xp - 100;
        }
    }

    void Movimento()
        {
            if (Keyboard.current.wKey.isPressed)
        {
            transform.position = transform.position + new Vector3 (0, 1, 0) * velocidade * Time.deltaTime;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            transform.position = transform.position + new Vector3 (-1, 0, 0) * velocidade * Time.deltaTime;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            transform.position = transform.position + new Vector3 (0, -1, 0) * velocidade * Time.deltaTime;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            transform.position = transform.position + new Vector3 (1, 0, 0) * velocidade * Time.deltaTime;
        }
        }
}
