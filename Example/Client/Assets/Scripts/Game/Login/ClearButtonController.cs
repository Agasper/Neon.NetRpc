using System;
using Neon.ClientExample.Net;
using Neon.ClientExample.Net.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Login
{
    public class ClearButtonController : MonoBehaviour
    {
        [SerializeField] Text loginText;
        [SerializeField] Button clearButton;

        void Awake()
        {
            clearButton.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            clearButton.onClick.RemoveListener(OnClick);
        }

        void OnClick()
        {
            CredentialsStorage.ClearCredentials();
            loginText.text = "Your login credentials are empty";
        }
    }
}