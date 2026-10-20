using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// The languages of the PAL Sly 2 and Sly 3, and a Tools → Language menu that switches them.
    /// The game keeps the language as a 4-byte index; writing a new one and reloading loads the
    /// game's text and voices in that language.
    /// </summary>
    public static class GameLanguages
    {
        // Sly 2 (PAL) language indexes.
        public static readonly KeyValuePair<int, string>[] Sly2 = new KeyValuePair<int, string>[]
        {
            new KeyValuePair<int, string>(0, "English"),
            new KeyValuePair<int, string>(1, "French"),
            new KeyValuePair<int, string>(2, "Italian"),
            new KeyValuePair<int, string>(3, "German"),
            new KeyValuePair<int, string>(4, "Spanish"),
            new KeyValuePair<int, string>(5, "Dutch"),
            new KeyValuePair<int, string>(6, "Portuguese"),
            new KeyValuePair<int, string>(7, "Danish"),
            new KeyValuePair<int, string>(8, "Finnish"),
            new KeyValuePair<int, string>(9, "Norwegian"),
            new KeyValuePair<int, string>(10, "Swedish"),
        };

        // Sly 3 (PAL) language indexes, from SluMAN-EXT. They start at 1, and Portuguese, Finnish
        // and the others are in a different order than on Sly 2.
        public static readonly KeyValuePair<int, string>[] Sly3 = new KeyValuePair<int, string>[]
        {
            new KeyValuePair<int, string>(1, "English"),
            new KeyValuePair<int, string>(2, "French"),
            new KeyValuePair<int, string>(3, "Italian"),
            new KeyValuePair<int, string>(4, "German"),
            new KeyValuePair<int, string>(5, "Spanish"),
            new KeyValuePair<int, string>(6, "Portuguese"),
            new KeyValuePair<int, string>(7, "Dutch"),
            new KeyValuePair<int, string>(8, "Danish"),
            new KeyValuePair<int, string>(9, "Norwegian"),
            new KeyValuePair<int, string>(10, "Swedish"),
            new KeyValuePair<int, string>(11, "Finnish"),
        };

        /// <summary>
        /// Adds a Language submenu with the game's languages to the given menu. Opening it ticks
        /// the game's current language; picking one calls setLanguage, which writes it and reloads.
        /// </summary>
        public static void AddMenu(ToolStripMenuItem menu, KeyValuePair<int, string>[] languages, StatusLine statusLine, Func<int> getLanguage, Action<int> setLanguage)
        {
            ToolStripMenuItem languageMenu = new ToolStripMenuItem("Language");

            foreach (KeyValuePair<int, string> language in languages)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(language.Value);
                item.Tag = language.Key;
                item.Click += (sender, e) =>
                {
                    try
                    {
                        setLanguage((int)item.Tag);
                        statusLine.Info($"Reloading in {item.Text}.");
                    }
                    catch (Exception ex)
                    {
                        statusLine.Error($"Couldn't change the language: {ex.Message}");
                    }
                };
                languageMenu.DropDownItems.Add(item);
            }

            languageMenu.DropDownOpening += (sender, e) =>
            {
                int current = getLanguage();
                foreach (ToolStripMenuItem item in languageMenu.DropDownItems)
                {
                    item.Checked = (int)item.Tag == current;
                }
            };

            // Before the separator above Power Off / Reboot, with the other game tools.
            int index = menu.DropDownItems.Count;
            for (int i = 0; i < menu.DropDownItems.Count; i++)
            {
                if (menu.DropDownItems[i] is ToolStripSeparator)
                {
                    index = i;
                    break;
                }
            }
            menu.DropDownItems.Insert(index, languageMenu);
        }
    }
}
