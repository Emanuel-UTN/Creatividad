using UnityEngine;

public class MazeCell : MonoBehaviour
{
    public bool visited = false;
    public GameObject wallNorth, wallSouth, wallEast, wallWest;
    public GameObject floor;

    public void RemoveWall(string direction)
    {
        switch (direction)
        {
            case "North": if (wallNorth) wallNorth.SetActive(false); break;
            case "South": if (wallSouth) wallSouth.SetActive(false); break;
            case "East":  if (wallEast)  wallEast.SetActive(false);  break;
            case "West":  if (wallWest)  wallWest.SetActive(false);  break;
        }
    }
}