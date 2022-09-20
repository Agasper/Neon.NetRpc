using System;
using Neon.ClientExample.Net.Util;
using Neon.ServerExample.Proto;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    //Room info card inside the rooms list
    public class RoomView : MonoBehaviour
    {
        [SerializeField] Text guidText;
        [SerializeField] Button joinButton;
        
        RealtimeRoomProto realtimeRoomProto;
        GameAction<RealtimeRoomProto> joinCallback;
        
        //Callback is used for Join room button
        public void UpdateInfo(RealtimeRoomProto realtimeRoomProto, GameAction<RealtimeRoomProto> joinCallback)
        {
            this.realtimeRoomProto = realtimeRoomProto;
            this.joinCallback = joinCallback;
            this.guidText.text = new Guid(realtimeRoomProto.RoomGuid.ToByteArray()).ToString();
        }

        public void SetEnabled(bool value)
        {
            joinButton.interactable = value;
        }

        void Start()
        {
            joinButton.onClick.AddListener(OnClickJoin);
        }

        void OnDestroy()
        {
            joinButton.onClick.RemoveListener(OnClickJoin);
        }

        void OnClickJoin()
        {
            joinCallback.Invoke(realtimeRoomProto);
        }
    }
}