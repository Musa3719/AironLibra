using System.Collections.Generic;
using TMPro;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    public static CharacterCreation _Instance;

    private bool _isMale;
    private GameObject _myCam;
    private GameObject _uI;
    private GameObject _colorSelection;
    private GameObject _maleHairSelection;
    private GameObject _femaleHairSelection;
    private GameObject _beardSelection;
    private DynamicCharacterAvatar _charPreview;
    private Button _confirmButton;
    private Button _chooseHairButton;
    private Button _chooseBeardButton;
    private int _lastDnaMenu;
    private Image _firstDnaImage;
    private Image _secondDnaImage;
    private Image _thirdDnaImage;
    private GameObject _firstDnaMenu;
    private GameObject _secondDnaMenu;
    private GameObject _thirdDnaMenu;

    public void Init()
    {
        _Instance = this;
        _isMale = true;
        _myCam = transform.Find("Cam").gameObject;
        _colorSelection = transform.Find("Canvas").Find("FlexibleColorPicker").gameObject;
        _maleHairSelection = transform.Find("Canvas").Find("MaleHairSelection").gameObject;
        _femaleHairSelection = transform.Find("Canvas").Find("FemaleHairSelection").gameObject;
        _beardSelection = transform.Find("Canvas").Find("BeardSelection").gameObject;
        _confirmButton = transform.Find("Canvas").Find("ConfirmButton").GetComponent<Button>();
        _chooseHairButton = transform.Find("Canvas").Find("ChooseHairButton").GetComponent<Button>();
        _chooseBeardButton = transform.Find("Canvas").Find("ChooseBeardButton").GetComponent<Button>();
        _charPreview = transform.Find("CharacterPreview").GetComponent<DynamicCharacterAvatar>();
        _firstDnaImage = transform.Find("Canvas").Find("DNA").Find("FirstImage").GetComponent<Image>();
        _secondDnaImage = transform.Find("Canvas").Find("DNA").Find("SecondImage").GetComponent<Image>();
        _thirdDnaImage = transform.Find("Canvas").Find("DNA").Find("ThirdImage").GetComponent<Image>();
        _firstDnaMenu = transform.Find("Canvas").Find("DNA").Find("BodyDnaSliders").gameObject;
        _secondDnaMenu = transform.Find("Canvas").Find("DNA").Find("FaceDnaSliders").gameObject;
        _thirdDnaMenu = transform.Find("Canvas").Find("DNA").Find("OtherDnaSliders").gameObject;

        _confirmButton.interactable = false;

        SliderArrange(transform.Find("Canvas").Find("DNA").Find("BodyDnaSliders"));
        SliderArrange(transform.Find("Canvas").Find("DNA").Find("FaceDnaSliders"));
        SliderArrange(transform.Find("Canvas").Find("DNA").Find("OtherDnaSliders"));
    }
    private void SliderArrange(Transform parent)
    {
        Slider slider;
        foreach (Transform child in parent)
        {
            slider = child.GetComponent<Slider>();
            if (slider == null) continue;
            string name = slider.gameObject.name;
            slider.onValueChanged.AddListener((float value) => SetDna(name, value));
        }
    }
    private void SliderRandomize(Transform parent)
    {
        Slider slider;
        foreach (Transform child in parent)
        {
            slider = child.GetComponent<Slider>();
            if (slider == null) continue;
            slider.value = Random.Range(slider.minValue, slider.maxValue);
        }
    }
    private void OnEnable()
    {
        GameManager._Instance._MainCamera.SetActive(false);
        _myCam.SetActive(true);
        if (Gaia.ProceduralWorldsGlobalWeather.Instance != null)
            Gaia.ProceduralWorldsGlobalWeather.Instance.transform.parent.gameObject.SetActive(false);
        _uI = GameObject.FindGameObjectWithTag("UI").transform.Find("UIMain").gameObject;
        _uI.SetActive(false);
    }
    private void OnDisable()
    {
        if (GameManager._Instance == null || GameManager._Instance._MainCamera == null) return;

        GameManager._Instance._MainCamera.SetActive(true);
        _myCam.SetActive(false);
        if (Gaia.ProceduralWorldsGlobalWeather.Instance != null)
            Gaia.ProceduralWorldsGlobalWeather.Instance.transform.parent.gameObject.SetActive(true);
        if (_uI != null)
            _uI.SetActive(true);
    }
    private void Update()
    {
        if (M_Input.GetButtonDown("Esc"))
            EmptyButton();
    }

    public void SetName(string name)
    {
        SaveSystemHandler._Instance._PlayerNameCreation = name;

        if (name == "")
        {
            _confirmButton.interactable = false;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = Localization._Instance._UI[17];
        }
        else
        {
            _confirmButton.interactable = true;
            _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = Localization._Instance._UI[23];
        }
    }
    public void SetGender(bool isMale)
    {
        Color skin = _charPreview.GetColor("Skin").color;
        Color hair = _charPreview.GetColor("Hair").color;
        Color eyes = _charPreview.GetColor("Eyes").color;
        WorldAndNpcArranger.SetGender(_charPreview, isMale);
        _charPreview.SetColorValue("Skin", skin);
        _charPreview.SetColorValue("Hair", hair);
        _charPreview.SetColorValue("Eyes", eyes);

        _isMale = isMale;
        if (isMale)
        {
            _chooseBeardButton.interactable = true;
            Transform sliderObj = transform.Find("Canvas").Find("DNA").Find("BodyDnaSliders").Find("breastCleavage");
            if (sliderObj != null)
            {
                sliderObj.name = "bodyFitness";
                sliderObj.parent.Find("TextChangable").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[65];
                sliderObj.GetComponent<Slider>().onValueChanged.RemoveAllListeners();
                sliderObj.GetComponent<Slider>().value = _charPreview.GetDNA()["bodyFitness"].Value;
                sliderObj.GetComponent<Slider>().onValueChanged.AddListener((float value) => SetDna("bodyFitness", value));
            }
        }
        else
        {
            _chooseBeardButton.interactable = false;
            Transform sliderObj = transform.Find("Canvas").Find("DNA").Find("BodyDnaSliders").Find("bodyFitness");
            if (sliderObj != null)
            {
                sliderObj.name = "breastCleavage";
                sliderObj.parent.Find("TextChangable").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[66];
                sliderObj.GetComponent<Slider>().onValueChanged.RemoveAllListeners();
                sliderObj.GetComponent<Slider>().value = _charPreview.GetDNA()["breastCleavage"].Value;
                sliderObj.GetComponent<Slider>().onValueChanged.AddListener((float value) => SetDna("breastCleavage", value));
            }
        }

        BuildChar();
    }

    public void RandomizeDna()
    {
        SliderRandomize(transform.Find("Canvas").Find("DNA").Find("BodyDnaSliders"));
        SliderRandomize(transform.Find("Canvas").Find("DNA").Find("FaceDnaSliders"));
        SliderRandomize(transform.Find("Canvas").Find("DNA").Find("OtherDnaSliders"));
    }
    public void DnaLeftMenu()
    {
        _lastDnaMenu--;
        _lastDnaMenu = _lastDnaMenu % 3;
        if (_lastDnaMenu < 0) _lastDnaMenu = 2;
        OpenDnaMenu(_lastDnaMenu);
    }
    public void DnaRightMenu()
    {
        _lastDnaMenu++;
        _lastDnaMenu = _lastDnaMenu % 3;
        if (_lastDnaMenu < 0) _lastDnaMenu = 2;
        OpenDnaMenu(_lastDnaMenu);
    }
    private void CloseAllDnaMenus()
    {
        _firstDnaImage.color = new Color(120f / 255f, 120f / 255f, 120f / 255f);
        _secondDnaImage.color = new Color(120f / 255f, 120f / 255f, 120f / 255f);
        _thirdDnaImage.color = new Color(120f / 255f, 120f / 255f, 120f / 255f);
        _firstDnaMenu.SetActive(false);
        _secondDnaMenu.SetActive(false);
        _thirdDnaMenu.SetActive(false);
    }
    public void OpenDnaMenu(int i)
    {
        CloseAllDnaMenus();

        _lastDnaMenu = i;
        _lastDnaMenu = _lastDnaMenu % 3;
        if (_lastDnaMenu < 0) _lastDnaMenu = 2;

        if (i == 0)
        {
            _firstDnaImage.color = Color.white;
            _firstDnaMenu.SetActive(true);
        }
        else if (i == 1)
        {
            _secondDnaImage.color = Color.white;
            _secondDnaMenu.SetActive(true);
        }
        else if (i == 2)
        {
            _thirdDnaImage.color = Color.white;
            _thirdDnaMenu.SetActive(true);
        }
    }

    public void SetDna(string name, float value)
    {
        if (_charPreview == null) return;

        _charPreview.SetDNA(name, value, true);

        BuildChar();
    }
    public void OpenColorSelection(int colorNumber)
    {
        EmptyButton();
        _colorSelection.GetComponent<RectTransform>().anchoredPosition = new Vector2(_colorSelection.GetComponent<RectTransform>().anchoredPosition.x, -52f - colorNumber * 48f);
        _colorSelection.GetComponent<FlexibleColorPicker>().onColorChange.RemoveAllListeners();

        Color color;
        if (colorNumber == 0)
            color = _charPreview.GetColor("Skin").color;
        else if (colorNumber == 1)
            color = _charPreview.GetColor("Hair").color;
        else if (colorNumber == 2)
            color = _charPreview.GetColor("Eyes").color;
        else
            color = _charPreview.GetColor("Eyes").color;//

        _colorSelection.GetComponent<FlexibleColorPicker>().SetStartingColor(color);
        _colorSelection.GetComponent<FlexibleColorPicker>().SetColor(color);

        if (colorNumber == 0)
            _colorSelection.GetComponent<FlexibleColorPicker>().onColorChange.AddListener((Color color) => _charPreview.SetColorValue("Skin", color));
        else if (colorNumber == 1)
            _colorSelection.GetComponent<FlexibleColorPicker>().onColorChange.AddListener((Color color) => _charPreview.SetColorValue("Hair", color));
        else if (colorNumber == 2)
            _colorSelection.GetComponent<FlexibleColorPicker>().onColorChange.AddListener((Color color) => _charPreview.SetColorValue("Eyes", color));

        _colorSelection.GetComponent<FlexibleColorPicker>().onColorChange.AddListener((Color color) => BuildChar());

        _colorSelection.SetActive(true);
    }

    public void OpenHairSelection()
    {
        EmptyButton();
        if (_isMale)
            _maleHairSelection.SetActive(true);
        else
            _femaleHairSelection.SetActive(true);
    }
    public void OpenBeardSelection()
    {
        EmptyButton();
        _beardSelection.SetActive(true);
    }

    public void SetHair(UMATextRecipe recipe)
    {
        _charPreview.ClearSlot("Hair");
        if (recipe != null)
            _charPreview.SetSlot(recipe);

        BuildChar();
    }
    public void SetBeard(UMATextRecipe recipe)
    {
        _charPreview.ClearSlot("Beard");
        if (recipe != null)
            _charPreview.SetSlot(recipe);

        BuildChar();
    }
    public void BuildChar()
    {
        _charPreview.BuildCharacterEnabled = false;
        _charPreview.BuildCharacterEnabled = true;
    }
    public void EmptyButton()
    {
        CloseHairAndBeardSelection();
        CloseColorSelection();
    }
    private void CloseHairAndBeardSelection()
    {
        _maleHairSelection.SetActive(false);
        _femaleHairSelection.SetActive(false);
        _beardSelection.SetActive(false);
    }
    private void CloseColorSelection()
    {
        _colorSelection.SetActive(false);
    }
    public void CloseCreation()
    {
        if (GameManager._Instance._LevelIndex == 0)
            gameObject.SetActive(false);
        else
            GameManager._Instance.ToMenu();
    }
    public void ConfirmCharacter()
    {
        //
        GameManager._Instance.CharacterCreationFinished();
    }
}
