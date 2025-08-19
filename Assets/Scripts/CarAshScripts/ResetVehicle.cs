using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashsvp
{
    public class ResetVehicle : MonoBehaviour
    {
        public void ResetVehiclePosition()
        {
            Debug.Log("resrerer");
            var pos = transform.position;
            pos.y += 1;
            transform.position = pos;
            transform.rotation = Quaternion.identity;
        }


        public void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
