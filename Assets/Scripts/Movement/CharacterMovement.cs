using System;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkForce;
    public float sprintForce;
    public bool canSprint = true;
    public bool sprinting; 
    [SerializeField] private float maximumSpeed;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airControlMultiplier;

    [Header("Ground Stuff")]
    [SerializeField] Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.5f;

    [Header("Character Related")]
    [NullWarn] public Transform orientation;
    public float characterHeight;
    public new SpriteAnimationHandler animation;
    public bool IsAnimated => animation != null && animation.enabled;

    [Header("Following and avoiding")]
    [SerializeField, InspectorReadOnly] Transform targetTransform; // Editor only
    const float MIN_TARGET_DISTANCE = 0.5f;
    [SerializeField, Min(MIN_TARGET_DISTANCE)] protected float targetDistanceLimit;
    public Transform TargetTransform { get; private set; } 
    public Vector3 TargetDirection
    {
        get
        {
            if (TargetTransform == null)
            {
                Debug.LogWarning(
                    "TargetDirection is being used without active target! " +
                    "Ensure SetTarget() is called before this and ClearTarget() is not called too early.", this);
                return Vector3.zero;
            }
            return TargetTransform.position - transform.position;
        }
    }
    public float TargetDistance => TargetDirection.magnitude;
    public bool TargetInDistance => TargetDistance < targetDistanceLimit;

    // Public interface for applying movement
    [HideInInspector] public Vector3 desiredMovement;

    // Other privates
    private Vector3 movement;
    float moveScaler;
    public Rigidbody Rigidbody { get; protected set;}
    public Collider Collider { get; protected set; }
    bool canJump = true;
    protected bool grounded;
    private int originalLayer;
    private Camera mainCamera;
    protected bool isMainCharacter = false;

    protected void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.freezeRotation = true;
        Collider = GetComponent<Collider>();
        originalLayer = gameObject.layer;
    }

    protected void Start()
    {
        // The SpriteAnimator reference can be set from the inspector, or automatically searched for
        if (animation == null)
        {
            animation = GetComponentInChildren<SpriteAnimationHandler>();
        }
        mainCamera = Camera.main;
    }

    protected void Update()
    {
        // State handling 
        if (groundCheck == null)
        {
            grounded = true;
        }
        else
        {
            grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        }
        StateHandler();
        UpdateAnimator();

    #if UNITY_EDITOR
        targetTransform = TargetTransform;
    #endif
    }
    protected void FixedUpdate()
    {
        MoveCharacter();
    }

    public bool Jump(Vector3 direction = default, bool allowAirJump = false)
    {
        // Makes the character jump if it is able able.
        // Returns whether the character could perform the jump.
        if (!(canJump && (grounded || allowAirJump)))
        {
            return false;
        }
        canJump = false;

        // Reset Y velocity on jump so you always jump same height
        Rigidbody.velocity = new Vector3(Rigidbody.velocity.x, 0f, Rigidbody.velocity.z);
        Rigidbody.AddForce((transform.up + direction) * jumpForce, ForceMode.Impulse);

        // Reset jump after a cooldown
        Invoke(nameof(ResetJump), jumpCooldown);

        return true;
    }

    private void StateHandler()
    {
        if (grounded && sprinting && canSprint)
        {
            moveScaler = sprintForce;
        }
        else
        {
            moveScaler = walkForce;
        }
    }

    // This function moves the player according to inputs
    private void MoveCharacter()
    {
        if (Rigidbody.isKinematic) return;

        // Move the character based on the input strength and the direction they are looking at 
        movement = desiredMovement.x * orientation.right + desiredMovement.y * orientation.up + desiredMovement.z * orientation.forward;

        // Take camera angle into account
        float angleDivider = math.sin(mainCamera.transform.rotation.eulerAngles.x * Mathf.Deg2Rad);
        if (movement.z != 0)
        {
            movement = new Vector3(movement.x, movement.y, movement.z / angleDivider); // affects z pos
        }

        if (grounded)
        {
            Rigidbody.AddForce(movement * moveScaler, ForceMode.Force);
        }
        else if (!grounded && isMainCharacter)
        {
            Rigidbody.AddForce(airControlMultiplier * movement * moveScaler, ForceMode.Force);
        }
        else if (!grounded && !isMainCharacter)
        {
            // Commented out so that non-main characters can't move in the air!
            // This makes so that e.g. deers wont turn into space rockets :D:D 
            //rb.AddForce(airControlMultiplier * moveScaler * movement, ForceMode.Force);
        }

        Vector3 xzVelocity = Rigidbody.velocity;
        xzVelocity.y = 0;

        // Limits horizontal movement speed so that we dont for ex. slide down slopes super fast
        var speedCap = sprinting ? maximumSpeed * 2 : maximumSpeed;
        if (xzVelocity.magnitude > speedCap)
        {
            Rigidbody.velocity = xzVelocity.normalized * speedCap + Vector3.up * Rigidbody.velocity.y;
        }
    }

    public void SetTargetDistanceLimit(float distance)
    {
        if (distance < MIN_TARGET_DISTANCE)
        {
            Debug.LogWarning($"Clamping target follow distance to {MIN_TARGET_DISTANCE}", this);;
            distance = MIN_TARGET_DISTANCE;
        }
        targetDistanceLimit = distance;
    }

    public void SetTarget(Transform transform, float limit = -1)
    {
        // Start following given transform whenever outside given distance
        if (transform == null)
        {
            throw new ArgumentException("SetTarget transform should not be null. Did you mean to ClearTarget()?");
        }
        if (limit != -1)
        {
            SetTargetDistanceLimit(limit);
        }
        TargetTransform = transform;
    }

    internal void ClearTarget()
    {
        TargetTransform = null;
    }


    private void ResetJump()
    {
        canJump = true;
    }

    public void UpdateAnimator()
    {
        if (!IsAnimated)
        {
            return;
        }
        animation.horizontal = orientation.forward.z;
        animation.vertical = orientation.forward.x;
        animation.speed = desiredMovement.sqrMagnitude;
        
        // Play rate is based on the current speed of the character
        float playRate = Rigidbody.velocity.magnitude / maximumSpeed;
        // Play rate has to be 1 when not moving (playing idle animation)
        playRate = playRate > 1 || playRate < 0.05f ? 1 : playRate;

        animation.PlaybackRate = sprinting ? 2f * playRate : playRate;
    }

    protected void SetGhosting(bool enabled)
    {
        // Ghosting enables movement through various obstacles which would
        // easily prevent e.g. automatically following something
        if (!Collider)
        {
            Debug.LogWarning("Tried to set ghosting on an object without a collider", this);
            return;
        }
        TerrainSampler.Instance.SetTerrainGhosting(Collider, enabled);
        if (!enabled)
        {
            gameObject.layer = originalLayer;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a line between us and the target
        if (TargetTransform != null)
        {
            Debug.DrawLine(transform.position, TargetTransform.position, Color.cyan);
        }
    }

}
