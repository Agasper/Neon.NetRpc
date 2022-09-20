using System;
using System.Collections;
using System.Collections.Generic;
using Neon.ClientExample.Net.Realtime;
using Neon.ServerExample.Proto;
using UnityEngine;

namespace Neon.ClientExample.Game.Realtime
{
    //Draw players circles on the screen and sends input updates
    public class RoomController : MonoBehaviour
    {
        [SerializeField] RoomObject roomObjectPrefab;
        [SerializeField] SpriteRenderer roomBg;

        RoomModel model;
        HashSet<int> cachedSet;
        HashSet<int> cachedSetObjects;

        Dictionary<int, RoomObject> objects;

        void Awake()
        {
            roomBg.color = new Color(1, 1, 1, 0.25f);
            cachedSet = new HashSet<int>();
            cachedSetObjects = new HashSet<int>();
            objects = new Dictionary<int, RoomObject>();
            roomObjectPrefab.gameObject.SetActive(false);
            RealtimeClient.Instance.Model.OnRoomJoined.AddListener(OnRoomJoined);
            RealtimeClient.Instance.Model.OnRoomLeft.AddListener(OnRoomLeft);

            StartCoroutine(Tick());
        }
        
        void OnDestroy()
        {
            //unsubscribe on destroy
            RealtimeClient.Instance.Model?.OnRoomJoined.RemoveListener(OnRoomJoined);
            RealtimeClient.Instance.Model?.OnRoomLeft.RemoveListener(OnRoomLeft);
            if (this.model != null)
                this.model.OnRoomStateUpdate.RemoveListener(OnRoomStateUpdate);
        }
        
        void OnRoomJoined(RoomModel model)
        {
            roomBg.color = new Color(1, 1, 1, 1);
            this.model = model;
            //subscribe on room update
            this.model.OnRoomStateUpdate.AddListener(OnRoomStateUpdate);
        }
        
        void OnRoomLeft()
        {
            roomBg.color = new Color(1, 1, 1, 0.25f);
            this.model?.OnRoomStateUpdate.RemoveListener(OnRoomStateUpdate);
            this.model = null;

            //Destroying all players
            foreach (var pair in objects)
            {
                Destroy(pair.Value.gameObject);
            }
            
            objects.Clear();
        }

        void OnRoomStateUpdate(RoomStateProto roomStateProto)
        {
            try
            {

                for (int i = 0; i < roomStateProto.Players.Count; i++)
                {
                    var playerProto = roomStateProto.Players[i];
                    if (objects.TryGetValue(playerProto.Id, out RoomObject obj))
                    {
                        //if player exists it updates it position
                        obj.UpdateCoords(new DateTime(roomStateProto.Timestamp),
                            new Vector2(playerProto.X, playerProto.Y));
                    }
                    else
                    {
                        //if not it creates a new one
                        var newPlayer = Instantiate(roomObjectPrefab, roomObjectPrefab.transform.parent);
                        newPlayer.Init(playerProto.Id,
                            model.MyId != playerProto.Id ? Color.yellow : Color.red);
                        newPlayer.UpdateCoords(new DateTime(roomStateProto.Timestamp),
                            new Vector2(playerProto.X, playerProto.Y));
                        objects.Add(playerProto.Id, newPlayer);
                    }

                    //adding actual players to the hashset
                    cachedSet.Add(playerProto.Id);
                }

                foreach (var pair in objects)
                {
                    //adding current players to the hashset
                    cachedSetObjects.Add(pair.Key);
                }
                
                //removing deleted players
                cachedSetObjects.ExceptWith(cachedSet);

                //actually destroying players no more exists in update
                foreach (var idToDelete in cachedSetObjects)
                {
                    if (objects.TryGetValue(idToDelete, out RoomObject obj))
                    {
                        Destroy(obj.gameObject);
                        objects.Remove(idToDelete);
                    }
                }
            }
            finally
            {
                cachedSetObjects.Clear();
                cachedSet.Clear();
            }
        }

        IEnumerator Tick()
        {
            //every 50ms getting a new move vector according to our input
            
            while (true)
            {
                Vector2 moveVector = Vector2.zero;

                if (Input.GetKey(KeyCode.S))
                    moveVector.y = -1;
                if (Input.GetKey(KeyCode.W))
                    moveVector.y = 1;
                if (Input.GetKey(KeyCode.A))
                    moveVector.x = -1;
                if (Input.GetKey(KeyCode.D))
                    moveVector.x = 1;


                moveVector.Normalize();
                moveVector *= 0.02f;

                //if it's not zero, send it
                if (moveVector.sqrMagnitude > 0)
                {
                    ClientMovedMessageProto movedMessageProto = new ClientMovedMessageProto();
                    movedMessageProto.X = moveVector.x;
                    movedMessageProto.Y = moveVector.y;
                    model?.Move(movedMessageProto);
                }

                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}