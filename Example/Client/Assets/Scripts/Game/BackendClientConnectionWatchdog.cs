using System;
using Neon.ClientExample.Net.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neon.ClientExample.Game
{
    //The watchdog catches session closing event from the backend client and shows the message leading you to the start scene
    public class BackendClientConnectionWatchdog : MonoBehaviour
    {
        void Start()
        {
            BackendClient.Instance.OnSessionClosed.AddListener(OnSessionClosed);
        }

        void OnDestroy()
        {
            BackendClient.Instance.OnSessionClosed.RemoveListener(OnSessionClosed);
        }

        void OnSessionClosed()
        {
            MessageBox.Instance.Show("Connection terminated!", () => SceneManager.LoadScene("start"));
        }
    }
}