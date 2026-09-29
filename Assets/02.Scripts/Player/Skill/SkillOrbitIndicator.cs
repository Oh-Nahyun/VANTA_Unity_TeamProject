using System;
using UnityEngine;

public class SkillOrbitIndicator : MonoBehaviour
{
    private static readonly int[] PetStartIDs =
    {
        Shader.PropertyToID("_PetStart1"),
        Shader.PropertyToID("_PetStart2"),
        Shader.PropertyToID("_PetStart3"),
        Shader.PropertyToID("_PetStart4"),
        Shader.PropertyToID("_PetStart5")
    };

    private static readonly int[] PetProgressIDs =
    {
        Shader.PropertyToID("_PetProgress1"),
        Shader.PropertyToID("_PetProgress2"),
        Shader.PropertyToID("_PetProgress3"),
        Shader.PropertyToID("_PetProgress4"),
        Shader.PropertyToID("_PetProgress5")
    };

    private static readonly int[] PetActiveIDs =
    {
        Shader.PropertyToID("_PetActive1"),
        Shader.PropertyToID("_PetActive2"),
        Shader.PropertyToID("_PetActive3"),
        Shader.PropertyToID("_PetActive4"),
        Shader.PropertyToID("_PetActive5")
    };

    private void Awake()
    {
        ResetPetRevealShader();

    }

    public void ResetPetRevealShader()
    {
        for (int i = 0; i < 5; i++)
        {
            Shader.SetGlobalFloat(
                PetStartIDs[i],
                0f
            );

            Shader.SetGlobalFloat(
                PetProgressIDs[i],
                0f
            );

            Shader.SetGlobalFloat(
                PetActiveIDs[i],
                0f
            );
        }
    }
    
}
