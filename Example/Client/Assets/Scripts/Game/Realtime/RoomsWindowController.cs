using System;
using System.Collections.Generic;
using Neon.ClientExample.Net.Backend;
using Neon.ClientExample.Net.Realtime;
using Neon.ServerExample.Proto;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Realtime
{
    //Window with a list of rooms 
    public class RoomsWindowController : MonoBehaviour
    {
        [SerializeField] CanvasGroup loadingFader;
        [SerializeField] CanvasGroup windowCanvasGroup;
        [SerializeField] Button closeButton;
        [SerializeField] Button createButton;
        [SerializeField] RoomView roomView;

        List<RoomView> spawnedRooms;
        
        void Awake()
        {
            spawnedRooms = new List<RoomView>();
            
            Hide(); //hide on start
            
            closeButton.onClick.AddListener(Hide);
            createButton.onClick.AddListener(OnCreateClick);
            
            roomView.gameObject.SetActive(false);
        }

        //disabling UI in case of long request
        void SetUiEnabled(bool value)
        {
            closeButton.interactable = value;
            createButton.interactable = value;

            for (int i = 0; i < spawnedRooms.Count; i++)
            {
                spawnedRooms[i].SetEnabled(value);
            }
        }

        //enabling/disabling loading panel
        void SetLoading(bool value)
        {
            loadingFader.alpha = value ? 1 : 0;
            loadingFader.interactable = value;
            loadingFader.blocksRaycasts = value;
        }

        //getting & building the rooms list from the server
        async void GetRooms()
        {
            SetLoading(true);
            SetUiEnabled(false);

            try
            {
                //destroying all existing rooms
                for (int i = 0; i < spawnedRooms.Count; i++)
                {
                    Destroy(spawnedRooms[i].gameObject);
                }

                spawnedRooms.Clear();

                //getting a new list
                var rooms = await BackendClient.Instance.ProfileModel.GetRooms();

                //creating gameobjects
                for (int i = 0; i < rooms.Rooms.Count; i++)
                {
                    var room = rooms.Rooms[i];
                    GameObject newInstance = Instantiate(roomView.gameObject, roomView.transform.parent);
                    RoomView newInstanceComponent = newInstance.GetComponent<RoomView>();
                    newInstanceComponent.UpdateInfo(room, OnJoinClick);
                    newInstance.SetActive(true);
                    spawnedRooms.Add(newInstanceComponent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Instance.Show($"Couldn't get rooms: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
                SetUiEnabled(true);
            }
        }

        void OnDestroy()
        {
            //unsubscribe on destroy
            closeButton.onClick.RemoveListener(Hide);
            createButton.onClick.RemoveListener(OnCreateClick);
        }

        //on room join button click
        async void OnJoinClick(RealtimeRoomProto roomProto)
        {
            SetLoading(true);
            SetUiEnabled(false);

            try
            {
                await RealtimeClient.Instance.Model.JoinRoom(roomProto);
            }
            catch(Exception ex)
            {
                MessageBox.Instance.Show($"Couldn't join the room: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
                SetUiEnabled(true);
                Hide();
            }
        }

        //show the window
        public void Show()
        {
            windowCanvasGroup.alpha = 1;
            windowCanvasGroup.interactable = true;
            windowCanvasGroup.blocksRaycasts = true;
            GetRooms();
        }

        //hide the window
        public void Hide()
        {
            windowCanvasGroup.alpha = 0;
            windowCanvasGroup.interactable = false;
            windowCanvasGroup.blocksRaycasts = false;
        }
        
        //on create room button click
        async void OnCreateClick()
        {
            SetUiEnabled(false);

            try
            {
                await BackendClient.Instance.ProfileModel.CreateRoom();
            }
            catch (Exception ex)
            {
                SetUiEnabled(true);
                MessageBox.Instance.Show($"Couldn't create room: {ex.Message}");
                return;
            }

            //update the rooms list afterwards
            GetRooms();
        }
    }
}