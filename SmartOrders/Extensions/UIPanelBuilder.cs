namespace SmartOrders.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Builder;
using UnityEngine;

// ReSharper disable once InconsistentNaming
public static class UIPanelBuilderExtensions {

    /// <summary> Wacky popup menu (Game do not have one, but it does have Dropdown). </summary>
    /// <param name="builder"> </param>
    /// <param name="placeholder"> Dropdown first value - this value will always be selected. </param>
    /// <param name="items"> </param>
    /// <returns> </returns>
    public static RectTransform AddPopupMenu(this UIPanelBuilder builder, string placeholder, IEnumerable<PopupMenuItem> items) {
        var itemArray = items.ToArray();
        List<string> values = [placeholder, .. itemArray.Select(o => o.DisplayName)];

        TMP_Dropdown dropdown = null!;
        var rect = builder.AddDropdown(values, 0, OnSelect)!;
        rect.FlexibleWidth();
        dropdown = rect.GetComponent<TMP_Dropdown>()!;
        dropdown.MultiSelect = false;
        return rect;

        void OnSelect(int o) {
            if (o == 0) {
                // ignore placeholder selection
                return;
            }

            itemArray[o - 1].OnSelected();

            // ReSharper disable once AccessToModifiedClosure
            dropdown.value = 0;
        }
    }

}

public class PopupMenuItem(string displayName, Action onSelected) {

    /// <summary> Item`s display name. </summary>
    public string DisplayName { get; } = displayName;

    /// <summary> Action executed, when item selected. </summary>
    public Action OnSelected { get; } = onSelected;

}