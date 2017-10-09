using UnityEngine;
using System.Collections;

public class PoolableLoopedParticles : PoolableParticles
{
    protected override bool CanReturn()
    {
        bool shouldEmit = timeCounter < particleLength - this.ps.main.startLifetime.constant;

        if (shouldEmit != this.ps.emission.enabled)
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
        }

        return timeCounter > particleLength;
    }
}