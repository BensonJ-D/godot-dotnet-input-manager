using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace InputManager.InputImageMapping;

public class InputImageMapper
{
    public InputImageMappingResource MappingResource { get; private set; }

    private Dictionary<InputType, List<string>> SDLInputButtonNames = new()
    {
        { InputType.XboxController, ["A", "B", "X", "Y", "View", "Guide", "Menu", "Left_Stick_Click", "Right_Stick_Click", "LB", "RB", "Dpad_Up", "Dpad_Down", "Dpad_Left", "Dpad_Right", "Share"] },
        { InputType.NintendoController, ["B", "A", "Y", "X", "Minus", "Home", "Plus", "Left Stick", "Right Stick", "LB", "RB", "Dpad_Up", "Dpad_Down", "Dpad_Left", "Dpad_Right", "Capture"] },
        { InputType.SonyController, ["Cross", "Circle", "Square", "Triangle", "Share", "PS", "Options", "L3", "R3", "L1", "R1", "Dpad_Up", "Dpad_Down", "Dpad_Left", "Dpad_Right", "Microphone"] }
    };
    
    public string MapResources(string inputDirectory, string filePattern, InputType inputType)
    {
        if (string.IsNullOrEmpty(inputDirectory))
        {
            return "Error: Please specify a valid directory";
        }
        
        if (string.IsNullOrEmpty(filePattern))
        {
            return "Error: Please specify a file pattern";
        }
        
        MappingResource = new InputImageMappingResource();
        
        try
        {
            DirAccess directory = DirAccess.Open(inputDirectory);
            if (directory == null)
            {
                return $"Error: Could not open directory {inputDirectory}";
            }
            
            directory.GetFiles();
            foreach (string filename in directory.GetFiles())
            {
                if (!MatchesPattern(filename, filePattern)) continue;
                
                string filePath = $"{inputDirectory.TrimEnd('/')}/{filename}";
                string inputName = ExtractInputName(filename, filePattern);
                
                if (string.IsNullOrEmpty(inputName)) continue;

                if (inputType == InputType.KeyboardAndMouse)
                {
                    InputEvent inputEvent = MapKeyToInputEvent(inputName);
                    if(inputEvent != null)
                        MappingResource.AddMapping(inputName, inputEvent, filePath);
                }
                else
                {
                    InputEvent inputEvent = GetControllerInputEventFromInputName(inputName, inputType);
                    if(inputEvent != null)
                        MappingResource.AddMapping(inputName, inputEvent, filePath);
                }
            }
            
            return $"Mapped {MappingResource.MappingCount} images to keys";
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error mapping images: {ex}");
            return $"Error: {ex.Message}";
        }
    }
    
    private static bool MatchesPattern(string filename, string pattern)
    {
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".")
            + "$";
        
        return Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase);
    }
    
    private static string ExtractInputName(string filename, string pattern)
    {
        int wildcardPos = pattern.IndexOf('*');

        if (wildcardPos < 0) return null;
        
        string prefix = pattern[..wildcardPos];
        string suffix = pattern[(wildcardPos + 1)..];

        if (!filename.StartsWith(prefix) || !filename.EndsWith(suffix)) return null;
        
        int startPos = prefix.Length;
        int length = filename.Length - suffix.Length - startPos;
                
        return length > 0 ? filename.Substring(startPos, length) : null;
    }
    
    private InputEvent MapKeyToInputEvent(string inputName)
    {
        if (inputName.Length != 1)
            return GetKeyboardAndMouseInputEventFromInputName(inputName);
        
        char keyChar = inputName[0];

        if (!char.IsLetterOrDigit(keyChar))
            return GetKeyboardAndMouseInputEventFromInputName(inputName);
        
        var keyEvent = new InputEventKey();
        keyEvent.PhysicalKeycode = (Key)keyChar;
        keyEvent.Keycode = (Key)keyChar;
        return keyEvent;

    }
    
    public string SaveResource(string outputFilePath)
    {
        try
        {
            Error error = ResourceSaver.Save(MappingResource, outputFilePath);
            return error == Error.Ok ? "Mappings saved successfully" : $"Error saving mappings: {error}";
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error saving resource: {ex}");
            return $"Error: {ex.Message}";
        }
    }

    private static InputEvent GetKeyboardAndMouseInputEventFromInputName(string inputName)
    {
        switch (inputName.ToLower())
        {
            // Keyboard Aliases
            case "space":
                var spaceEvent = new InputEventKey();
                spaceEvent.PhysicalKeycode = Key.Space;
                return spaceEvent;
            
            case "enter":
            case "return":
                var enterEvent = new InputEventKey();
                enterEvent.PhysicalKeycode = Key.Enter;
                return enterEvent;
            
            case "shift":
                var shiftEvent = new InputEventKey();
                shiftEvent.PhysicalKeycode = Key.Shift;
                return shiftEvent;
            
            case "ctrl":
            case "control":
                var ctrlEvent = new InputEventKey();
                ctrlEvent.PhysicalKeycode = Key.Ctrl;
                return ctrlEvent;
            
            case "alt":
                var altEvent = new InputEventKey();
                altEvent.PhysicalKeycode = Key.Alt;
                return altEvent;
            
            case "esc":
            case "escape":
                var escEvent = new InputEventKey();
                escEvent.PhysicalKeycode = Key.Escape;
                return escEvent;
                
            
            case "up":
            case "arrow_up":
                var upEvent = new InputEventKey();
                upEvent.PhysicalKeycode = Key.Up;
                return upEvent;
            
            case "down":
            case "arrow_down":
                var downEvent = new InputEventKey();
                downEvent.PhysicalKeycode = Key.Down;
                return downEvent;
            
            case "left":
            case "arrow_left":
                var leftEvent = new InputEventKey();
                leftEvent.PhysicalKeycode = Key.Left;
                return leftEvent;
            
            case "right":
            case "arrow_right":
                var rightEvent = new InputEventKey();
                rightEvent.PhysicalKeycode = Key.Right;
                return rightEvent;
            
            default:
                return null;
        }
    }

    private InputEvent GetControllerInputEventFromInputName(string inputName, InputType inputType)
    {
        List<string> inputNames = SDLInputButtonNames[inputType];
        int inputIndex = inputNames.IndexOf(inputName);
        
        if (inputIndex < 0) return null;
        
        InputEventJoypadButton inputEvent = new InputEventJoypadButton();
        inputEvent.ButtonIndex = (JoyButton)inputIndex;
        
        return inputEvent;
    }
}