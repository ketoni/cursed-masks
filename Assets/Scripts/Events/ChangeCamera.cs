using Cinemachine;

public class ChangeCamera : InteractionEvent 
{
    public new CinemachineVirtualCamera camera; 
    protected override void Execute(InteractionContext context, string argument)
    {
        CutsceneManager.Cam.SetActiveCamera(camera);
    }
}
