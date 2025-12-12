using Godot;

namespace DotNetInputManager.InputImageMapping;

[Tool]
public partial class PluginWindow : EditorPlugin
{
    private const string DefaultResourceFileName = "input_image_mapping";
    private const string DefaultResourceFilePath = "res://Game/Settings/Inputs/";
    
    private Control InputImageMapperWindow { get; set; }
    private LineEdit InputDirectoryPathLineEdit { get; set; }
    private LineEdit OutputDirectoryPathLineEdit { get; set; }
    private LineEdit OutputFileNameLineEdit { get; set; }
    private LineEdit FilePatternLineEdit { get; set; }
    private OptionButton InputTypeDropdown { get; set; }
    
    private Label StatusLabel { get; set; }

    private InputImageMapper InputImageMapper { get; } = new();
    
    public override void _EnterTree()
    {
        Initialize();
        AddControlToBottomPanel(InputImageMapperWindow, "Input Image Mapper");
    }
    
    public override void _ExitTree()
    {
        InputImageMapperWindow.QueueFree();
        RemoveControlFromBottomPanel(InputImageMapperWindow);
    }
    
    private void Initialize()
    {
        InputImageMapperWindow = new Control();
        InputImageMapperWindow.CustomMinimumSize = new Vector2(0, 250);
        
        var container = new VBoxContainer();
        container.SetAnchorAndOffset(Side.Left, 0, 0);
        InputImageMapperWindow.AddChild(container);
        
        var inputDirectoryRow = new HBoxContainer();
        container.AddChild(inputDirectoryRow);
        
        var inputDirectoryLabel = new Label();
        inputDirectoryLabel.Text = "Input Directory: ";
        inputDirectoryRow.AddChild(inputDirectoryLabel);
        
        InputDirectoryPathLineEdit = new LineEdit();
        InputDirectoryPathLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        InputDirectoryPathLineEdit.CustomMinimumSize = new Vector2(500, 0);
        inputDirectoryRow.AddChild(InputDirectoryPathLineEdit);
        
        var browseInputDirectoriesButton = new Button();
        browseInputDirectoriesButton.Text = "Browse";
        browseInputDirectoriesButton.Pressed += OnInputBrowsePressed;
        inputDirectoryRow.AddChild(browseInputDirectoriesButton);
        
        var outputDirectoryRow = new HBoxContainer();
        container.AddChild(outputDirectoryRow);
        
        var outputDirectoryLabel = new Label();
        outputDirectoryLabel.Text = "Output Directory: ";
        outputDirectoryRow.AddChild(outputDirectoryLabel);
        
        OutputDirectoryPathLineEdit = new LineEdit();
        OutputDirectoryPathLineEdit.Text = DefaultResourceFilePath;
        OutputDirectoryPathLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        OutputDirectoryPathLineEdit.CustomMinimumSize = new Vector2(500, 0);
        outputDirectoryRow.AddChild(OutputDirectoryPathLineEdit);
        
        var browseOutputDirectoriesButton = new Button();
        browseOutputDirectoriesButton.Text = "Browse";
        browseOutputDirectoriesButton.Pressed += OnOutputBrowsePressed;
        outputDirectoryRow.AddChild(browseOutputDirectoriesButton);
        
        var outputFilenameRow = new HBoxContainer();
        container.AddChild(outputFilenameRow);
        
        var outputFilenameLabel = new Label();
        outputFilenameLabel.Text = "Filename (excluding extensions): ";
        outputFilenameRow.AddChild(outputFilenameLabel);
        
        OutputFileNameLineEdit = new LineEdit();
        OutputFileNameLineEdit.Text = DefaultResourceFileName;
        OutputFileNameLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        OutputFileNameLineEdit.PlaceholderText = "e.g., control_input_prompts";
        outputFilenameRow.AddChild(OutputFileNameLineEdit);
        
        var filePatternRow = new HBoxContainer();
        container.AddChild(filePatternRow);
        
        var filePatternLabel = new Label();
        filePatternLabel.Text = "File Pattern: ";
        filePatternRow.AddChild(filePatternLabel);
        
        FilePatternLineEdit = new LineEdit();
        FilePatternLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        FilePatternLineEdit.PlaceholderText = "e.g., *_Key_Light.png";
        filePatternRow.AddChild(FilePatternLineEdit);
        
        var controllerInfoRow = new HBoxContainer();
        container.AddChild(controllerInfoRow);
        
        InputTypeDropdown = new OptionButton();
        InputTypeDropdown.Text = "Input Type: ";
        InputTypeDropdown.AddItem("Keyboard & Mouse"); // KeyboardAndMouse,
        InputTypeDropdown.AddItem("Xbox Controller"); // XboxController,
        InputTypeDropdown.AddItem("Nintendo Controller"); // NintendoController,
        InputTypeDropdown.AddItem("Sony Controller"); // SonyController,
        InputTypeDropdown.AddItem("Generic Controller"); // GenericController
        controllerInfoRow.AddChild(InputTypeDropdown);
        
        var actionsRow = new HBoxContainer();
        container.AddChild(actionsRow);
        
        var mapButton = new Button();
        mapButton.Text = "Map Images";
        mapButton.Pressed += OnMapPressed;
        actionsRow.AddChild(mapButton);
        
        var saveButton = new Button();
        saveButton.Text = "Save Mapping";
        saveButton.Pressed += OnSavePressed;
        actionsRow.AddChild(saveButton);
        
        StatusLabel = new Label();
        StatusLabel.Text = "Ready";
        container.AddChild(StatusLabel);
    }

    private void OnMapPressed()
    {
        string inputDirectory = InputDirectoryPathLineEdit.Text;
        string filePattern = FilePatternLineEdit.Text;
        var inputType = (InputType)InputTypeDropdown.Selected;
        
        string result = InputImageMapper.MapResources(inputDirectory, filePattern, inputType);
        StatusLabel.Text = result;
    }

    private void OnSavePressed()
    {
        string outputDirectory = OutputDirectoryPathLineEdit.Text;
        string outputFilename = OutputFileNameLineEdit.Text;
        string outputPath = outputDirectory + outputFilename + ".tres";

        string result = InputImageMapper.SaveResource(outputPath);
        StatusLabel.Text = result;
    }
    
    private void OnInputBrowsePressed() => BrowseFilesystem("Source Images Directory", InputDirectoryPathLineEdit);
    private void OnOutputBrowsePressed() => BrowseFilesystem("Directory to save resource", OutputDirectoryPathLineEdit);
    private void BrowseFilesystem(string searchTitle, LineEdit linkedLineEdit)
    {
        var fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
        fileDialog.Access = FileDialog.AccessEnum.Resources;
        fileDialog.Title = searchTitle;
        
        if(!string.IsNullOrEmpty(linkedLineEdit.Text)) 
            fileDialog.CurrentDir = linkedLineEdit.Text; 
        
        AddChild(fileDialog);
        fileDialog.DirSelected += dir => {
            linkedLineEdit.Text = dir;
            fileDialog.QueueFree();
        };
        fileDialog.Canceled += () => fileDialog.QueueFree();
        
        fileDialog.PopupCentered(new Vector2I(1600, 1000));
    }
}