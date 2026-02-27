using UnityEngine;

namespace Start_Menu
{
    public class rotate : MonoBehaviour
    {
        public Vector3 rotationSpeed = new Vector3(0, 0, 10); 

        void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}