using System;
using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    public class RemoveBotButtonController : MonoBehaviour
    {
        [SerializeField] Button removeBotButton;

        RoomModel model;
        
        void Start()
        {
            removeBotButton.interactable = false;
            removeBotButton.onClick.AddListener(OnClick);
            RealtimeClient.Instance.Model.OnRoomJoined.AddListener(OnRoomJoined);
            RealtimeClient.Instance.Model.OnRoomLeft.AddListener(OnRoomLeft);
        }

        void OnDestroy()
        {
            removeBotButton.onClick.RemoveListener(OnClick);
            RealtimeClient.Instance.Model?.OnRoomJoined.RemoveListener(OnRoomJoined);
            RealtimeClient.Instance.Model?.OnRoomLeft.RemoveListener(OnRoomLeft);
        }
        
        void OnRoomJoined(RoomModel model)
        {
            this.model = model;
            removeBotButton.interactable = true;
        }
        
        void OnRoomLeft()
        {
            this.model = null;
            removeBotButton.interactable = false;
        }

        async void OnClick()
        {
            removeBotButton.interactable = false;
            try
            {
                if (model == null)
                    throw new Exception("We're not joined to any room");
                await model.RemoveBot();
            }
            catch (Exception ex)
            {
                MessageBox.Instance.Show(ex.Message);
            }
            finally
            {
                removeBotButton.interactable = true;
            }
        }
    }
}