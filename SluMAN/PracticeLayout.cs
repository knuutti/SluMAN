using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Builds a Practice window's own controls in the layout every game shares
    /// (.claude/ui-uniform-plan.md): two columns of 312-wide group boxes, each group in a fixed
    /// slot. Add the left column's groups in slot order (Level / Map, Job, Reload, Saved
    /// Positions), then the right column's (Game, Utility, Toggles); a group a game doesn't have
    /// is left out and the ones below it move up.
    /// </summary>
    public class PracticeLayout
    {
        public const int GroupWidth = 312;
        // Inside a group: the full width a row's controls share, and where it starts.
        public const int InnerX = 10;
        public const int InnerWidth = 292;

        private const int LeftX = 12;
        private const int RightX = 336;
        private const int Top = 32;
        private const int GroupGap = 6;

        // A control row: a 21-high combo box or text box, its 23-high buttons a pixel higher.
        private const int FirstRowY = 20;
        private const int RowHeight = 29;
        private const int ControlGap = 6;
        // A status row: a label and its value.
        private const int StatusRowHeight = 22;
        // Toggles in two sub-columns.
        private const int ToggleRowHeight = 23;
        private const int ToggleColumnX = 160;

        public const string NotAvailableYet = "Not available yet.";

        private readonly Form host;
        private readonly ToolTip tip = new ToolTip();
        private int leftY = Top;
        private int rightY = Top;

        public PracticeLayout(Form host)
        {
            this.host = host;
            host.FormClosed += (sender, e) => tip.Dispose();
        }

        /// <summary>The window's tooltips.</summary>
        public ToolTip ToolTip
        {
            get { return tip; }
        }

        /// <summary>
        /// The menu bar with its Tools menu, which <see cref="GameSession.FillPracticeToolsMenu"/>
        /// fills once the session exists.
        /// </summary>
        public ToolStripMenuItem AddMenu()
        {
            MenuStrip menuStrip = new MenuStrip();
            menuStrip.BackColor = SystemColors.ControlDark;
            ToolStripMenuItem tools = new ToolStripMenuItem("Tools");
            menuStrip.Items.Add(tools);
            host.MainMenuStrip = menuStrip;
            host.Controls.Add(menuStrip);
            return tools;
        }

        /// <summary>Sets up a code-built Practice window: title, fixed size and the app's icon.</summary>
        public static void SetUpWindow(Form form, string name, string title)
        {
            form.Name = name;
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.MaximizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Font = new Font("Microsoft Sans Serif", 8.25F);
            form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            // The tabs fit the height to the content; the width is fixed.
            form.ClientSize = new Size(660, 400);
        }

        /// <summary>The y of a control row in a group (0-based).</summary>
        public static int Row(int row)
        {
            return FirstRowY + row * RowHeight;
        }

        /// <summary>A group's height for this many control rows.</summary>
        public static int RowsHeight(int rows)
        {
            return rows * RowHeight + 22;
        }

        public GroupBox AddLeft(string text, int rows)
        {
            return AddGroup(text, LeftX, ref leftY, RowsHeight(rows));
        }

        public GroupBox AddRight(string text, int rows)
        {
            return AddGroup(text, RightX, ref rightY, RowsHeight(rows));
        }

        private GroupBox AddGroup(string text, int x, ref int y, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(GroupWidth, height);
            host.Controls.Add(group);
            y += height + GroupGap;
            return group;
        }

        // Slot L1 / L2: a list across the top row, a second list and Load below it.

        /// <summary>
        /// A Level / Map or Job group: <paramref name="first"/> across the top row,
        /// <paramref name="second"/> (if any) and the Load button below, and any
        /// <paramref name="extraRows"/> (such as Sly 4's character) between them.
        /// </summary>
        public GroupBox AddPickerGroup(string text, ComboBox first, ComboBox second, EventHandler load, params ComboBox[] extraRows)
        {
            GroupBox group = AddLeft(text, 2 + extraRows.Length);
            AddComboBox(group, first, InnerX, Row(0), InnerWidth);
            for (int i = 0; i < extraRows.Length; i++)
            {
                AddComboBox(group, extraRows[i], InnerX, Row(1 + i), InnerWidth);
            }
            int lastRow = 1 + extraRows.Length;
            if (second != null)
            {
                AddComboBox(group, second, InnerX, Row(lastRow), 193);
            }
            AddButton(group, "Load", 208, Row(lastRow) - 1, 94, load);
            return group;
        }

        // Slot L3: Reload.

        /// <summary>
        /// The Reload group: a row of buttons, and below them the character to reload as (Sly 2, 3)
        /// if <paramref name="character"/> is given.
        /// </summary>
        public GroupBox AddReloadGroup(Button[] buttons, ComboBox character)
        {
            GroupBox group = AddLeft("Reload", character != null ? 2 : 1);
            AddButtonRow(group, Row(0) - 1, buttons);
            List<string> placeholders = new List<string>();
            foreach (Button button in buttons)
            {
                if (!button.Enabled && NotAvailableYet.Equals(button.Tag))
                {
                    placeholders.Add(button.Text);
                }
            }
            if (placeholders.Count > 0)
            {
                tip.SetToolTip(group, $"{string.Join(", ", placeholders)}: {NotAvailableYet}");
            }
            if (character != null)
            {
                AddLabel(group, "Character:", InnerX, Row(1) + 3);
                AddComboBox(group, character, 70, Row(1), 133);
            }
            return group;
        }

        // Slot L4: Saved Positions.

        /// <summary>A slot list, Save and Load; the slot is the game's selected position slot.</summary>
        public GroupBox AddSavedPositionsGroup(IGame game, ComboBox slotComboBox, EventHandler save, EventHandler load)
        {
            GroupBox group = AddLeft("Saved Positions", 1);
            for (int slot = 1; slot <= IGame.PositionSlotCount; slot++)
            {
                slotComboBox.Items.Add($"Slot {slot}");
            }
            slotComboBox.SelectedIndex = game.selectedPositionIndex;
            slotComboBox.SelectedIndexChanged += (sender, e) => game.selectedPositionIndex = slotComboBox.SelectedIndex;
            // The Player tab's slot grid may have picked another slot meanwhile.
            slotComboBox.DropDown += (sender, e) => slotComboBox.SelectedIndex = game.selectedPositionIndex;
            AddComboBox(group, slotComboBox, InnerX, Row(0), 92);
            AddButton(group, "Save", 108, Row(0) - 1, 94, save);
            AddButton(group, "Load", 208, Row(0) - 1, 94, load);
            return group;
        }

        // Slot R1: Game.

        /// <summary>The Game group: a row per name, its value in the label beside it.</summary>
        public GroupBox AddStatusGroup(string[] names, Label[] values)
        {
            GroupBox group = AddGroup("Game", RightX, ref rightY, FirstRowY + names.Length * StatusRowHeight + 4);
            for (int i = 0; i < names.Length; i++)
            {
                int y = FirstRowY + i * StatusRowHeight;
                AddLabel(group, names[i] + ":", InnerX, y);
                values[i].Text = "-";
                values[i].AutoEllipsis = true;
                values[i].Location = new Point(80, y);
                values[i].Size = new Size(222, 13);
                group.Controls.Add(values[i]);
            }
            return group;
        }

        // Slot R2: Utility.

        /// <summary>A value to set: its name, a text box or list, and a Set button (Enter sets it too).</summary>
        public class ValueSetter
        {
            public string name;
            public Control input;
            public EventHandler set;
        }

        public static ValueSetter Setter(string name, Control input, EventHandler set)
        {
            return new ValueSetter { name = name, input = input, set = set };
        }

        /// <summary>
        /// A value another game can set and this one can't yet: its row shows, disabled and
        /// calling nothing.
        /// </summary>
        public static ValueSetter PlaceholderSetter(string name)
        {
            return new ValueSetter { name = name, input = new TextBox(), set = null };
        }

        /// <summary>The Utility group: a row per value, with its own Set button.</summary>
        public GroupBox AddUtilityGroup(params ValueSetter[] setters)
        {
            GroupBox group = AddRight("Utility", setters.Length);
            List<string> placeholders = new List<string>();
            for (int i = 0; i < setters.Length; i++)
            {
                ValueSetter setter = setters[i];
                if (setter.set == null)
                {
                    setter.input.Enabled = false;
                    placeholders.Add("Set " + setter.name);
                }
                AddLabel(group, setter.name + ":", InnerX, Row(i) + 3);
                setter.input.Location = new Point(70, Row(i));
                setter.input.Size = new Size(133, 21);
                ComboBox comboBox = setter.input as ComboBox;
                if (comboBox != null)
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                group.Controls.Add(setter.input);
                Button button = AddButton(group, "Set " + setter.name, 208, Row(i) - 1, 94, setter.set);
                button.Enabled = setter.set != null;
                TextBox textBox = setter.input as TextBox;
                if (textBox != null && setter.set != null)
                {
                    func.BindEnter(textBox, button);
                }
            }
            if (placeholders.Count > 0)
            {
                tip.SetToolTip(group, $"{string.Join(", ", placeholders)}: {NotAvailableYet}");
            }
            return group;
        }

        // Slot R3: Toggles.

        /// <summary>A toggle: its text and what it does when ticked or unticked (null for none).</summary>
        public static CheckBox Toggle(CheckBox checkBox, string text, EventHandler changed)
        {
            checkBox.Text = text;
            checkBox.AutoSize = true;
            if (changed != null)
            {
                checkBox.CheckedChanged += changed;
            }
            return checkBox;
        }

        /// <summary>
        /// A toggle another game has and this one doesn't yet: disabled, calling nothing, so the
        /// layout already matches (.claude/ui-uniform-plan.md, Future features).
        /// </summary>
        public CheckBox PlaceholderToggle(string text)
        {
            CheckBox checkBox = Toggle(new CheckBox(), text, null);
            checkBox.Enabled = false;
            tip.SetToolTip(checkBox, NotAvailableYet);
            return checkBox;
        }

        /// <summary>
        /// The Toggles group, in the shared order: the first half down the left sub-column, the
        /// rest down the right.
        /// </summary>
        public GroupBox AddTogglesGroup(params CheckBox[] toggles)
        {
            int rows = (toggles.Length + 1) / 2;
            GroupBox group = AddGroup("Toggles", RightX, ref rightY, FirstRowY + rows * ToggleRowHeight + 4);
            for (int i = 0; i < toggles.Length; i++)
            {
                int column = i / rows;
                toggles[i].Location = new Point(InnerX + column * (ToggleColumnX - InnerX), FirstRowY + (i % rows) * ToggleRowHeight);
                group.Controls.Add(toggles[i]);
            }
            return group;
        }

        // Controls.

        public static Button MakeButton(string text, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.UseVisualStyleBackColor = true;
            if (click != null)
            {
                button.Click += click;
            }
            return button;
        }

        /// <summary>A button another game has and this one doesn't yet: disabled, calling nothing.</summary>
        public Button PlaceholderButton(string text)
        {
            Button button = MakeButton(text, null);
            button.Enabled = false;
            // A disabled button shows no tooltip of its own, so its group's says it (AddReloadGroup).
            button.Tag = NotAvailableYet;
            return button;
        }

        /// <summary>Lays buttons out across a row, sharing its width.</summary>
        public static void AddButtonRow(Control parent, int y, Button[] buttons)
        {
            int width = (InnerWidth - (buttons.Length - 1) * ControlGap) / buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Location = new Point(InnerX + i * (width + ControlGap), y);
                buttons[i].Size = new Size(width, 23);
                parent.Controls.Add(buttons[i]);
            }
        }

        public static Button AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
        {
            Button button = MakeButton(text, click);
            button.Location = new Point(x, y);
            button.Size = new Size(width, 23);
            parent.Controls.Add(button);
            return button;
        }

        public static void AddComboBox(Control parent, ComboBox comboBox, int x, int y, int width)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Location = new Point(x, y);
            comboBox.Size = new Size(width, 21);
            parent.Controls.Add(comboBox);
        }

        public static void AddLabel(Control parent, string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            parent.Controls.Add(label);
        }
    }
}
