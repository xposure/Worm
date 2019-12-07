public struct SpriteAnimation
{
    public uint AnimationID;

    public bool Enabled;

    public int AnimationIndex;

    public float Timer;

    public float FPS;

    public SpriteAnimation(uint animationID, bool enabled = false, int animationIndex = 0, float timer = 0, float fps = 1f / 15)
    {
        AnimationID = animationID;
        Enabled = enabled;
        AnimationIndex = animationIndex;
        Timer = timer;
        FPS = fps;
    }
}