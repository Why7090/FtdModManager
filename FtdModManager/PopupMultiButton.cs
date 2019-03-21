using System;
using System.Collections.Generic;
using BrilliantSkies.Ui.Consoles;
using BrilliantSkies.Ui.Consoles.Getters;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Buttons;
using BrilliantSkies.Ui.Special.PopUps.Internal;
using BrilliantSkies.Ui.Tips;
using UnityEngine;

namespace FtdModManager
{
    public class PopupMultiButton : AbstractPopup<PopMultiButton>
    {
        public readonly string message;
        public readonly List<ButtonOfPopup> buttons = new List<ButtonOfPopup>();
        private bool wrapText;

        public PopupMultiButton(string title, string message, bool wrapText = true) : base(title, new PopMultiButton())
        {
            this.message = message;
            this.wrapText = wrapText;
        }

        public PopupMultiButton AddButton(string buttonText, Action<PopupMultiButton> onClick = null, bool closeAfterClick = true, ToolTip toolTip = null)
        {
            buttons.Add(new ButtonOfPopup(buttonText, onClick, closeAfterClick, toolTip));
            return this;
        }

        protected override void AddContentToWindow(ConsoleWindow window)
        {
            Helper.Log("AddContentToWindow");
            var screenSegmentStandard = window.Screen.CreateStandardSegment(InsertPosition.OnCursor);
            screenSegmentStandard.AddInterpretter(new SubjectiveDisplay<PopMultiButton>(_focus, M.m((PopMultiButton I) => message), null)
            {
                WrapText = wrapText,
                Justify = TextAnchor.UpperLeft
            });
            window.Screen.CreateSpace(0);
            var seg = window.Screen.CreateTableSegment(buttons.Count, 1);
            seg.SqueezeTable = false;
            foreach (var button in buttons)
            {
                seg.AddInterpretter(SubjectiveButton<PopMultiButton>.Quick(
                    _focus, button.label, button.toolTip, x =>
                {
                    button.action?.Invoke(this);
                    if (button.closeAfterClick)
                        x.Do();
                }));
            }
        }
    }

    public class ButtonOfPopup
    {
        public string label;
        public bool closeAfterClick;
        public Action<PopupMultiButton> action;
        public ToolTip toolTip;

        public ButtonOfPopup(string label, Action<PopupMultiButton> onClick = null, bool closeAfterClick = true, ToolTip toolTip = null)
        {
            action = onClick;
            this.label = label;
            this.closeAfterClick = closeAfterClick;
            this.toolTip = toolTip ?? new ToolTip(label);
        }
    }

    public class PopMultiButton : Pop
    {
        public void Do() => Done = true;
    }
}
