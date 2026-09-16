namespace WaterConveyorSort.InputHandling
{
    public interface IInputReceiver
    {
        bool CanReceiveInput { get; }
        void OnClick();
    }
}
