using System;
using Neon.ClientExample.Net.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    public class AddBotButtonController : MonoBehaviour
    {
        [SerializeField] Button addBotButton;

        RoomModel model;

        void Start()
        {
            addBotButton.interactable = false;
            addBotButton.onClick.AddListener(OnClick);
            RealtimeClient.Instance.Model.OnRoomJoined.AddListener(OnRoomJoined);
            RealtimeClient.Instance.Model.OnRoomLeft.AddListener(OnRoomLeft);
        }

        void OnDestroy()
        {
            addBotButton.onClick.RemoveListener(OnClick);
            RealtimeClient.Instance.Model?.OnRoomJoined.RemoveListener(OnRoomJoined);
            RealtimeClient.Instance.Model?.OnRoomLeft.RemoveListener(OnRoomLeft);
        }
        
        void OnRoomJoined(RoomModel model)
        {
            this.model = model;
            addBotButton.interactable = true;
        }
        
        void OnRoomLeft()
        {
            this.model = null;
            addBotButton.interactable = false;
        }

        async void OnClick()
        {
            addBotButton.interactable = false;
            try
            {
                if (model == null)
                    throw new Exception("We're not joined to any room");
                await model.AddBot();
            }
            catch (Exception ex)
            {
                MessageBox.Instance.Show(ex.Message);
            }
            finally
            {
                addBotButton.interactable = true;
            }
        }
    }
}