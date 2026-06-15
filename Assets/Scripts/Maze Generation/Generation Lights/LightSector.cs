using System.Collections.Generic;
using UnityEngine;

public class LightSector
{
    public int sectorId;

    public List<LightController> lights = new List<LightController>();
    public bool powered;

    public LightSector(int id)
    {
        sectorId = id;
    }

    public void SetPower(bool state)
    {
        powered = state;
        foreach (LightController light in lights)
            if (light != null)
                light.SetLightState(state);
    }
}