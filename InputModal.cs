using Godot;
using GodotTask;
using Serilog;

namespace DotNetInputManager;

public partial class InputModal : Popup
{
    public InputEvent RecordedAction { get; private set; }

    [Export] private Label Prompt { get; set; }

    private string _defaultTitle;
    private string _defaultPrompt;

    public override void _Ready()
    {
        base._Ready();
        _defaultTitle = Title;

        if (Prompt is not null)
        {
            _defaultPrompt = Prompt.Text;
        }
    }

    public void OpenModal(string title = null,
        string promptText = null, bool exclusive = false)
    {
        Title = title ?? _defaultTitle;

        if (Prompt != null) Prompt.Text = promptText ?? _defaultPrompt;

        Exclusive = exclusive;

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
    }

    public async GDTask WaitForModalAction() => await ToSignal(this, "popup_hide");
}