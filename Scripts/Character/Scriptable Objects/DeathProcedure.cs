using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "DeathProcedure", menuName = "Scriptable Objects/DeathProcedure")]
public class DeathProcedure : ScriptableObject
{
    //public ScriptableRendererFeature fullScreenFlash; //I need to figure out how to access the full screen render feature and not have it dirtied after runtime
    //public ScriptableRendererFeature combatantDisplay; //I need to figure out how to access the full screen render feature and not have it dirtied after runtime
    public float deathMultiplier;
    public float deathSlowScale;
    public float deathFlashTime;
    public float deathSlowTime;
    public bool ragdoll;

    /*
    public void ResetRenderFeatures()
    {
        if (fullScreenFlash is not null) { fullScreenFlash.SetActive(false); }
        if (combatantDisplay is not null) { combatantDisplay.SetActive(false); }
    }
    //*/
}
