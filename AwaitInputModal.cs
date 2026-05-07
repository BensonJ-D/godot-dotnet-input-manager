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

    public void OpenModal(string title, string prompt)
    {
        Title.Text = title;
        Prompt.Text = prompt;
        
        Show();
        RecordedAction = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (!@event.IsPressed() || @event.IsEcho())
            return;

        RecordedAction = @event;
        Log.Debug( "Event recorded from Input Modal: {Event}", @event);
        
        Hide();
        EmitSignal(SignalName.ModalHide);
    }

    public async GDTask WaitForModalAction() => await ToSignal(this, SignalName.ModalHide);
}