using UnityEngine;

// Acha o objeto raiz do jogador (o que tem o PlayerMove). Não depende só da tag "Player":
// no modelo 3D, filhos como o modelo e o reator também podem estar com essa tag, e o
// FindWithTag devolveria um filho sem PlayerXp/PlayerStats/Empurravel.
public static class Jogador
{
    public static GameObject Encontrar()
    {
        PlayerMove playerMove = Object.FindAnyObjectByType<PlayerMove>();
        if (playerMove != null)
        {
            return playerMove.gameObject;
        }

        // Reserva: pela tag, subindo até a raiz que tem o PlayerMove (se houver).
        GameObject comTag = GameObject.FindWithTag("Player");
        if (comTag == null)
        {
            return null;
        }

        PlayerMove raiz = comTag.GetComponentInParent<PlayerMove>();
        return raiz != null ? raiz.gameObject : comTag;
    }
}
