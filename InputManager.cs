using System;
using Godot;
using Godot.Collections;
using DotNetInputManager.InputImageMapping;

namespace DotNetInputManager;

public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; }
    
    [Export] public InputImageMappingResource KeyboardIconMapping { get; private set; }
    [Export] public InputImageMappingResource SonyIconMapping { get; private set; }
    [Export] public InputImageMappingResource NintendoIconMapping { get; private set; }
    [Export] public InputImageMappingResource XboxIconMapping { get; private set; }
    
    [Export] public InputActionGroups InputActionGroups { get; private set; } 
    
    public InputType InputType { get; private set; }
    public string DeviceName { get; private set; }
    public string DeviceVendor { get; private set; } 
    public string DeviceId { get; private set; } 
    public Action<InputType> InputTypeChanged { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion or InputEventMouseButton or InputEventKey 
            && InputType != InputType.KeyboardAndMouse)
        {
            InputType = InputType.KeyboardAndMouse;
            InputTypeChanged?.Invoke(InputType);
        }
            
        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
        {
            int deviceId = @event.Device;
            Dictionary deviceInfo = Input.GetJoyInfo(deviceId);

            string deviceName = Input.GetJoyName(deviceId);
            string deviceVendor = deviceInfo["vendor_id"].AsInt32().ToString("X4");
            string productId = deviceInfo["product_id"].AsInt32().ToString("X4");

            InputType inputType = GetControllerType(deviceVendor, productId);
            
            if (inputType == InputType) return;
            
            DeviceVendor = deviceVendor;
            DeviceId = productId;
            DeviceName = deviceName;
            InputType = inputType;
            InputTypeChanged?.Invoke(InputType);
        }
    }

    public void AddInputMapInputEvent(string inputAction, InputEvent inputEvent)
    {
        Array<string> actionGroup = InputActionGroups.GetActions(inputAction);
        foreach (string action in actionGroup)
        {
            InputMap.ActionAddEvent(action, inputEvent);
        }
    }
    
    public void RemoveInputMapInputEvent(string inputAction, InputEvent inputEvent)
    {
        Array<string> actionGroup = InputActionGroups.GetActions(inputAction);
        foreach (string action in actionGroup)
        {
            InputMap.ActionEraseEvent(action, inputEvent);
        }
    }
    
    public void RemoveInputMapInputEvents(string inputAction)
    {
        Array<string> actionGroup = InputActionGroups.GetActions(inputAction);
        foreach (string action in actionGroup)
        {
            InputMap.ActionEraseEvents(action);
        }
    }

    public string GetInputIcon(InputEvent inputEvent, InputType inputType)
    {
        InputImageMappingResource mappingResource = inputType switch
        {
            InputType.KeyboardAndMouse => KeyboardIconMapping,
            InputType.XboxController => XboxIconMapping,
            InputType.NintendoController => NintendoIconMapping,
            InputType.SonyController => SonyIconMapping,
            _ => XboxIconMapping
        };

        return mappingResource.GetImagePathForInput(inputEvent);
    }

    public InputType LastSeenControllerType => GetControllerType(DeviceVendor, DeviceId);
    
    private InputType GetControllerType(string vendorId, string deviceId)
    {
        return vendorId switch
        {
            "057E" when deviceId == "2009" => InputType.NintendoController,
            "054C" when deviceId is "054C" or "0CE6" => InputType.SonyController,
            "0738" when deviceId == "4507" => InputType.XboxController,
            _ => InputType.GenericController
        };
    }
}