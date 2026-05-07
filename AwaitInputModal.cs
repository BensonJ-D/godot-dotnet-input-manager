using Godot;
using GodotTask;
using Serilog;

namespace DotNetInputManager;

public partial class AwaitInputModal : PanelContainer
{
    public InputEvent RecordedAction { get; private set; }
    
    [Signal]
    private delegate void ModalHideEventHandler();
    
    [Export] private Label Title { get; set; }
    [Export] private Label Prompt { get; set; }
    private GenericInputType _inputType;

    public void OpenModal(string title, string prompt, GenericInputType inputType)
    {
        Title.Text = title;
        Prompt.Text = prompt;
        _inputType = inputType;
        
        Show();
        RecordedAction = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (!@event.IsPressed() || @event.IsEcho())
            return;
        
        if(@event is InputEventJoypadMotion { Axis: JoyAxis.TriggerLeft or JoyAxis.TriggerRight, AxisValue: < 1.0f })
        {
            return;
        }
        
        switch(_inputType)
        {
            case GenericInputType.Keyboard when @event is not InputEventKey:
            case GenericInputType.Controller when @event is not InputEventJoypadButton and not InputEventJoypadMotion:
            case GenericInputType.Mouse when @event is not InputEventMouseButton:
                return;
        }
        
        RecordedAction = @event;
        Log.Debug( "Event recorded from Input Modal: {Event}", @event);
        
        Hide();
        EmitSignal(SignalName.ModalHide);
    }

    public async GDTask WaitForModalAction() => await ToSignal(this, SignalName.ModalHide);
}