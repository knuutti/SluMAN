using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace racman
{
    /// <summary>
    /// A status bar along the bottom of a form, for feedback that shouldn't interrupt with a
    /// dialog. Messages clear themselves after a few seconds; errors stay up longer.
    /// </summary>
    public class StatusLine
    {
        private const int InfoDurationMs = 6000;
        private const int ErrorDurationMs = 15000;

        private readonly Form form;
        private readonly StatusStrip strip = new StatusStrip();
        private readonly ToolStripStatusLabel label = new ToolStripStatusLabel();
        private readonly Timer clearTimer = new Timer();
        private readonly bool showsGlobalMessages;

        /// <param name="form">The form to add the bar to. It grows by the bar's height so nothing is covered.</param>
        /// <param name="showsGlobalMessages">Also show messages sent with <see cref="func.Status"/>.
        /// Game forms set this; tool windows only show their own messages.</param>
        public StatusLine(Form form, bool showsGlobalMessages)
        {
            this.form = form;
            this.showsGlobalMessages = showsGlobalMessages;

            label.Spring = true;
            label.TextAlign = ContentAlignment.MiddleLeft;
            strip.SizingGrip = false;
            strip.Items.Add(label);

            // Grow the form, then put every control back where it was. Controls anchored to the
            // bottom would otherwise move down behind the bar.
            form.SuspendLayout();
            Dictionary<Control, Rectangle> bounds = new Dictionary<Control, Rectangle>();
            foreach (Control control in form.Controls)
            {
                bounds[control] = control.Bounds;
            }
            form.ClientSize = new Size(form.ClientSize.Width, form.ClientSize.Height + strip.Height);
            foreach (KeyValuePair<Control, Rectangle> entry in bounds)
            {
                if (entry.Key.Dock == DockStyle.None)
                {
                    entry.Key.Bounds = entry.Value;
                }
            }
            form.Controls.Add(strip);
            form.ResumeLayout(true);

            clearTimer.Tick += clearTimer_Tick;

            if (showsGlobalMessages)
            {
                func.StatusMessage += Show;
            }
            form.FormClosed += form_FormClosed;
        }

        /// <summary>
        /// Shows a message. Safe to call from any thread.
        /// </summary>
        public void Show(string message, bool isError)
        {
            if (form.IsDisposed)
            {
                return;
            }

            if (form.InvokeRequired)
            {
                if (form.IsHandleCreated)
                {
                    try
                    {
                        form.BeginInvoke(new Action(() => Show(message, isError)));
                    }
                    catch
                    {
                        // The form closed between the check and the call.
                    }
                }
                return;
            }

            label.Text = message;
            label.ForeColor = isError ? Color.Firebrick : SystemColors.ControlText;
            label.ToolTipText = message;

            clearTimer.Stop();
            clearTimer.Interval = isError ? ErrorDurationMs : InfoDurationMs;
            clearTimer.Start();
        }

        public void Info(string message)
        {
            Show(message, false);
        }

        public void Error(string message)
        {
            Show(message, true);
        }

        private void clearTimer_Tick(object sender, EventArgs e)
        {
            clearTimer.Stop();
            label.Text = "";
            label.ToolTipText = "";
        }

        private void form_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (showsGlobalMessages)
            {
                func.StatusMessage -= Show;
            }
            clearTimer.Stop();
            clearTimer.Dispose();
        }
    }
}
