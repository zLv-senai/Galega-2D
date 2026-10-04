using UnityEngine;
 
[ExecuteAlways]
public class ZigZagMovement : MonoBehaviour
{
[Header("Movimento")]
public float speed = 5f;
public float zigZagAmplitude = 2f;
public float zigZagFrequency = 3f;
 
private float startX;
 
void Start()
{
startX = transform.position.x;
}
 
void Update()
{
// Movimento para frente no eixo Y

 
// Oscilação no eixo X
float newX = startX + Mathf.Sin(Time.time * zigZagFrequency) * zigZagAmplitude;
 
transform.position = new Vector3(
newX,
transform.position.y,
transform.position.z
);
}


}