using System.Collections.Generic;
using Godot;

namespace InputManager.InputImageMapping;

[GlobalClass]
public partial class InputImageMappingResource : Resource
{
    [Export] private Godot.Collections.Dictionary<string, string> InputNameToImagePath { get; set; } = new();
    [Export] private Godot.Collections.Dictionary<string, InputEvent> InputNameToInputEvent { get; set; } = new();
    public int MappingCount => InputNameToImagePath.Count;

    public struct KeyMapping
    {
        public string InputName; 
        public InputEvent InputEvent; 
        public string ImagePath;
    };
    
    public void AddMapping(string inputName, InputEvent inputEvent, string imagePath)
    {
        InputNameToImagePath[inputName] = imagePath;
        InputNameToInputEvent[inputName] = inputEvent;  
        
        GD.Print($"Added mapping: {inputName} -> {imagePath}");
    }
    
    public void ClearMappings()
    {
        InputNameToImagePath.Clear();
        InputNameToInputEvent.Clear();
    }
    
    public List<KeyMapping> GetMappings()
    {
        var result = new List<KeyMapping>();
        
        foreach (var inputName in InputNameToImagePath.Keys)
        {
            if (InputNameToInputEvent.TryGetValue(inputName, out var inputEvent))
            {
                result.Add(new KeyMapping { InputName = inputName, InputEvent = inputEvent, ImagePath = InputNameToImagePath[inputName] });
            }
        }
        
        return result;
    }
    
    public string GetImagePathForInput(InputEvent inputEvent)
    {
        foreach (var (inputName, existingInputEvent) in InputNameToInputEvent)
        {
            if (InputMatchesEvent(inputEvent, existingInputEvent) && 
                InputNameToImagePath.TryGetValue(inputName, out var imagePath))
            {
                return imagePath;
            }
        }
        
        return null;
    }
    
    private static bool InputMatchesEvent(InputEvent inputEvent, InputEvent mapped)
    {
        return inputEvent switch
        {
            InputEventKey inputKey when mapped is InputEventKey mappedKey =>
                inputKey.PhysicalKeycode == mappedKey.PhysicalKeycode,
            
            InputEventJoypadButton inputButton when mapped is InputEventJoypadButton mappedButton => 
                inputButton.ButtonIndex == mappedButton.ButtonIndex,
            
            _ => false
        };
    }
}