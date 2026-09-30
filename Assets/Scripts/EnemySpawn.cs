using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawn: MonoBehaviour
{
    public Camera mainCamera;
    private float maxX, maxY;

    [SerializeField] public float rayRange = 5f;

    [SerializeField] public Vector2 direcaoRay = Vector2.up;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnEnemy();
        }

        Debug.DrawRay(transform.position, direcaoRay * rayRange, Color.red);
    }

    public Vector2 SpawnPosition()
    {
        float xPos = UnityEngine.Random.Range(-maxX, maxX);
        float yPos = UnityEngine.Random.Range(-maxY, maxY);
        Vector2 spawnPoint = new Vector2(xPos, yPos) * rayRange;
        Debug.Log(spawnPoint);
        return spawnPoint;
    }

    public void SpawnEnemy()
    {
        GameObject Enemy = Resources.Load<GameObject>("Enemy");
        Vector2 spawnPoint = SpawnPosition();
        Instantiate(Enemy, spawnPoint, quaternion.identity);
    }
}