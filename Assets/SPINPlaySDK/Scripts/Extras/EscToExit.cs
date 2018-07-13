using UnityEngine;

namespace Pixvana.Extras
{
    public class EscToExit : MonoBehaviour
    {

        void Update ()
        {
        
            // Esc to exit
            if (Input.GetKeyDown (KeyCode.Escape)) {

                Application.Quit ();
            }
        }
    }
}