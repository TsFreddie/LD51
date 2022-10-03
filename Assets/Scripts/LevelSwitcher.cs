public class LevelSwitcher : Switchable
{
    public string NextLevel;
    
    public override void Trigger()
    {
        GameManager.Instance.Finish(NextLevel);
    }
}
