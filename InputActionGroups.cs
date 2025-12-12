using System.Linq;
using Godot;
using Godot.Collections;

namespace DotNetInputManager;

[GlobalClass]
public partial class InputActionGroups : Resource
{
        [Export] public Array<InputActionGroup> ActionGroups { get; private set; }
        
        public Array<string> GetActions(string input) => ActionGroups.First(it => it.GroupName == input).Actions;
}