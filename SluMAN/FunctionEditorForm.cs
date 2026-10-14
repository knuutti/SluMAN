using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Adds or edits a function row in the memory window: a name and a formula that uses the
    /// other watches, with a live preview of the result.
    /// </summary>
    public class FunctionEditorForm : Form
    {
        private readonly TextBox nameTextBox = new TextBox();
        private readonly TextBox formulaTextBox = new TextBox();
        private readonly Label previewLabel = new Label();
        private readonly ListBox watchesListBox = new ListBox();
        private readonly Timer previewTimer = new Timer();

        private readonly WatchFormula.VariableLookup lookup;
        private readonly Func<string, bool> nameTaken;

        /// <summary>The name to show, once the dialog returns OK.</summary>
        public string FunctionName { get; private set; }

        /// <summary>The parsed formula, once the dialog returns OK.</summary>
        public WatchFormula Formula { get; private set; }

        /// <param name="watchNames">Watches the formula can use, offered for inserting.</param>
        /// <param name="lookup">Current values, for the preview.</param>
        /// <param name="nameTaken">True when another row already has the name.</param>
        public FunctionEditorForm(string title, string name, string formula, IEnumerable<string> watchNames,
            WatchFormula.VariableLookup lookup, Func<string, bool> nameTaken)
        {
            this.lookup = lookup;
            this.nameTaken = nameTaken;

            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 338);

            AddLabel("Name:", 12, 12);
            nameTextBox.Location = new Point(80, 9);
            nameTextBox.Size = new Size(348, 20);
            nameTextBox.Text = name;
            nameTextBox.TextChanged += (sender, e) => UpdatePreview();
            Controls.Add(nameTextBox);

            AddLabel("Formula:", 12, 41);
            formulaTextBox.Location = new Point(80, 38);
            formulaTextBox.Size = new Size(348, 20);
            formulaTextBox.Text = formula;
            formulaTextBox.TextChanged += (sender, e) => UpdatePreview();
            Controls.Add(formulaTextBox);

            previewLabel.Location = new Point(77, 63);
            previewLabel.Size = new Size(351, 30);
            Controls.Add(previewLabel);

            AddLabel("Watches (double-click to insert):", 12, 98);
            watchesListBox.Location = new Point(12, 116);
            watchesListBox.Size = new Size(190, 186);
            watchesListBox.Items.AddRange(watchNames.Cast<object>().ToArray());
            watchesListBox.DoubleClick += watchesListBox_DoubleClick;
            Controls.Add(watchesListBox);

            Label help = new Label();
            help.Location = new Point(214, 98);
            help.Size = new Size(214, 200);
            help.Text =
                "Use watch names as numbers:\n" +
                "sqrt(speedX^2 + speedY^2)\n" +
                "Names with spaces go in braces:\n" +
                "{Speed X} * 30\n\n" +
                "Operators: + - * / % ^ ( )\n\n" +
                "Functions:\n" +
                "sqrt  abs  min  max  floor  ceil\n" +
                "round(x, digits)  pow(x, y)\n" +
                "sin  cos  tan  atan2(y, x)\n" +
                "deg  rad  log  clamp(x, low, high)\n\n" +
                "pi is 3.14159...";
            Controls.Add(help);

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(272, 305);
            okButton.Size = new Size(75, 23);
            okButton.Click += okButton_Click;
            Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(353, 305);
            cancelButton.Size = new Size(75, 23);
            cancelButton.DialogResult = DialogResult.Cancel;
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            // The watches keep changing, so the preview follows them.
            previewTimer.Interval = 200;
            previewTimer.Tick += (sender, e) => UpdatePreview();
            previewTimer.Start();
            FormClosed += (sender, e) => previewTimer.Dispose();

            UpdatePreview();
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            Controls.Add(label);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // A new function starts at the formula, since the name is filled in already.
            formulaTextBox.Focus();
            formulaTextBox.SelectionStart = formulaTextBox.TextLength;
        }

        private void watchesListBox_DoubleClick(object sender, EventArgs e)
        {
            if (watchesListBox.SelectedItem == null)
            {
                return;
            }
            formulaTextBox.SelectedText = WatchFormula.Reference(watchesListBox.SelectedItem.ToString());
            formulaTextBox.Focus();
        }

        /// <summary>Checks the formula; returns null when it's fine, otherwise what's wrong.</summary>
        private string Check(out WatchFormula formula, out double value)
        {
            value = double.NaN;
            string error;
            if (!WatchFormula.TryParse(formulaTextBox.Text, out formula, out error))
            {
                return error;
            }

            string name = CleanName();
            if (name == "")
            {
                return "Give the function a name.";
            }
            if (nameTaken(name))
            {
                return $"Another watch is already called \"{name}\". Formulas find watches by name, so pick another one.";
            }
            if (formula.Uses(name))
            {
                return "A function can't use its own value.";
            }

            try
            {
                value = formula.Evaluate(lookup);
            }
            catch (FormatException ex)
            {
                // A watch it uses doesn't exist.
                return ex.Message;
            }
            return null;
        }

        private void UpdatePreview()
        {
            WatchFormula formula;
            double value;
            string error = Check(out formula, out value);
            if (error != null)
            {
                previewLabel.ForeColor = Color.Firebrick;
                previewLabel.Text = error;
            }
            else
            {
                previewLabel.ForeColor = SystemColors.ControlText;
                previewLabel.Text = "= " + WatchFormula.Format(value);
            }
        }

        // Commas separate the fields in a watchlist file.
        private string CleanName()
        {
            return nameTextBox.Text.Replace(",", " ").Trim();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            WatchFormula formula;
            double value;
            if (Check(out formula, out value) != null)
            {
                // The preview already says what's wrong.
                UpdatePreview();
                return;
            }

            FunctionName = CleanName();
            Formula = formula;
            DialogResult = DialogResult.OK;
        }
    }
}
