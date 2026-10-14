using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SluMAN
{
    /// <summary>
    /// Two-click confirmation in place of a Yes/No dialog. The first click turns the button red
    /// and changes its text; a second click within a few seconds confirms. Waiting, or moving
    /// focus elsewhere, puts the button back.
    /// </summary>
    public static class ConfirmButton
    {
        private const int ArmedDurationMs = 3000;

        private class ArmedState
        {
            public string text;
            public Color backColor;
            public Color foreColor;
            public bool useVisualStyleBackColor;
            public Timer timer;
        }

        private static readonly Dictionary<Button, ArmedState> armed = new Dictionary<Button, ArmedState>();

        /// <summary>
        /// Call first thing in the button's Click handler. Returns true when this click confirms
        /// the action, false when it only armed the button.
        /// </summary>
        /// <param name="confirmText">What the button says while armed, such as "Confirm".</param>
        /// <param name="statusLine">Optional: where to explain what the second click does.</param>
        /// <param name="message">The explanation, such as "Click again to delete Route 1."</param>
        public static bool Confirm(Button button, string confirmText, StatusLine statusLine = null, string message = null)
        {
            if (armed.ContainsKey(button))
            {
                Disarm(button);
                return true;
            }

            ArmedState state = new ArmedState
            {
                text = button.Text,
                backColor = button.BackColor,
                foreColor = button.ForeColor,
                useVisualStyleBackColor = button.UseVisualStyleBackColor,
                timer = new Timer()
            };
            armed[button] = state;

            button.Text = confirmText;
            button.UseVisualStyleBackColor = false;
            button.BackColor = Color.Firebrick;
            button.ForeColor = Color.White;

            state.timer.Interval = ArmedDurationMs;
            state.timer.Tick += (sender, e) => Disarm(button);
            state.timer.Start();
            button.Leave += button_Leave;
            button.Disposed += button_Disposed;

            if (statusLine != null && message != null)
            {
                statusLine.Info(message);
            }
            return false;
        }

        private static void Disarm(Button button)
        {
            ArmedState state;
            if (!armed.TryGetValue(button, out state))
            {
                return;
            }
            armed.Remove(button);

            state.timer.Stop();
            state.timer.Dispose();
            button.Leave -= button_Leave;
            button.Disposed -= button_Disposed;

            if (!button.IsDisposed)
            {
                button.Text = state.text;
                button.BackColor = state.backColor;
                button.ForeColor = state.foreColor;
                button.UseVisualStyleBackColor = state.useVisualStyleBackColor;
            }
        }

        private static void button_Leave(object sender, EventArgs e)
        {
            Disarm((Button)sender);
        }

        private static void button_Disposed(object sender, EventArgs e)
        {
            Disarm((Button)sender);
        }
    }
}
