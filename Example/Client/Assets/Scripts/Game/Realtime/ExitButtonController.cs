using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    public class ExitButtonController : MonoBehaviour
    {
        [SerializeField] Button exitButton;

        void Start()
        {
            exitButton.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            exitButton.onClick.RemoveListener(OnClick);
        }

        void OnClick()
        {
            RealtimeClient.Instance.Disconnect();
            SceneManager.LoadScene("lobby");
        }
    }
}