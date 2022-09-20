using System;
using Neon.ClientExample.Net.Backend;
using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neon.ClientExample.Game
{
    //The watchdog catches session closing event from the realtime client and shows the message leading you to the start scene
    public class RealtimeClientConnectionWatchdog : MonoBehaviour
    {
        void Start()
        {
            RealtimeClient.Instance.OnSessionClosed.AddListener(OnSessionClosed);
        }

        void OnDestroy()
        {
            RealtimeClient.Instance.OnSessionClosed.RemoveListener(OnSessionClosed);
        }

        void OnSessionClosed()
        {
            MessageBox.Instance.Show("Connection terminated!", () => SceneManager.LoadScene("start"));
        }
    }
}