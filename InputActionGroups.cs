using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace DotNetInputManager;

[GlobalClass]
public partial class InputActionGroups : Resource
{
        [Export] public Array<InputActionGroup> KeyboardAndMouseActionGroups { get; private set; }
        [Export] public Array<InputActionGroup> ControllerActionGroups { get; private set; }
        
        public Array<InputActionGroup> GetGroups(GenericInputType inputType)
        {
                return inputType switch
                {
                        GenericInputType.KeyboardAndMouse => KeyboardAndMouseActionGroups,
                        GenericInputType.Controller => ControllerActionGroups,
                        _ => throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null)
                };
        }
        
        public InputActionGroup GetGroup(GenericInputType inputType, string input)
        {
                Array<InputActionGroup> actionGroups = inputType switch
                {
                        GenericInputType.KeyboardAndMouse => KeyboardAndMouseActionGroups,
                        GenericInputType.Controller => ControllerActionGroups,
                        _ => throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null)
                };
                
                return actionGroups?.First(it => it.GroupName == input);
        }
        
        public Array<string> GetActions(GenericInputType inputType, string input)
        {
                Array<InputActionGroup> actionGroups = inputType switch
                {
                        GenericInputType.KeyboardAndMouse => KeyboardAndMouseActionGroups,
                        GenericInputType.Controller => ControllerActionGroups,
                        _ => throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null)
                };
                
                return actionGroups?.First(it => it.GroupName == input).Actions;
        }
}