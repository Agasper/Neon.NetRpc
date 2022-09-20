using System;
using Neon.ClientExample.Net.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game.Lobby
{
    public class MoneyView : MonoBehaviour
    {
        [SerializeField] Text moneyText;

        PlayerProfileModel model;

        float currentMoneyAmount;
        int desiredMoneyAmount;
        
        void Start()
        {
            //subscribing to the player's model property
            model = BackendClient.Instance.ProfileModel;
            //if it changes, it will raise the event
            model.Money.AddListener(OnMoneyChanged);

            currentMoneyAmount = model.Money.Value;
            desiredMoneyAmount = model.Money.Value;
            moneyText.text = model.Money.Value.ToString();
        }

        void OnDestroy()
        {
            //unsubscribe on destroy
            model.Money.RemoveListener(OnMoneyChanged);
        }

        void OnMoneyChanged(int amount)
        {
            desiredMoneyAmount = amount;
        }

        void Update()
        {
            //for animation purposes
            currentMoneyAmount = Mathf.Lerp(currentMoneyAmount, desiredMoneyAmount, Time.deltaTime*10);
            moneyText.text = Mathf.RoundToInt(currentMoneyAmount).ToString();
        }
    }
}