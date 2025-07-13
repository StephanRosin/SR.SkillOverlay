// SkillOverlay.cs – Valheim BepInEx plug‑in
// Shows all skills > 15 sorted by level in a compact overlay on the left side
// Hides while inventory / chest is open, waits 0.5 s before reappearing
// C# 7.3 compatible – no target‑typed new or index‑from‑end syntax

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

[BepInPlugin("SR.SkillOverlay", "Skill Overlay", "1.3.0")]
public class SkillOverlay : BaseUnityPlugin
{
    private static SkillOverlay _instance;

    private readonly Harmony _harmony = new Harmony("SR.SkillOverlay");
    private readonly List<SkillRow> _rows = new List<SkillRow>();

    private GameObject _panel;
    private float _nextUpdate;

    private const float UPDATE_INTERVAL = 1f;    // refresh skills every second
    private const float BAR_WIDTH = 120f;  // yellow bar length
    private const float SHOW_DELAY = 0.5f;  // delay after closing container

    // cache for reflection lookups
    private static MethodInfo _invIsVisibleMethod;
    private static FieldInfo _curContainerField;

    // runtime helpers
    private bool _wasInvVisible;
    private float _showAgainTime;

    private void Awake()
    {
        _instance = this;
        _harmony.PatchAll();
    }

    // ------------------------------------------------------------
    // Create panel after Hud is ready
    // ------------------------------------------------------------
    [HarmonyPatch(typeof(Hud), "Awake")]
    private static class Hud_Awake_Patch
    {
        private static void Postfix() => _instance.CreatePanel();
    }

    private void CreatePanel()
    {
        if (_panel != null) return;

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Logger.LogWarning("SkillOverlay: Canvas not found yet – will retry later");
            return;
        }

        _panel = new GameObject("SkillOverlayPanel", typeof(RectTransform));
        _panel.transform.SetParent(canvas.transform, false);

        RectTransform rt = _panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(15f, 0f); // align with food buff columns

        Logger.LogInfo("SkillOverlay: Panel created");
    }

    // ------------------------------------------------------------
    private void Update()
    {
        if (_panel == null) return;

        // 1) determine if any inventory / chest UI is visible
        bool invVisible = false;
        var invGui = InventoryGui.instance;
        if (invGui != null)
        {
            if (_invIsVisibleMethod == null)
                _invIsVisibleMethod = typeof(InventoryGui).GetMethod("IsVisible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_curContainerField == null)
                _curContainerField = typeof(InventoryGui).GetField("m_currentContainer", BindingFlags.Instance | BindingFlags.NonPublic);

            if (_invIsVisibleMethod != null)
                invVisible |= (bool)_invIsVisibleMethod.Invoke(invGui, null);
            if (_curContainerField != null)
                invVisible |= _curContainerField.GetValue(invGui) != null;
        }

        // 2) visibility handling with delay
        if (invVisible)
        {
            if (_panel.activeSelf) _panel.SetActive(false);
            _wasInvVisible = true;
            _showAgainTime = Time.time + SHOW_DELAY;
            return; // skip updates while hidden
        }
        else
        {
            if (_wasInvVisible)
            {
                // wait until delay elapsed
                if (Time.time < _showAgainTime) return;
                _panel.SetActive(true);
                _wasInvVisible = false;
            }
        }

        // 3) update skills regularly
        if (Player.m_localPlayer == null) return;
        if (Time.time < _nextUpdate) return;
        _nextUpdate = Time.time + UPDATE_INTERVAL;
        RefreshRows();
    }

    // ------------------------------------------------------------
    private void RefreshRows()
    {
        List<Skills.Skill> list = Player.m_localPlayer
            .GetSkills()
            .GetSkillList()
            .Where(s => s.m_level > 15f)
            .OrderByDescending(s => s.m_level)
            .ToList();

        // ensure row count matches
        while (_rows.Count < list.Count)
            _rows.Add(new SkillRow(_panel.transform));
        while (_rows.Count > list.Count)
        {
            _rows[_rows.Count - 1].Destroy();
            _rows.RemoveAt(_rows.Count - 1);
        }

        // set data
        for (int i = 0; i < list.Count; i++)
            _rows[i].Set(list[i], i);
    }

    // ------------------------------------------------------------
    private class SkillRow
    {
        private readonly GameObject _root;
        private readonly Image _icon;
        private readonly Image _barFill;
        private readonly Text _label;

        private static readonly MethodInfo GetSkillLevelMethod =
            typeof(Player).GetMethod("GetSkillLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public SkillRow(Transform parent)
        {
            _root = new GameObject("SkillRow", typeof(RectTransform));
            _root.transform.SetParent(parent, false);

            RectTransform rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            // --- Bar container (background + fill)
            GameObject barObj = new GameObject("Bar", typeof(RectTransform));
            barObj.transform.SetParent(_root.transform, false);
            RectTransform barRT = barObj.GetComponent<RectTransform>();
            barRT.anchoredPosition = new Vector2(72, -16); // 24px icon + 16px gap
            barRT.sizeDelta = new Vector2(BAR_WIDTH, 8);

            Image barBack = barObj.AddComponent<Image>();
            barBack.color = new Color(0f, 0f, 0f, 0.4f);

            // --- Bar fill
            _barFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _barFill.transform.SetParent(barObj.transform, false);
            _barFill.color = new Color(1f, 0.85f, 0f);
            _barFill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _barFill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _barFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _barFill.rectTransform.sizeDelta = new Vector2(0, 8);

            // --- Icon
            _icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _icon.transform.SetParent(_root.transform, false);
            _icon.rectTransform.sizeDelta = new Vector2(24, 24);
            _icon.rectTransform.anchoredPosition = new Vector2(0, -12);
            _icon.transform.SetAsLastSibling();

            // --- Label
            _label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _label.transform.SetParent(_root.transform, false);
            _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontSize = 13;
            _label.alignment = TextAnchor.MiddleLeft;
            _label.color = new Color(1f, 0.9f, 0f);
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.rectTransform.anchoredPosition = new Vector2(184, -12);
        }

        public void Set(Skills.Skill skill, int index)
        {
            // vertical stacking
            _root.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -index * 28);

            if (skill.m_info.m_icon != null) _icon.sprite = skill.m_info.m_icon;

            // base vs effective level
            int baseLv = Mathf.FloorToInt(skill.m_level);
            int effLv = baseLv;
            if (GetSkillLevelMethod != null)
            {
                object res = GetSkillLevelMethod.Invoke(Player.m_localPlayer, new object[] { skill.m_info.m_skill });
                if (res is float f) effLv = Mathf.FloorToInt(f);
            }
            int bonus = Mathf.Max(0, effLv - baseLv);

            // bar size
            _barFill.rectTransform.sizeDelta = new Vector2(BAR_WIDTH * Mathf.Clamp01(baseLv / 100f), 8);

            // text
            _label.text = bonus > 0
                ? string.Format("{0} {1} <color=#00b4ff>+{2}</color>", skill.m_info.m_skill, baseLv, bonus)
                : string.Format("{0} {1}", skill.m_info.m_skill, baseLv);
        }

        public void Destroy()
        {
            if (_root) UnityEngine.Object.Destroy(_root);
        }
    }
}
