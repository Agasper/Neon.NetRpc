using System;
using Neon.ClientExample.Net.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Login
{
    public class LoginButtonController : MonoBehaviour
    {
        [SerializeField] Text loginText;
        [SerializeField] Button loginButton;

        void Awake()
        {
            loginButton.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            loginButton.onClick.RemoveListener(OnClick);
        }

        async void OnClick()
        {
            try
            {
                loginButton.interactable = false;
                BackendClient client = BackendClient.Instance;
                loginText.text = "Connecting to the server";
                await client.Connect();
                loginText.text = "Connected!";
                SceneManager.LoadScene("lobby");
            }
            catch(Exception ex)
            {
                loginText.text = ex.Message; 
                loginButton.interactable = true;
                throw;
            }
        }
    }
}