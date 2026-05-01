using UnityEngine;
using System.Collections.Generic;

public class MazeCell : MonoBehaviour
{
    public bool visited = false;
    public GameObject wallNorth, wallSouth, wallEast, wallWest;
    public GameObject floor;
    [Header("Celdas Vecinas")]
    public List<Vector2Int> neighbors = new List<Vector2Int>();

    private void ActiveWallDirection(string direction, bool active)
    {
        switch (direction)
        {
            case "North": if (wallNorth) wallNorth.SetActive(active); break;
            case "South": if (wallSouth) wallSouth.SetActive(active); break;
            case "East":  if (wallEast)  wallEast.SetActive(active);  break;
            case "West":  if (wallWest)  wallWest.SetActive(active);  break;
        }
    }

    public void RemoveWall(string direction)
    {
        ActiveWallDirection(direction, false);
    }

    public void SetWall(GameObject doorPrefab,string direction, int lockCount = 0)
    {
        // Si se especifica un número de candados, agregar el componente Door
        if (lockCount > 0)
        {
            GameObject wall = null;
            switch (direction)
            {
                case "North": wall = wallNorth; break;
                case "South": wall = wallSouth; break;
                case "East":  wall = wallEast;  break;
                case "West":  wall = wallWest;  break;
            }

            if (wall != null)
            {
                wall.SetActive(false); // Desactivar la pared existente
                // Reemplazar la pared con una puerta
                Vector3 position = wall.transform.position;
                Quaternion rotation = Quaternion.identity;
                switch (direction)
                {
                    case "North": rotation = Quaternion.Euler(0, 180, 0); break;
                    case "South": rotation = Quaternion.Euler(0, 0, 0); break;
                    case "East":  rotation = Quaternion.Euler(0, 270, 0); break;
                    case "West":  rotation = Quaternion.Euler(0, 90, 0); break;
                }
                GameObject doorInstance = Instantiate(doorPrefab, position, rotation, transform);
                Door doorScript = doorInstance.GetComponent<Door>();
                if (doorScript != null)
                {
                    doorScript.Initiate(lockCount);
                }
                Debug.Log(direction + " wall replaced with door having " + lockCount + " locks.");
            }
        }
    }
}