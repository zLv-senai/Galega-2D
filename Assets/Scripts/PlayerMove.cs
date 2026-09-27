using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour, IDamageable
{
    public int vida = 100;
    public int level = 1;
    private int xp = 0;
    public float velocidade = 5.0f;
    public GameManager gameManager;
    [SerializeField] private Transform gun;
    public GameObject tiroPrefab;
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
            if(Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

                Vector2 direction = ((Vector2)mouseWorld - (Vector2)transform.up).normalized;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                GameObject tiro = Instantiate(tiroPrefab, gun.position, Quaternion.Euler(0f, 0f, angle + (-90f)));

                Rigidbody2D rb = tiro.GetComponent<Rigidbody2D>();
                rb.linearVelocity = direction * velocidade;
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
