using Godot;
using GodotTask;

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
        
        switch(@event)
        {
            // Only register full press on triggers
            case InputEventJoypadMotion { Axis: JoyAxis.TriggerLeft or JoyAxis.TriggerRight, AxisValue: < 1.0f }:
                
            // Only register sticks that favour a direction
            case InputEventJoypadMotion { AxisValue: > -0.75f and < 0.75f}:
                return;
        }
        
        switch(_inputType)
        {
            case GenericInputType.KeyboardAndMouse when @event is not InputEventKey and not InputEventMouseButton:
            case GenericInputType.Controller when @event is not InputEventJoypadButton and not InputEventJoypadMotion:
                return;
        }
        
        RecordedAction = @event;
        GD.Print( "Event recorded from Input Modal: {Event}", @event);
        if(@event is InputEventJoypadMotion joypadMotion)
        {
            GD.Print( "Axis recorded: {Axis}", joypadMotion.Axis.ToString());
            GD.Print( "AxisValue recorded: {AxisValue}", joypadMotion.AxisValue);
        }
        
        Hide();
        EmitSignal(SignalName.ModalHide);
    }

    public async GDTask WaitForModalAction() => await ToSignal(this, SignalName.ModalHide);
}