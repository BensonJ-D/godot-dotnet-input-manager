using System;
using System.Linq;
using Godot;
using DotNetInputManager.InputImageMapping;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace DotNetInputManager;

public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; }

    [Export] public InputImageMappingResource KeyboardIconMapping { get; private set; }
    [Export] public InputImageMappingResource SonyIconMapping { get; private set; }
    [Export] public InputImageMappingResource NintendoIconMapping { get; private set; }
    [Export] public InputImageMappingResource XboxIconMapping { get; private set; }

    [Export] public InputActionGroups InputActionGroups { get; private set; }
    [Export] public float RumbleIntensity { get; private set; } = 1f;

    public InputType InputType { get; private set; }
    public string DeviceName { get; private set; }
    public string DeviceVendor { get; private set; }
    public string ProductId { get; private set; }
    public int DeviceId { get; private set; }
    public Action<InputType> InputTypeChanged { get; set; }
    public Action<InputEvent> InputPressed { get; set; }
    public Action<InputActionGroup> InputActionGroupUpdated { get; set; }
    
    public enum SwapStatus { Success, NoChange, NoReplacement }
    
    public record InputSwapResponse(InputActionGroup TargetGroup, SwapStatus Status)
    {
        public static InputSwapResponse NoChange => new(null, SwapStatus.NoChange);
        public static InputSwapResponse NoReplacement => new(null, SwapStatus.NoReplacement);
        public static InputSwapResponse Success(InputActionGroup group) => new (group, SwapStatus.Success);
    }
    
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
        
        bool isKeyboardOrMouseEvent = @event switch
        {
            InputEventMouseMotion motion => motion.Relative != Vector2.Zero,
            InputEventMouseButton or InputEventKey => true,
            _ => false,
        };
        
        if (isKeyboardOrMouseEvent && InputType != InputType.KeyboardAndMouse)
        {
            InputType = InputType.KeyboardAndMouse;
            InputTypeChanged?.Invoke(InputType);
        }

        if (@event is InputEventJoypadButton or InputEventJoypadMotion)
        {
            // Ignore small amounts of stick drift that don't register for movement
            if (@event is InputEventJoypadMotion { AxisValue: > -0.25f and < 0.25f })
            {
                return;
            }
            
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
    
    public InputSwapResponse SwapKeyboardAndMouseEvents(InputActionGroup groupToUpdate, InputEvent newEvent, bool isPrimaryInput = false)
    {
        GenericInputType inputType = GenericInputType.KeyboardAndMouse;
        
        // Get events in this group
        Array<InputEvent> eventsInPrimaryAction = new Array<InputEvent>(
            InputMap.ActionGetEvents(groupToUpdate.PrimaryAction)
                .Where(@event => @event is InputEventKey or InputEventMouseButton)
        );
        
        while(eventsInPrimaryAction.Count < 2)
        {
            eventsInPrimaryAction.Add(null);
        }
        
        // Grab primary or secondary event, based on which one we clicked on
        InputEvent existingEvent = isPrimaryInput ? eventsInPrimaryAction[0] : eventsInPrimaryAction[1];
        
        // If event is the same, early exit
        if(InputMatchesEvent(newEvent, existingEvent)) return InputSwapResponse.NoChange;
       
        // If the swapped event is in our current group, remove both and add them back in the right order
        InputEvent otherEvent = isPrimaryInput ? eventsInPrimaryAction[1] : eventsInPrimaryAction[0];
        if(InputMatchesEvent(newEvent, otherEvent)) {        
            RemoveInputMapInputEvent(groupToUpdate.GroupName, newEvent, inputType);
            RemoveInputMapInputEvent(groupToUpdate.GroupName, existingEvent, inputType);
            AddInputMapInputEvent(groupToUpdate.GroupName, newEvent, inputType, isPrimaryInput);
            AddInputMapInputEvent(groupToUpdate.GroupName, existingEvent, inputType, !isPrimaryInput);
            return InputSwapResponse.Success(null);
        }
        
        // Find group with existing event
        InputActionGroup owningGroup = InputActionGroups.GetGroups(inputType).Where(group => group != groupToUpdate).FirstOrDefault(group =>
            group.Actions
                .SelectMany(action => InputMap.ActionGetEvents(action))
                .Any(iEvent => InputMatchesEvent(newEvent, iEvent))
        );
        
        if(owningGroup == null)
        {
            // Replace the event in our current group with the new action
            RemoveInputMapInputEvent(groupToUpdate.GroupName, existingEvent, inputType);
            AddInputMapInputEvent(groupToUpdate.GroupName, newEvent, inputType, isPrimaryInput);
            return InputSwapResponse.Success(null);
        };
        
        // Grab the primary actions
        Array<InputEvent> eventsInOwningPrimaryAction = new Array<InputEvent>(
            InputMap.ActionGetEvents(owningGroup.PrimaryAction)
                .Where(@event => @event is InputEventKey or InputEventMouseButton)
            );
        
        bool isSwappedEventPrimaryInput = InputMatchesEvent(eventsInOwningPrimaryAction.FirstOrDefault(), newEvent);
        bool owningGroupHasMultipleEvents = eventsInOwningPrimaryAction.Count > 1;
        
        // If our source event is empty, and we're swapping with the only event for an action, no-op
        if(isSwappedEventPrimaryInput && !owningGroupHasMultipleEvents && existingEvent == null) return InputSwapResponse.NoReplacement;
        
        // Otherwise replace the events
        RemoveInputMapInputEvent(groupToUpdate.GroupName, existingEvent, inputType);
        AddInputMapInputEvent(groupToUpdate.GroupName, newEvent, inputType, isPrimaryInput);
        
        RemoveInputMapInputEvent(owningGroup.GroupName, newEvent, inputType);
        AddInputMapInputEvent(owningGroup.GroupName, existingEvent, inputType, isSwappedEventPrimaryInput);
        
        return InputSwapResponse.Success(owningGroup);
    }
    
    public InputSwapResponse SwapControllerEvents(InputActionGroup groupToUpdate, InputEvent targetEvent)
    {
        GenericInputType inputType = GenericInputType.Controller;
        
        InputEvent existingEvent = groupToUpdate.Actions
            .SelectMany(action => InputMap.ActionGetEvents(action))
            .FirstOrDefault(it => it is InputEventJoypadButton or InputEventJoypadMotion);
        
        if(InputMatchesEvent(targetEvent, existingEvent)) return InputSwapResponse.NoChange;
        
        if(targetEvent is InputEventJoypadMotion joypadMotion)
        {
            if(joypadMotion.AxisValue < 0) joypadMotion.AxisValue = -1.0f;
            if(joypadMotion.AxisValue > 0) joypadMotion.AxisValue = 1.0f;
        }
        
        InputActionGroup owningGroup = InputActionGroups.GetGroups(inputType).FirstOrDefault(group =>
            group.Actions
                .SelectMany(action => InputMap.ActionGetEvents(action))
                .Any(iEvent => InputMatchesEvent(targetEvent, iEvent))
        );
        
        RemoveInputMapInputEvent(groupToUpdate.GroupName, existingEvent, inputType);
        AddInputMapInputEvent(groupToUpdate.GroupName, targetEvent, inputType);
        
        if(owningGroup != null)
        {
            RemoveInputMapInputEvent(owningGroup.GroupName, targetEvent, inputType);
            AddInputMapInputEvent(owningGroup.GroupName, existingEvent, inputType);
        }
        
        return InputSwapResponse.Success(owningGroup);
    }

    public void AddInputMapInputEvent(string inputAction, InputEvent inputEvent, GenericInputType inputType, bool isPrimaryInput = false)
    {
        if(inputEvent == null) return;
        
        var actionGroup = InputActionGroups.GetGroup(inputType, inputAction);
        foreach (string action in actionGroup.Actions)
        {
            // If the action is the primary one, we want it to be at the front of the action
            // It doesn't matter if types are interspaced (keyboard, controller, keyboard) 
            if (isPrimaryInput)
            {
                Array<InputEvent> existing = InputMap.ActionGetEvents(action);
                InputMap.ActionEraseEvents(action);
                InputMap.ActionAddEvent(action, inputEvent);
                foreach (InputEvent iEvent in existing)
                {
                    InputMap.ActionAddEvent(action, iEvent);
                }
            }
            else
            {
                InputMap.ActionAddEvent(action, inputEvent);
            }
            
            // Treat triggers like buttons
            if(inputEvent is InputEventJoypadMotion { Axis: JoyAxis.TriggerLeft or JoyAxis.TriggerRight })
            {
                InputMap.ActionSetDeadzone(action, 1.0f);
            }
        }
        
        InputActionGroupUpdated?.Invoke(actionGroup);
    }

    public void RemoveInputMapInputEvent(string inputAction, InputEvent inputEvent, GenericInputType inputType)
    {
        if(inputEvent == null) return;
        
        var actionGroup = InputActionGroups.GetGroup(inputType, inputAction);
        foreach (string action in actionGroup.Actions)
        {
            InputMap.ActionEraseEvent(action, inputEvent);
        }
        InputActionGroupUpdated?.Invoke(actionGroup);
    }

    public void RemoveInputMapInputEvents(string groupName, GenericInputType inputType)
    {
        var actionGroup = InputActionGroups.GetGroup(inputType, groupName);
        foreach (string action in actionGroup.Actions)
        {
            InputMap.ActionEraseEvents(action);
        }
        InputActionGroupUpdated?.Invoke(actionGroup);
    }
    
    public void RemoveDefaultEvents(string groupName, GenericInputType inputType)
    {
        var actionGroup = InputActionGroups.GetGroup(inputType, groupName);
        foreach (string action in actionGroup.Actions)
        {
            Array<InputEvent> defaultEvents = GetActionEventsForType(action, inputType);
            
            foreach(InputEvent iEvent in defaultEvents)
            {
                InputMap.ActionEraseEvent(action, iEvent);
            }
        }
    }
    
    public void AddLoadedEvents(string groupName, Array<InputEvent> inputEvents, GenericInputType inputType)
    {
        var actionGroup = InputActionGroups.GetGroup(inputType, groupName);
        foreach (string action in actionGroup.Actions)
        {
            foreach(InputEvent iEvent in inputEvents)
            {
                InputMap.ActionAddEvent(action, iEvent);
            }
        }
    }
    
    public Array<InputEvent> GetActionEventsForType(string action, GenericInputType inputType)
    {
        return new Array<InputEvent>(
            InputMap.ActionGetEvents(action)
                .Where(it =>
                    inputType == GenericInputType.KeyboardAndMouse ?
                        it is InputEventKey or InputEventMouseButton :
                        it is InputEventJoypadButton or InputEventJoypadMotion)
        );
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
            "054C" when deviceId is "054C" or "0CE6" or "09CC" => InputType.SonyController,
            "0738" when deviceId == "4507" => InputType.XboxController,
            _ => InputType.GenericController,
        };
    }

    
    public void SetRumbleIntensity(float intensity)
    {
        RumbleIntensity = Mathf.Clamp(intensity, 0f, 1f);
    }

    public void StartRumble(float weakMotorIntensity, float strongMotorIntensity, float duration)
    {
        if(InputType == InputType.KeyboardAndMouse)
            return;
        
        StopRumble();
        Input.StartJoyVibration(DeviceId, weakMotorIntensity * RumbleIntensity, strongMotorIntensity * RumbleIntensity, duration);
    }
    
    public void StartRumble(float weakMotorIntensity, float strongMotorIntensity)
    {
        if(InputType == InputType.KeyboardAndMouse)
            return;
        
        StopRumble();
        Input.StartJoyVibration(DeviceId, weakMotorIntensity * RumbleIntensity, strongMotorIntensity * RumbleIntensity);
    }

    public void StopRumble()
    {
        if(InputType == InputType.KeyboardAndMouse)
            return;
        
        Input.StopJoyVibration(DeviceId);
    }
    
    public bool IsKeyboardMouseInput => InputType == InputType.KeyboardAndMouse;
    public bool IsControllerInput => InputType == LastSeenControllerType;
    
    public static bool IsControllerConnected() => Input.GetConnectedJoypads().Count > 0;
    
    private static bool InputMatchesEvent(InputEvent inputEvent, InputEvent targetEvent)
    {
        return inputEvent switch
        {
            InputEventKey inputKey when targetEvent is InputEventKey targetKey =>
                inputKey.PhysicalKeycode == targetKey.PhysicalKeycode,
            
            InputEventJoypadButton inputButton when targetEvent is InputEventJoypadButton targetButton =>
                inputButton.ButtonIndex == targetButton.ButtonIndex,
            
            InputEventMouseButton inputButton when targetEvent is InputEventMouseButton targetButton =>
                inputButton.ButtonIndex == targetButton.ButtonIndex,
            
            InputEventJoypadMotion inputAxis when targetEvent is InputEventJoypadMotion targetAxis =>
                Math.Sign(inputAxis.AxisValue) == Math.Sign(targetAxis.AxisValue) &&
                inputAxis.Axis == targetAxis.Axis,
            
            _ => false
        };
    }
    
    public static Array<InputEvent> GetEventsForInputType(GenericInputType genericInputType, string action)
    {
        var inputActions = InputMap.ActionGetEvents(action);
        switch(genericInputType)
        {
            case GenericInputType.KeyboardAndMouse:
                return new Array<InputEvent>(inputActions.Where(it => it is InputEventKey or InputEventMouse));
            
            case GenericInputType.Controller:
                return new Array<InputEvent>(inputActions.Where(it => it is InputEventJoypadButton or InputEventJoypadMotion));
        }
        
        return [];
    }
}