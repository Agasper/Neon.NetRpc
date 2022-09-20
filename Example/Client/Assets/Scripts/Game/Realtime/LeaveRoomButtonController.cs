using System;
using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    public class LeaveRoomButtonController : MonoBehaviour
    {
        [SerializeField] Button leaveRoomButton;

        void Start()
        {
            leaveRoomButton.interactable = false;
            leaveRoomButton.onClick.AddListener(OnClick);
            RealtimeClient.Instance.Model.OnRoomJoined.AddListener(OnRoomJoined);
            RealtimeClient.Instance.Model.OnRoomLeft.AddListener(OnRoomLeft);
        }

        void OnDestroy()
        {
            leaveRoomButton.onClick.RemoveListener(OnClick);
            RealtimeClient.Instance.Model?.OnRoomJoined.RemoveListener(OnRoomJoined);
            RealtimeClient.Instance.Model?.OnRoomLeft.RemoveListener(OnRoomLeft);
        }
        
        void OnRoomJoined(RoomModel model)
        {
            leaveRoomButton.interactable = true;
        }
        
        void OnRoomLeft()
        {
            leaveRoomButton.interactable = false;
        }

        async void OnClick()
        {
            try
            {
                await RealtimeClient.Instance.Model.LeaveRoom();
            }
            catch (Exception ex)
            {
                MessageBox.Instance.Show(ex.Message);
            }
        }
    }
}