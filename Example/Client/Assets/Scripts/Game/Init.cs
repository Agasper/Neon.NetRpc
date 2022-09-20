using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neon.ClientExample.Game
{
    //Just loads the next scene
    public class Init : MonoBehaviour
    {
        void Awake()
        {
            SceneManager.LoadScene("start");
        }
    }
}