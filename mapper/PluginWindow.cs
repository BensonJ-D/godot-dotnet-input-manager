using Godot;

namespace InputManager.InputImageMapping;

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
        
        VBoxContainer container = new VBoxContainer();
        container.SetAnchorAndOffset(Side.Left, 0, 0);
        InputImageMapperWindow.AddChild(container);
        
        HBoxContainer inputDirectoryRow = new HBoxContainer();
        container.AddChild(inputDirectoryRow);
        
        Label inputDirectoryLabel = new Label();
        inputDirectoryLabel.Text = "Input Directory: ";
        inputDirectoryRow.AddChild(inputDirectoryLabel);
        
        InputDirectoryPathLineEdit = new LineEdit();
        InputDirectoryPathLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        InputDirectoryPathLineEdit.CustomMinimumSize = new Vector2(500, 0);
        inputDirectoryRow.AddChild(InputDirectoryPathLineEdit);
        
        Button browseInputDirectoriesButton = new Button();
        browseInputDirectoriesButton.Text = "Browse";
        browseInputDirectoriesButton.Pressed += OnInputBrowsePressed;
        inputDirectoryRow.AddChild(browseInputDirectoriesButton);
        
        HBoxContainer outputDirectoryRow = new HBoxContainer();
        container.AddChild(outputDirectoryRow);
        
        Label outputDirectoryLabel = new Label();
        outputDirectoryLabel.Text = "Output Directory: ";
        outputDirectoryRow.AddChild(outputDirectoryLabel);
        
        OutputDirectoryPathLineEdit = new LineEdit();
        OutputDirectoryPathLineEdit.Text = DefaultResourceFilePath;
        OutputDirectoryPathLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        OutputDirectoryPathLineEdit.CustomMinimumSize = new Vector2(500, 0);
        outputDirectoryRow.AddChild(OutputDirectoryPathLineEdit);
        
        Button browseOutputDirectoriesButton = new Button();
        browseOutputDirectoriesButton.Text = "Browse";
        browseOutputDirectoriesButton.Pressed += OnOutputBrowsePressed;
        outputDirectoryRow.AddChild(browseOutputDirectoriesButton);
        
        HBoxContainer outputFilenameRow = new HBoxContainer();
        container.AddChild(outputFilenameRow);
        
        Label outputFilenameLabel = new Label();
        outputFilenameLabel.Text = "Filename (excluding extensions): ";
        outputFilenameRow.AddChild(outputFilenameLabel);
        
        OutputFileNameLineEdit = new LineEdit();
        OutputFileNameLineEdit.Text = DefaultResourceFileName;
        OutputFileNameLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        OutputFileNameLineEdit.PlaceholderText = "e.g., control_input_prompts";
        outputFilenameRow.AddChild(OutputFileNameLineEdit);
        
        HBoxContainer filePatternRow = new HBoxContainer();
        container.AddChild(filePatternRow);
        
        Label filePatternLabel = new Label();
        filePatternLabel.Text = "File Pattern: ";
        filePatternRow.AddChild(filePatternLabel);
        
        FilePatternLineEdit = new LineEdit();
        FilePatternLineEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        FilePatternLineEdit.PlaceholderText = "e.g., *_Key_Light.png";
        filePatternRow.AddChild(FilePatternLineEdit);
        
        HBoxContainer controllerInfoRow = new HBoxContainer();
        container.AddChild(controllerInfoRow);
        
        InputTypeDropdown = new OptionButton();
        InputTypeDropdown.Text = "Input Type: ";
        InputTypeDropdown.AddItem("Keyboard & Mouse"); // KeyboardAndMouse,
        InputTypeDropdown.AddItem("Xbox Controller"); // XboxController,
        InputTypeDropdown.AddItem("Nintendo Controller"); // NintendoController,
        InputTypeDropdown.AddItem("Sony Controller"); // SonyController,
        InputTypeDropdown.AddItem("Generic Controller"); // GenericController
        controllerInfoRow.AddChild(InputTypeDropdown);
        
        HBoxContainer actionsRow = new HBoxContainer();
        container.AddChild(actionsRow);
        
        Button mapButton = new Button();
        mapButton.Text = "Map Images";
        mapButton.Pressed += OnMapPressed;
        actionsRow.AddChild(mapButton);
        
        Button saveButton = new Button();
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
        InputType inputType = (InputType)InputTypeDropdown.Selected;
        
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
        FileDialog fileDialog = new FileDialog();
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