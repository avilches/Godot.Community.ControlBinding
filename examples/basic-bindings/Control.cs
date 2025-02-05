using Godot.Community.ControlBinding.Collections;
using Godot.Community.ControlBinding.Formatters;
using Godot;
using Godot.Community.ControlBinding;
using System.Collections.Generic;

namespace ControlBinding;

public partial class Control : ObservableNode
{
	private bool labelIsVisible = true;
	public bool IsAddNewPlayerEnabled
	{
		get { return labelIsVisible; }
		set { SetValue(ref labelIsVisible, value); }
	}

	private string longText;
	public string LongText
	{
		get { return longText; }
		set { SetValue(ref longText, value); }
	}

	private int spinBoxValue;
	public int SpinBoxValue
	{
		get { return spinBoxValue; }
		set { SetValue(ref spinBoxValue, value); }
	}

	public ObservableList<PlayerData> playerDatas { get; set; } = new(){
		new PlayerData{Health = 500},
	};

	public ObservableList<PlayerData> playerDatas2 { get; set; } = new(){
		new PlayerData{Health = 500},
	};

	private BindingMode _selectedBindingMode;
	public BindingMode SelectedBindingMode
	{
		get { return _selectedBindingMode; }
		set { SetValue(ref _selectedBindingMode, value); }
	}

    private ObservableList<string> _backinglistForTesting = new() { "Test" };
    public ObservableList<string> BackingListForTesting
    {
        get { return _backinglistForTesting; }
        set { SetValue(ref _backinglistForTesting, value); }
    }

	private string errorMessage;
	public string ErrorMessage
	{
		get { return errorMessage; }
		set { SetValue(ref errorMessage, value); }
	}

	public override void _Ready()
	{
		// Bind root properties to UI        
		BindProperty<Button, bool, bool>("%Button", nameof(Button.Disabled), nameof(IsAddNewPlayerEnabled), formatter: new ReverseBoolValueFormatter());
		BindProperty<CheckBox, bool, bool>("%CheckBox", nameof(CheckBox.ButtonPressed), nameof(IsAddNewPlayerEnabled), BindingMode.TwoWay);
		BindProperty<CodeEdit, string, string>("%CodeEdit", nameof(CodeEdit.Text), nameof(LongText), BindingMode.TwoWay);
		BindProperty<CodeEdit, string, string>("%CodeEdit2", nameof(CodeEdit.Text), nameof(LongText), BindingMode.TwoWay);

		BindProperty<HSlider, int, int>("%HSlider", nameof(SpinBox.Value), nameof(SpinBoxValue), BindingMode.TwoWay);
		BindProperty<VSlider, int, int>("%VSlider", nameof(SpinBox.Value), nameof(SpinBoxValue), BindingMode.TwoWay);
		BindProperty<SpinBox, int, int>("%SpinBox", nameof(SpinBox.Value), nameof(SpinBoxValue), BindingMode.TwoWay)
			.AddValidator(v => (double)v > 0f ? null : "Value must be greater than 0");

		BindProperty<HScrollBar, int, int>("%HScrollBar", nameof(SpinBox.Value), nameof(SpinBoxValue), BindingMode.TwoWay);
		BindProperty<VScrollBar, int, int>("%VScrollBar", nameof(SpinBox.Value), nameof(SpinBoxValue), BindingMode.TwoWay);
		BindProperty<ProgressBar, int, int>("%ProgressBar", nameof(SpinBox.Value), nameof(SpinBoxValue), BindingMode.TwoWay);
		BindProperty<Label, string, int>("%SpinboxLabel", nameof(Label.Text), nameof(SpinBoxValue), BindingMode.OneWay);

		// Bind to SelectedPlayerData.Health        
		BindProperty<LineEdit, int, string>("%LineEdit", nameof(LineEdit.Text), $"{nameof(SelectedPlayerData)}.{nameof(PlayerData.Health)}", BindingMode.TwoWay,
			new ValueFormatter<int, string>()
			{
				FormatTarget = (v, p) => int.TryParse(v, out int value) ? value : 0,
			})
			.AddValidator(v => int.TryParse(v, out var value) ? null : "Health must be a number")
			.AddValidator(v => int.TryParse(v, out var value) && value > 0 ? null : "Health must be greater than 0")
			.AddValidationHandler((control, isValid, message) => { 
				control.RightIcon = isValid ? null : (Texture2D)ResourceLoader.Load("uid://b5s5nstqwi4jh"); 
				control.Modulate = new Color(1, 1, 1, 1) ;
			});

		// list binding
		BindListProperty<ItemList, PlayerData, ListItem>("%ItemList", nameof(playerDatas), formatter: new PlayerDataListFormatter());
		BindListProperty<ItemList, PlayerData, ListItem>("%ItemList2", nameof(playerDatas2), formatter: new PlayerDataListFormatter());

		// BindProperty<TextEdit>("%TextEdit", nameof(TextEdit.Text), $"{nameof(SelectedPlayerData)}.{nameof(PlayerData.ListOfThings)}", BindingMode.OneWayToTarget, new StringToListFormatter());
		// BindListProperty<ItemList>("%ItemList3", $"{nameof(SelectedPlayerData)}.{nameof(PlayerData.ListOfThings)}", BindingMode.TwoWay);

		BindEnumProperty<OptionButton, BindingMode>("%OptionButton", $"{nameof(SelectedPlayerData)}.BindingMode");
		BindSceneList<VBoxContainer>("%VBoxContainer", nameof(playerDatas), "uid://die1856ftg8w8", BindingMode.TwoWay);
		BindProperty<Label, bool, bool>("%ErrorLabel", nameof(Label.Visible), nameof(HasErrors), BindingMode.OneWay);
		BindProperty<Label, bool, string>("%ErrorLabel", nameof(Label.Text), nameof(ErrorMessage), BindingMode.OneWay);

		ControlValidationChanged += (control, propertyName, message, isValid) =>
		{
			control.Modulate = isValid ? Colors.White : Colors.Red;
			control.TooltipText = message;
			ErrorMessage = message;
		};

		base._Ready();
	}

	public void _on_item_list_item_selected(int index)
	{
		if (index == -1)
			SelectedPlayerData = null;
		else
			SelectedPlayerData = playerDatas[index];
	}

	public void _on_Button_pressed()
	{
		playerDatas.Add(new PlayerData
		{
			Health = 100
		});
	}

	private PlayerData selectedPlayerData = new();
	public PlayerData SelectedPlayerData
	{
		get { return selectedPlayerData; }
		set { SetValue(ref selectedPlayerData, value); }
	}

	public void _on_button_2_pressed()
	{
		var node = GetNode<ItemList>("%ItemList");
		var selectedIndexes = node.GetSelectedItems();
		if (selectedIndexes.Length > 0)
		{
			foreach (var selectedIndex in selectedIndexes)
			{
				var item = playerDatas[selectedIndex];
				playerDatas.Remove(item);
				playerDatas2.Add(item);

				if (SelectedPlayerData == item)
					SelectedPlayerData = null;
			}
		}
	}

	public void _on_button_3_pressed()
	{
		var node = GetNode<ItemList>("%ItemList2");
		var selectedIndexes = node.GetSelectedItems();
		if (selectedIndexes.Length > 0)
		{
			foreach (var selectedIndex in selectedIndexes)
			{
				var item = playerDatas2[selectedIndex];
				playerDatas2.Remove(item);
				playerDatas.Add(item);
			}
		}
	}
}
