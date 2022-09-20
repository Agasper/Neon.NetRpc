using System;
using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Lobby
{
    public class PlayButtonController : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button logoutButton;

        void Start()
        {
            playButton.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            playButton.onClick.RemoveListener(OnClick);
        }

        async void OnClick()
        {
            playButton.interactable = false;
            logoutButton.interactable = false;
            try
            {
                await RealtimeClient.Instance.Connect();
                SceneManager.LoadScene("game");
            }
            catch (Exception ex)
            {
                MessageBox.Instance.Show($"Couldn't connect to the realtime server: {ex.Message}");
            }
            finally
            {
                playButton.interactable = true;
                logoutButton.interactable = true;
            }
        }
    }
}