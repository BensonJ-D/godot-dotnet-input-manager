using Godot;
using Godot.Collections;

namespace InputManager;

[GlobalClass]
public partial class InputAssociatedActions : Resource
{
        [Export] public string GroupName { get; private set; }
        [Export] public Array<string> AllActions { get; private set; }
}