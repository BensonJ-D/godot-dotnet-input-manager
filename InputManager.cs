using System;
using Godot;
using Godot.Collections;
using InputManager.InputImageMapping;

namespace InputManager;

[GlobalClass]
public partial class InputManager : Node
{
    [Export] public InputImageMappingResource KeyboardIconMapping { get; private set; }
    [Export] public InputImageMappingResource SonyIconMapping { get; private set; }
    [Export] public InputImageMappingResource NintendoIconMapping { get; private set; }
    [Export] public InputImageMappingResource XboxIconMapping { get; private set; }
    
    [Export] public Dictionary<string, Array<string>> InputActionGroups { get; private set; } 
    
    public string DeviceName { get; private set; }
    public string DeviceVendor { get; private set; } 
    public string DeviceId { get; private set; } 
    public Action<int> DeviceChanged { get; set; }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventJoypadButton) return;
        
        int deviceId = @event.Device;
        Dictionary deviceInfo = Input.GetJoyInfo(deviceId);
            
        DeviceVendor = deviceInfo["vendor_id"].AsString();
        DeviceId = deviceInfo["product_id"].AsString();
        DeviceName = Input.GetJoyName(deviceId);
            
        DeviceChanged(deviceId);
    }

    public void AddInputMapInputEvent(string inputAction, InputEvent inputEvent)
    {
        Array<string> actionGroup = InputActionGroups[inputAction];
        foreach (string action in actionGroup)
        {
            InputMap.ActionAddEvent(action, inputEvent);
        }
    }
    
    public void RemoveInputMapInputEvent(string inputAction, InputEvent inputEvent)
    {
        Array<string> actionGroup = InputActionGroups[inputAction];
        foreach (string action in actionGroup)
        {
            InputMap.ActionEraseEvent(action, inputEvent);
        }
    }
    
    public void RemoveInputMapInputEvents(string inputAction)
    {
        Array<string> actionGroup = InputActionGroups[inputAction];
        foreach (string action in actionGroup)
        {
            InputMap.ActionEraseEvents(action);
        }
    }

    public string GetInputIcon(InputEvent inputEvent, InputType inputType)
    {
        InputImageMappingResource mappingResource;
        switch (inputType)
        {
            case InputType.KeyboardAndMouse:
                mappingResource = KeyboardIconMapping;
                break;
            case InputType.XboxController:
                mappingResource = XboxIconMapping;
                break;
            case InputType.NintendoController:
                mappingResource = NintendoIconMapping;
                break;
            case InputType.SonyController:
                mappingResource = SonyIconMapping;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(inputType), inputType, null);
        }
        
        return mappingResource.GetImagePathForInput(inputEvent);
    }
}