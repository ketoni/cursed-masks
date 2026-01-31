using UnityEngine;
using DG.Tweening;
using Cuts = CutsceneManager;

public class SceneWarpEvent : InteractionEvent
{
    [SerializeField] ParticleSystem fogTransitionSystem;
    [SerializeField] Transform walkInLocation;
    [SerializeField] Transform teleportLocation;
    [SerializeField] Transform walkOutLocation;
    [SerializeField] float startWaitTime = 3f;
    [SerializeField] float endWaitTime = 2f;
    [SerializeField] string yarnNode;

    protected override void Execute(InteractionContext context, string arg)
    {
        // If we are autowalking through the other barrier while teleporting, don't execute.
        // We can be sure of this by checking whether player controls are enabled
        if (!InputManager.Player.enabled) return;

        Debug.Log("Set a course for other side of the forest. Maximum warp. Engage! -Picard");
        var effect = fogTransitionSystem.gameObject.GetComponent<StayInFrontOfCamera>();
        effect.followCam = true;

        Color temp = Color.black;
        temp.a = 0;

        void moveWalkInLocation()
        {
            // Moves the walk-in location so that Tytti keeps walking in
            // the direction they are facing at the point of contact
            var facing = GameManager.McMovement.orientation.forward;
            var walkInPosition = GameManager.McTransform.position + facing.normalized * 20f;
            walkInLocation.position = walkInPosition;
        }

        // GOMU GOMU NO, FOG MAN
        fogTransitionSystem.Play();
        Cuts.Sequence(disableControls: true)
            .AppendCallback(() => moveWalkInLocation())
            .Append(Cuts.WalkMainTo(walkInLocation))
            .AppendInterval(startWaitTime)
            .Append(Cuts.FadeIn(duration: 0.5f))
            .Append(Cuts.TeleportMC(teleportLocation))
            .AppendCallback(() => DialogueManager.Instance.StartDialogue(yarnNode))
            .Append(Cuts.WalkMainTo(walkOutLocation))
            .Join(Cuts.FadeOut(duration: 1f))
            .AppendInterval(endWaitTime)
            .AppendCallback(() => effect.followCam = false);
    }
}