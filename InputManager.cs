using System;
using System.Linq;
using System.Threading.Tasks;
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
    public string ProductId { get; private set; }
    public int DeviceId { get; private set; }
    public Action<InputType> InputTypeChanged { get; set; }
    public Action<InputEvent> InputPressed { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsPressed() && !@event.IsEcho())
        {
            InputPressed?.Invoke(@event);
        }

        if (@event is InputEventMouseMotion or InputEventMouseButton or InputEventKey
            && InputType != InputType.KeyboardAndMouse)
        {
            InputType = InputType.KeyboardAndMouse;
            InputTypeChanged?.Invoke(InputType);
        }

        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
        {
            DeviceId = @event.Device;
            var deviceInfo = Input.GetJoyInfo(DeviceId);

            string deviceName = Input.GetJoyName(DeviceId);
            string deviceVendor = deviceInfo["vendor_id"].AsInt32().ToString("X4");
            string productId = deviceInfo["product_id"].AsInt32().ToString("X4");

            var inputType = GetControllerType(deviceVendor, productId);

            if (inputType == InputType) return;

            DeviceVendor = deviceVendor;
            ProductId = productId;
            DeviceName = deviceName;
            InputType = inputType;
            InputTypeChanged?.Invoke(InputType);
        }
    }

    public void AddInputMapInputEvent(string inputAction, InputEvent inputEvent)
    {
        var actionGroup = InputActionGroups.GetActions(inputAction);
        foreach (string action in actionGroup)
        {
            InputMap.ActionAddEvent(action, inputEvent);
        }
    }

    public void RemoveInputMapInputEvent(string inputAction, InputEvent inputEvent)
    {
        var actionGroup = InputActionGroups.GetActions(inputAction);
        foreach (string action in actionGroup)
        {
            InputMap.ActionEraseEvent(action, inputEvent);
        }
    }

    public void RemoveInputMapInputEvents(string groupName)
    {
        var actionGroup = InputActionGroups.GetActions(groupName);
        foreach (string action in actionGroup)
        {
            InputMap.ActionEraseEvents(action);
        }
    }

    public string GetInputIcon(string inputAction)
    {
        bool isKeyboard = InputType == InputType.KeyboardAndMouse;

        var inputEvent = InputMap.ActionGetEvents(inputAction)
            .FirstOrDefault(it => isKeyboard ?
                it is InputEventKey or InputEventMouseButton :
                it is InputEventJoypadButton or InputEventJoypadMotion
            );

        return inputEvent != null ? GetInputIcon(inputEvent, InputType) : null;
    }

    public string GetInputIcon(InputEvent inputEvent, InputType inputType)
    {
        var mappingResource = inputType switch
        {
            InputType.KeyboardAndMouse => KeyboardIconMapping,
            InputType.XboxController => XboxIconMapping,
            InputType.NintendoController => NintendoIconMapping,
            InputType.SonyController => SonyIconMapping,
            _ => XboxIconMapping,
        };

        return mappingResource.GetImagePathForInput(inputEvent);
    }

    public InputType LastSeenControllerType
    {
        get => GetControllerType(DeviceVendor, ProductId);
    }

    private InputType GetControllerType(string vendorId, string deviceId)
    {
        return vendorId switch
        {
            "057E" when deviceId == "2009" => InputType.NintendoController,
            "054C" when deviceId is "054C" or "0CE6" => InputType.SonyController,
            "0738" when deviceId == "4507" => InputType.XboxController,
            _ => InputType.GenericController,
        };
    }

    public void StartRumble()
    {
        if (InputType != InputType.KeyboardAndMouse)
        {
            Input.StartJoyVibration(DeviceId, 0.3f, 0.3f);
        }
    }

    public void StopRumble()
    {
        if (InputType != InputType.KeyboardAndMouse)
        {
            Input.StopJoyVibration(DeviceId);
        }
    }
}