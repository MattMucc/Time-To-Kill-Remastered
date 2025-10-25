using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public RoomManager room;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            room.StartRoom();
            gameObject.SetActive(false); // Prevents retriggering just in case
        }
    }
}