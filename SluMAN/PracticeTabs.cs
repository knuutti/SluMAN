using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace racman
{
    /// <summary>
    /// A tool that shows the game's current state. Tabs keep a tool's form once it's created, so
    /// it reads the game again each time its tab is selected; a save loaded meanwhile would
    /// otherwise show the old values.
    /// </summary>
    public interface IRefreshOnShow
    {
        void RefreshFromGame();
    }

    /// <summary>
    /// Turns a Practice window into one window with tabs: the window's own controls on the first
    /// tab, and tool windows (gadgets, position editor, memory) embedded on the others. A tool is
    /// created the first time its tab is opened, and the window resizes to fit the selected tab.
    /// </summary>
    public class PracticeTabs
    {
        private class Tool
        {
            public TabPage page;
            public Func<Form> create;
            public Form form;
        }

        private readonly Form host;
        private readonly TabControl tabs = new TabControl();
        private readonly TabPage practicePage;
        private readonly Size practiceSize;
        private readonly List<Tool> tools = new List<Tool>();

        /// <summary>Raised when a tool's form has been created, so the host can keep a reference.</summary>
        public event Action<string, Form> ToolCreated;

        /// <summary>Raised when a tool's form closes, e.g. a gadget editor after Save.</summary>
        public event Action<string> ToolClosed;

        /// <summary>
        /// Moves every control of <paramref name="host"/> except its menu and status bar onto a
        /// "Practice" tab. Call at the end of the constructor, after the status line exists.
        /// </summary>
        public PracticeTabs(Form host, string practiceTitle)
        {
            this.host = host;

            MenuStrip menu = host.Controls.OfType<MenuStrip>().FirstOrDefault();
            StatusStrip status = host.Controls.OfType<StatusStrip>().FirstOrDefault();
            int top = menu != null ? menu.Height : 0;
            int bottom = status != null ? status.Height : 0;
            practiceSize = new Size(host.ClientSize.Width, host.ClientSize.Height - top - bottom);

            host.SuspendLayout();

            practicePage = new TabPage(practiceTitle);
            // Keep the window's own background, so its controls look as they did without tabs.
            practicePage.UseVisualStyleBackColor = false;
            practicePage.BackColor = host.BackColor;

            List<Control> content = host.Controls.Cast<Control>()
                .Where(c => !(c is MenuStrip) && !(c is StatusStrip))
                .ToList();
            foreach (Control control in content)
            {
                Point location = control.Location;
                host.Controls.Remove(control);
                control.Location = new Point(location.X, location.Y - top);
                practicePage.Controls.Add(control);
            }

            tabs.TabPages.Add(practicePage);
            tabs.Dock = DockStyle.Fill;
            tabs.SelectedIndexChanged += tabs_SelectedIndexChanged;
            host.Controls.Add(tabs);
            // Docked last, so it fills the space the menu and status bar leave.
            tabs.BringToFront();

            host.ResumeLayout(true);
            FitTo(practiceSize);
            // The tab header's real size is only known once the window exists, so fit again then.
            host.Load += (sender, e) => FitTo(practiceSize);
        }

        // Set while the host closes its tools, so they don't switch tabs on the way out.
        private bool closingAll;

        /// <summary>Adds a tab for a tool window, created by <paramref name="create"/> when first opened.</summary>
        public void AddTool(string title, Func<Form> create)
        {
            Tool tool = new Tool { page = new TabPage(title), create = create };
            tool.page.UseVisualStyleBackColor = true;
            tabs.TabPages.Add(tool.page);
            tools.Add(tool);
        }

        /// <summary>Selects a tool's tab, creating the tool if needed, and returns its form.</summary>
        public Form Show(string title)
        {
            Tool tool = tools.FirstOrDefault(t => t.page.Text == title);
            if (tool == null)
            {
                return null;
            }
            tabs.SelectedTab = tool.page;
            return EnsureCreated(tool);
        }

        /// <summary>Selects the first tab, the window's own controls.</summary>
        public void ShowPractice()
        {
            tabs.SelectedTab = practicePage;
        }

        /// <summary>The tool's form if it has been created and is still open, otherwise null.</summary>
        public Form Get(string title)
        {
            Tool tool = tools.FirstOrDefault(t => t.page.Text == title);
            return tool != null && tool.form != null && !tool.form.IsDisposed ? tool.form : null;
        }

        /// <summary>
        /// Has the tool on the selected tab read the game again, e.g. after a load. Tools on other
        /// tabs read it when their tab is selected.
        /// </summary>
        public void RefreshShownTool()
        {
            Tool tool = tools.FirstOrDefault(t => t.page == tabs.SelectedTab);
            if (tool == null || tool.form == null || tool.form.IsDisposed)
            {
                return;
            }
            IRefreshOnShow refreshable = tool.form as IRefreshOnShow;
            if (refreshable != null)
            {
                refreshable.RefreshFromGame();
            }
        }

        /// <summary>Closes every tool, so their own cleanup runs. Call when the host closes.</summary>
        public void CloseAll()
        {
            closingAll = true;
            foreach (Tool tool in tools)
            {
                if (tool.form != null && !tool.form.IsDisposed)
                {
                    tool.form.Close();
                }
            }
        }

        private Form EnsureCreated(Tool tool)
        {
            if (tool.form != null && !tool.form.IsDisposed)
            {
                return tool.form;
            }

            Form form = tool.create();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Location = Point.Empty;
            form.FormClosed += (sender, e) => OnToolClosed(tool);
            tool.page.Controls.Add(form);
            form.Show();
            tool.form = form;

            Action<string, Form> handler = ToolCreated;
            if (handler != null)
            {
                handler(tool.page.Text, form);
            }

            if (tabs.SelectedTab == tool.page)
            {
                FitTo(form.ClientSize);
            }
            return form;
        }

        private void OnToolClosed(Tool tool)
        {
            tool.form = null;
            Action<string> handler = ToolClosed;
            if (handler != null)
            {
                handler(tool.page.Text);
            }

            // A tool that closes itself (a gadget editor after Save) hands back to Practice.
            if (!closingAll && !host.IsDisposed && tabs.SelectedTab == tool.page)
            {
                host.BeginInvoke(new Action(ShowPractice));
            }
        }

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs.SelectedTab == practicePage)
            {
                FitTo(practiceSize);
                return;
            }

            Tool tool = tools.FirstOrDefault(t => t.page == tabs.SelectedTab);
            if (tool != null)
            {
                // A new form has just read the game in its constructor.
                bool existed = tool.form != null && !tool.form.IsDisposed;
                Form form = EnsureCreated(tool);
                IRefreshOnShow refreshable = form as IRefreshOnShow;
                if (existed && refreshable != null)
                {
                    refreshable.RefreshFromGame();
                }
                FitTo(form.ClientSize);
            }
        }

        /// <summary>Resizes the window so a tab's content of <paramref name="contentSize"/> fits exactly.</summary>
        private void FitTo(Size contentSize)
        {
            Rectangle page = tabs.DisplayRectangle;
            int extraWidth = tabs.Width - page.Width;
            int extraHeight = tabs.Height - page.Height;
            int outsideTabs = host.ClientSize.Height - tabs.Height;

            host.ClientSize = new Size(contentSize.Width + extraWidth, contentSize.Height + extraHeight + outsideTabs);
        }
    }
}
