using System;
using UnityEngine;
using Yarn.Unity;

public class MainCharacterMovement : CharacterMovement
{
    // Assumed velocity for calculating sound loudness when moving
    const float assumedWalkingVelocity = 10f;

    new void Awake()
    {
        base.Awake();
        isMainCharacter = true;
    }


    new void Start()
    {
        base.Start();
        canSprint = true;
    }

    new void Update()
    {
        // Having a target transfor for the main character effectively overrides any input,
        // as this only happens in cutscenes
        if (TargetTransform != null)
        {
            HandleTarget();
        }
        // Otherwise, we do normal input-based movement
        else
        {
            sprinting = InputManager.Player.Sprint.ReadValue<float>() > 0.5f;
            HandleInput();
        }
        EmitSounds();
        base.Update();
    }

    private void HandleTarget()
    {
        orientation.rotation = Quaternion.LookRotation(TargetDirection);
        desiredMovement = Vector3.forward;
    }

    public void EmitSounds()
    {
        // Play sounds if moving through terrain grass details
        var grasses = TerrainSampler.Instance.SampleTerrainGrassAt(transform.position);
        foreach (var (grassName, amount) in grasses)
        {
            var densityScale = TerrainSampler.Instance.DetailDensityScale(amount); 
            var velocityScale = Mathf.Min(Rigidbody.velocity.sqrMagnitude / assumedWalkingVelocity, 1.0f);

            // Sound volume is based on both grass density and velocity through them
            var volume = densityScale * velocityScale;
            if (volume < 0.1f)
            {
                continue;
            }
            if (!AudioManager.Instance.GetParameterValue(typeof(Sounds.Terrain), grassName, out int parameter))
            {
                Debug.LogWarning($"Missing sound parameter for terrain detail '{grassName}'");
            }
            else
            {
                AudioManager.Instance.PlaySound(
                    eventPath: "event:/SFX/Player/terrain_shuffle",
                    volume: volume, parameterName: "ShuffleMaterial", parameterValue: parameter);
            }
        }
    }

    [YarnCommand("EnableSprinting")]
    public void EnableSprinting(bool enable)
    {
        canSprint = enable;
    }

    private void HandleInput()
    {
        if (InputManager.Player.Jump.WasPerformedThisFrame())
        {
            Jump();
        }
        Vector2 input = InputManager.Player.Move.ReadValue<Vector2>();

        if(input == Vector2.zero)
        {
            desiredMovement = Vector3.zero;
        }
        else
        {
            desiredMovement = Vector3.forward;
        }
        orientation.LookAt(orientation.position + new Vector3(input.x, 0, input.y));
    }

}
