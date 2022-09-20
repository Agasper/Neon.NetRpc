using System;
using Neon.ClientExample.Net.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Lobby
{
    public class AddMoneyButtonController : MonoBehaviour
    {
        [SerializeField] Button addMoneyButton;

        PlayerProfileModel model;

        void Start()
        {
            model = BackendClient.Instance.ProfileModel;
            addMoneyButton.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            addMoneyButton.onClick.RemoveListener(OnClick);
        }

        async void OnClick()
        {
            addMoneyButton.interactable = false;
            try
            {
                await model.AddMoney();
            }
            finally
            {
                addMoneyButton.interactable = true;
            }
        }
    }
}