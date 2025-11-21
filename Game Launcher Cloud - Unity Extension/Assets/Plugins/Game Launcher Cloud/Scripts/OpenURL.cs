using System.Collections;
using UnityEngine;

namespace GameLauncherCloud
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
