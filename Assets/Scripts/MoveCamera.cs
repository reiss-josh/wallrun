using UnityEngine;

//on every frame, move the camera to the same position as the player
//apparently this is good
public class MoveCamera : MonoBehaviour
{
    public Transform player;
    void Update() {
        this.transform.position = player.transform.position;
    }
}