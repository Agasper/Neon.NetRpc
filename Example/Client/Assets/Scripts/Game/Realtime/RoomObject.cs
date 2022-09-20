using System;
using UnityEngine;

namespace Neon.ClientExample.Game.Realtime
{
    //Room object represents a view for a server's player
    public class RoomObject : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] TextMesh text;

        //sets the id and color
        public void Init(int id, Color color)
        {
            gameObject.SetActive(true);
            text.text = id.ToString();
            spriteRenderer.color = color;
        }

        //updates position on the screen
        public void UpdateCoords(DateTime timestamp, Vector2 coords)
        {
            Vector3 position = transform.localPosition;
            position.x = Mathf.Lerp(-0.45f, 0.45f, coords.x);
            position.y = Mathf.Lerp(-0.45f, 0.45f, coords.y);

            transform.localPosition = position;
        }
    }
}