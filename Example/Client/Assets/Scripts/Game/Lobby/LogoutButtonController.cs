using System;
using Neon.ClientExample.Net.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Lobby
{
    public class LogoutButtonController : MonoBehaviour
    {
        [SerializeField] Button logoutButton;

        void Start()
        {
            logoutButton.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            logoutButton.onClick.RemoveListener(OnClick);
        }

        void OnClick()
        {
            BackendClient.Instance.Disconnect();
            SceneManager.LoadScene("start");
        }
    }
}