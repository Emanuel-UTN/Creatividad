using UnityEngine;

public abstract class PuzzleObject : MonoBehaviour
{
    protected PowerPuzzle puzzle;
    
    public virtual void Initialize(PowerPuzzle owner)
    {
        puzzle = owner;
    }

    public abstract void Interact();
}