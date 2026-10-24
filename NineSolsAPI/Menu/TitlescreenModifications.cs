using BepInEx.Bootstrap;
using I2.Loc;
using NineSolsAPI.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace NineSolsAPI.Menu;

internal class TitlescreenModifications {
    private UIControlGroup? group;
    private UIControlButton? button;

    private const bool Enable = false;

    public void Load() {
        MaybeExtendMainMenu(SceneManager.GetActiveScene());
    }

    public void Unload() {
        if (button != null) Object.Destroy(button.gameObject);
        if (group != null) Object.Destroy(group.gameObject);
    }


    public void MaybeExtendMainMenu(Scene scene) {
        if (!Enable) return;

        if (scene.name != "TitleScreenMenu") return;
        if (button) return;

        group = CreateUiControlGroup();
        button = CreateOptionsButton();
        button.clickToShowGroup = group;
    }


    private UIControlButton CreateOptionsButton() {
        var optionsButton = GameObject.Find("MainMenuButton_Option");

        var modOptions = ObjectUtils.InstantiateAutoReference(optionsButton, optionsButton.transform.parent, false);
        Object.Destroy(modOptions.GetComponentInChildren<Localize>());
        modOptions.GetComponentInChildren<TMP_Text>().text = "Mod Options";
        modOptions.gameObject.transform.SetSiblingIndex(optionsButton.transform.GetSiblingIndex() + 1);
        var modOptionButton = modOptions.GetComponentInChildren<UIControlButton>();

        return modOptionButton;
    }

    private UIControlGroup CreateUiControlGroup() {
        var providers = StartMenuLogic.Instance.gameObject.GetComponentInChildren<UICursorProvider>();

        var obj = new GameObject("ModOptions Panel");
        obj.transform.SetParent(providers.transform, false);

        var rectTransform = obj.AddComponent<RectTransform>();
        var uiControlGroup = obj.AddComponent<UIControlGroup>();
        var rcgUiPanel = obj.GetComponent<RCGUIPanel>();
        obj.AddComponent<CanvasRenderer>();
        obj.AddComponent<SelectableNavigationProvider>();
        obj.AddComponent<Animator>();
        AutoAttributeManager.AutoReference(obj);

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        rcgUiPanel.OnShowInit = new UnityEvent();
        rcgUiPanel.OnHideInit = new UnityEvent();
        rcgUiPanel.OnShowComplete = new UnityEvent();
        rcgUiPanel.OnHideComplete = new UnityEvent();

        var layout = new GameObject();
        var layoutTransform = layout.AddComponent<RectTransform>();
        layoutTransform.anchorMin = new Vector2(0.5f, 0.5f);
        layoutTransform.anchorMax = new Vector2(0.5f, 0.5f);
        layoutTransform.sizeDelta = new Vector2(600, 800);
        layout.AddComponent<VerticalLayoutGroup>().spacing = 20;
        layout.transform.SetParent(uiControlGroup.transform, false);

        var title = new GameObject();
        title.AddComponent<TextMeshProUGUI>().text = "Mod options";
        title.AddComponent<LayoutElement>();
        title.transform.SetParent(layout.transform, false);

        var padding = new GameObject();
        padding.AddComponent<CanvasRenderer>();
        padding.AddComponent<RectTransform>();
        padding.AddComponent<LayoutElement>().minHeight = 0;
        padding.transform.SetParent(layout.transform, false);

        var buttonOrig = ObjectUtils.FindDisabledByName<Button>("Show HUD")!;

        foreach (var plugin in Chainloader.PluginInfos) {
            var c = ObjectUtils.InstantiateAutoReference(buttonOrig.gameObject, layout.transform);
            c.GetComponentInChildren<TMP_Text>().text = $"{plugin.Key} {plugin.Value.Metadata.Version}";
            var flag = new FlagFieldEntryInt();
            var intFlag = ScriptableObject.CreateInstance<GameFlagInt>();
            intFlag.field = new FlagFieldInt();
            flag.flagBase = intFlag;
            flag.fieldName = "field";
        }


        uiControlGroup.defaultSelectable = layout.GetComponentInChildren<Selectable>();

        return uiControlGroup;
    }
}

internal class TitleScreenModList
{
    private static SaveSlotModMetaData[] saveModMetaDataList => SaveUtils.saveModMetaDataList;
    private static Dictionary<string, ModData> modDict => SaveUtils.modDict;

    private enum Desyncs
    {
        Missing,
        DifferentVersion,
        New
    }
    private static int[][] desyncTracker = new int[][]
    {
        new int[5],
        new int[5],
        new int[5]
    };

    private static bool modListLoaded = false;
    private static bool startGameConfirmed = false;

    private static Dictionary<string, GameObject>[] modButtons = new Dictionary<string, GameObject>[5];

    private static GameObject ModList_ScrollRect = null!;
    private static GameObject buttonClone = null!;

    private static string URLConfirm_String = "Open the download page of {0}?\n({1})";
    private static string NotSynced_ConfirmString = "This save's mods are not synced with your mods. Load anyway?\n({0} missing)\n({1} different version)\n({2} new)";
    
    internal static bool CheckModDesyncsBeforeLoadSave(SaveSlotUIButton slotButtonInstance)
    {
        if (startGameConfirmed)
        {
            return true;
        }
        int slotNumber = (int)slotButtonInstance.name[8] - 48;
        SaveSlotModMetaData meta = saveModMetaDataList[slotNumber];

        int Missing = desyncTracker[(int) Desyncs.Missing][slotNumber];
        int DifferentVersion = desyncTracker[(int) Desyncs.DifferentVersion][slotNumber];
        int New = desyncTracker[(int) Desyncs.New][slotNumber];

        if (Missing > 0 || DifferentVersion > 0 || New > 0)
        {
            startGameConfirmed = true;
            SingletonBehaviour<StartMenuLogic>.Instance.confirmPanel.ShowConfirm(string.Format(NotSynced_ConfirmString, Missing, DifferentVersion, New), () => slotButtonInstance.Submit());
            return false;
        }
        return true;
    }

    private static void LoadModList()
    {
        for (int i = 0; i < 5; i++)
        {
            modButtons[i] = new Dictionary<string, GameObject>();

            SaveSlotModMetaData? meta = saveModMetaDataList[i];

            Dictionary<string, ModData>? slotModDict = saveModMetaDataList[i]?.modList.ToDictionary(mod => mod.guid);

            for (int j = 0; j < slotModDict?.Count; j++)
            {
                ModData data = slotModDict.ElementAt(j).Value;

                GameObject newButton = ObjectUtils.InstantiateAutoReference(buttonClone, buttonClone.transform.parent, false);
                modButtons[i].Add(data.guid, newButton);
                newButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, (-50 * j));
                var newButtonText = newButton.transform.GetChild(0).GetComponent<Text>();
                newButtonText.text = $"{data.name} - {data.version}";

                if (!modDict.ContainsKey(data.guid))
                {
                    newButtonText.color = new Color(1f, 0f, 0f);
                    desyncTracker[(int)Desyncs.Missing][i]++;
                }
                else if (modDict[data.guid].version != data.version)
                {
                    newButtonText.color = new Color(1f, 0.5f, 0f);
                    desyncTracker[(int)Desyncs.DifferentVersion][i]++;
                }

                if (data.url != "")
                {
                    newButton.GetComponent<Button>().onClick.AddListener(() =>
                    SingletonBehaviour<StartMenuLogic>.Instance.confirmPanel.ShowConfirm(string.Format(URLConfirm_String, data.name, data.url), () =>
                    Application.OpenURL(data.url)));
                }
            }

            foreach (ModData data in modDict.Values)
            {
                if (slotModDict != null && !slotModDict.ContainsKey(data.guid))
                {
                    desyncTracker[(int)Desyncs.New][i]++;
                }
            }
        }
        modListLoaded = true;
    }

    private static void DisplayModListForSelectedFile(UIControlButton button)
    {
        startGameConfirmed = false;
        if (!modListLoaded)
        {
            LoadModList();
        }
        int slotNumber = (int)button.name[8] - 48;
        for (int i = 0; i < 5; i++)
        {
            foreach (var buttonoff in modButtons[i])
            {
                buttonoff.Value.SetActive(slotNumber == i);
            }
        }
    }

    public static void Setup()
    {
        modListLoaded = false;
        startGameConfirmed = false;

        for (int i = 0; i < desyncTracker.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                desyncTracker[i][j] = 0;
            }
        }

        // Mod list label
        GameObject TitleText = GameObject.Find("/MenuLogic").transform.Find("TitleText").gameObject;
        var modListLabel = ObjectUtils.InstantiateAutoReference(TitleText, TitleText.transform.parent, false);
        modListLabel.name = "modListLabel";
        modListLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(700, -200);
        modListLabel.GetComponent<Text>().text = "Mod List";

        // Adding listeners to save slot buttons to update modlist when selected
        for (int i = 0; i < 4; i++)
        {
            string slotpath = "/MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/SlotGroup/SlotGroup H/";
            GameObject slot = GameObject.Find($"{slotpath}SaveSlotContainer_Slot{i}/SaveSlot{i}");
            slot.GetComponent<SaveSlotUIButton>().OnSelectedEvent.AddListener(DisplayModListForSelectedFile);
        }
        
        // Mod list window
        GameObject ScrollView = GameObject.Find("/MenuLogic").transform.Find("Scroll View").gameObject;
        ModList_ScrollRect = ObjectUtils.InstantiateAutoReference(ScrollView, ScrollView.transform.parent, false);
        ModList_ScrollRect.SetActive(true);
        ModList_ScrollRect.name = "Modlist";
        ModList_ScrollRect.GetComponent<RectTransform>().anchoredPosition = new Vector2(140, -237);
        ModList_ScrollRect.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 610);
        

        // Adding listeners to start game and back button to toggle visibility of mod list
        GameObject.Find("MenuLogic/MainMenuLogic/Providers/MenuUIPanel/Button Layout/MainMenuButton_Slot").GetComponent<UIControlButton>().onSubmit.AddListener(() => ModList_ScrollRect.SetActive(true));
        GameObject.Find("/MenuLogic/MainMenuLogic/Providers/StartGame SaveSlotPanel/SavePanel_BackButton").GetComponent<UIControlButton>().onSubmit.AddListener(() => ModList_ScrollRect.SetActive(false));
        
        // Cleanup of mod list elements and preparation of DisplayModListForSelectedFile()
        buttonClone = GameObject.Find("/MenuLogic/Modlist/Viewport/Content/ButtonCandidate1");
        buttonClone.name = "buttonClone";
        buttonClone.SetActive(false);
        GameObject.Destroy(GameObject.Find("/MenuLogic/Modlist/Viewport/Content/ButtonCandidate2"));
        ModList_ScrollRect.SetActive(false);
    }
}
