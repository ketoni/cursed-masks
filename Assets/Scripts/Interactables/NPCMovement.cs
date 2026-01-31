using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class NPCMovement : CharacterMovement
{
    public enum MovementBehavior
    {
        STATIC,
        SHUFFLE,
        WANDER,
        FOLLOW,
        AVOID,
        FLEE,
        DELETE
    }

    [Header("Behavior")]
    public MovementBehavior _behavior;

    public float shuffleLength;
    public float shuffleDelay;
    public float shuffleDistance = 50f;
    public float destroySelfTime = 10f;

    float timer;
    private Vector3 behaviorStartPos;

    private new void Start()
    {
        base.Start();

        // Randomize shuffle timer so that similar NPCs dont shuffle in unison
        timer = Random.Range(0, shuffleDelay);

        // Initialize initial behavior
        SetBehavior(_behavior);
    }

    new void Update()
    {
        timer += Time.deltaTime;
        Act();
        base.Update();
    }

    public void SetBehavior(MovementBehavior newBehavior)
    {
        _behavior = newBehavior;
        bool terrainPhasing = false;

        switch (_behavior)
        {
            case MovementBehavior.FOLLOW:
                CheckTarget();
                terrainPhasing = true;
                break;
            case MovementBehavior.AVOID:
                CheckTarget();
                terrainPhasing = true;
                break;
            case MovementBehavior.FLEE:
                sprinting = true;
                terrainPhasing = true;
                break;
            case MovementBehavior.DELETE:
                DestroySelfAfterSeconds(destroySelfTime);
                terrainPhasing = true;
                break;
        }
        behaviorStartPos = TargetTransform != null ? TargetTransform.position : transform.position;
        SetGhosting(terrainPhasing);
    }

    private void Act()
    {
        desiredMovement = Vector3.zero;
        switch (_behavior)
        {
            case MovementBehavior.STATIC:
                // Do nothing
                break;
            case MovementBehavior.SHUFFLE:
                // Move an amount every shuffle period 
                if (timer < shuffleLength)
                {
                    desiredMovement = Vector3.forward;
                }
                else if (timer > shuffleLength + shuffleDelay)
                {
                    BiasedRandomFaceDir(behaviorStartPos, shuffleDistance);
                    timer = 0;
                }
                break;
            case MovementBehavior.FOLLOW:
                // Move towards the target until they are in distance and keep looking at target
                if (!TargetInDistance)
                {
                    desiredMovement = Vector3.forward;
                }
                // Sprint if target is sprinting
                if(canSprint)
                {
                    // Can we be sure that it's always in the parent? Who knows? I'll ask my parents.
                    try
                    {
                        sprinting = TargetTransform.GetComponentInParent<CharacterMovement>().sprinting;
                    }
                    catch (NullReferenceException)
                    {
                        // no parent, too bad.
                    }
                }
                orientation.rotation = Quaternion.LookRotation(TargetDirection);
                break;
            case MovementBehavior.AVOID:
                // Move away from the target if in distance and keep looking away from target
                if (TargetInDistance)
                {
                    desiredMovement = Vector3.forward;
                }
                orientation.rotation = Quaternion.LookRotation(-TargetDirection);
                break;
            case MovementBehavior.FLEE:
                // Always move away from the target and keep looking away.
                // We zero Y so that we don't shoot light objects into the sky lolol
                desiredMovement = Vector3.forward;
                orientation.rotation = Quaternion.LookRotation(-TargetDirection.WithY(0));
                break;
            case MovementBehavior.DELETE:
                // We basically.... DANCE TILL YOU'RE DEAD
                if (timer < shuffleLength)
                {
                    desiredMovement = Vector3.forward;
                }
                else if (timer > shuffleLength + shuffleDelay)
                {
                    BiasedRandomFaceDir(behaviorStartPos, shuffleDistance);
                    timer = 0;
                }
                break;
            default:
                throw new NotImplementedException($"NPCMovement behavior {_behavior} has not been implemented");
        }
    }

    void RandomFaceDir()
    {
        // Randomly rotates the character's orientation around the up-axis
        orientation.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
    }

    // Randomize direction biasing towards biasPosition, based on distance
    // Chance is higher the further away from behavior start pos
    void BiasedRandomFaceDir(Vector3 biasPosition, float distance)
    {
        // Distance to bias position
        float difference = (biasPosition - transform.position).magnitude;

        // If out of range, force orientation back
        if(difference >= distance)
        {
            orientation.LookAt(biasPosition);
        }
        else
        {
            float randomNumber = Random.Range(0, distance);
            // Chance is lower the higher difference is
            if(randomNumber > difference)
            {
                RandomFaceDir();
            }
            else
            {
                // force orientation to bias position
                orientation.LookAt(biasPosition);
            }
        }
    }

    void CheckTarget()
    {
        if (TargetTransform == null)
        {
            Debug.LogError("Follow and Avoid behaviors must have a target!", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a circle around the target 
        if (TargetTransform != null)
        {
            // Circle color is based on behavior: Green is towards, red is away, yellow otherwise
            var circleColor = Color.yellow;
            if (_behavior == MovementBehavior.FOLLOW)
            {
                circleColor = Color.green;
            }
            else if (_behavior == MovementBehavior.AVOID || _behavior == MovementBehavior.FLEE)
            {
                circleColor = Color.red;
            }
            CircleDrawer.DrawCircle(TargetTransform.position, targetDistanceLimit, circleColor);
        }
        if(_behavior == MovementBehavior.SHUFFLE)
        {
            CircleDrawer.DrawCircle(behaviorStartPos, shuffleDistance, Color.magenta);
        }
    }

    public void DestroySelfAfterSeconds(float seconds)
    {
        Destroy(gameObject, seconds);
    }
}