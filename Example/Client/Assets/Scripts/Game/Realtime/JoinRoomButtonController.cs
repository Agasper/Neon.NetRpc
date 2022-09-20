using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    public class JoinRoomButtonController : MonoBehaviour
    {
        [SerializeField] Button joinRoomButton;
        [SerializeField] RoomsWindowController roomsWindowController;

        void Start()
        {
            joinRoomButton.onClick.AddListener(OnClick);
            RealtimeClient.Instance.Model.OnRoomJoined.AddListener(OnRoomJoined);
            RealtimeClient.Instance.Model.OnRoomLeft.AddListener(OnRoomLeft);
        }

        void OnDestroy()
        {
            joinRoomButton.onClick.RemoveListener(OnClick);
            RealtimeClient.Instance.Model?.OnRoomJoined.RemoveListener(OnRoomJoined);
            RealtimeClient.Instance.Model?.OnRoomLeft.RemoveListener(OnRoomLeft);
        }
        
        
        void OnRoomJoined(RoomModel model)
        {
            joinRoomButton.interactable = false;
        }
        
        void OnRoomLeft()
        {
            joinRoomButton.interactable = true;
        }

        void OnClick()
        {
            roomsWindowController.Show();
        }
    }
}