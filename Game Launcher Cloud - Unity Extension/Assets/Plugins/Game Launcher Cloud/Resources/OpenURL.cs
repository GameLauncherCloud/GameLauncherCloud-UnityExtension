using System.Collections;
using UnityEngine;

namespace GameLauncher
{
    public class OpenURL : MonoBehaviour
    {
        public string URL;

        public void Open ()
        {
            Application.OpenURL (URL);
        }
    }
}
