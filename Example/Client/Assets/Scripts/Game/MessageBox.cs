using System;
using Neon.ClientExample.Net.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Neon.ClientExample.Game
{
    //Simple in game message box
    public class MessageBox : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Text text;
        [SerializeField] Canvas canvas;

        public static MessageBox Instance => instance;

        static MessageBox instance;

        GameAction callback;

        void Awake()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            button.onClick.AddListener(OnOkClick);
            canvas.enabled = false;
        }

        void OnDestroy()
        {
            button.onClick.RemoveListener(OnOkClick);
        }

        void OnOkClick()
        {
            canvas.enabled = false;
            callback?.Invoke();
        }

        public void Show(string message)
        {
            canvas.enabled = true;
            text.text = message;
            callback = null;
        }
        
        public void Show(string message, GameAction callback)
        {
            this.callback = callback;
            canvas.enabled = true;
            text.text = message;
        }
    }
}