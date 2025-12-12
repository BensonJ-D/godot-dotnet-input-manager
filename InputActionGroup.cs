using System.Linq;
using Godot;
using Godot.Collections;

namespace DotNetInputManager;

[GlobalClass]
public partial class InputActionGroup : Resource
{
        [Export] public string GroupName { get; private set; }
        [Export] public Array<string> Actions { get; private set; }

        public string PrimaryAction => Actions.First();
}