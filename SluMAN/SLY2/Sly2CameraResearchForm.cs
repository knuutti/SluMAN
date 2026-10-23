using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using SluMAN.Memory;

namespace SluMAN
{
    /// <summary>
    /// Research tools for finding the Sly 2 camera source. This is intentionally separate from
    /// freecam: it identifies the address the game uses before camera control is implemented.
    /// </summary>
    public class Sly2CameraResearchForm : Form
    {
        private const float CameraTestRaise = 100f;
        private const int CameraTestMs = 2000;

        private static readonly uint[][] CameraSearchRanges =
        {
            new uint[] { 0x00010000, 0x10000000 },
            new uint[] { 0x30000000, 0x40000000 },
        };

        private readonly sly2 game;
        private List<uint> lastFinds;
        private uint lastVisibleCandidate;
        private readonly Button findButton = new Button();
        private readonly Label instructions = new Label();

        public Sly2CameraResearchForm(sly2 game)
        {
            this.game = game;
            Name = "Sly2CameraResearchForm";
            Text = "SluMAN :: Sly 2 Camera Research";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(410, 128);

            instructions.AutoSize = false;
            instructions.Location = new Point(12, 12);
            instructions.Size = new Size(386, 58);
            instructions.Text = "RPCS3 only. Search once, move the in-game camera, then search again. " +
                "Persistent position copies are raised one at a time to identify the rendered camera source.";
            Controls.Add(instructions);

            findButton.Text = "Find camera";
            findButton.Location = new Point(12, 82);
            findButton.Size = new Size(110, 28);
            findButton.Click += (sender, e) => FindCamera();
            Controls.Add(findButton);
        }

        private void FindCamera()
        {
            if (!(game.api is RPCS3))
            {
                MessageBox.Show(this, "Finding the camera requires RPCS3.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                if (sly2.addr.cameraMatrixAddress == 0)
                {
                    MessageBox.Show(this, "No camera matrix address is configured for this Sly 2 version.", Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                uint positionAddress = sly2.addr.cameraMatrixAddress + 0x30;
                byte[] position = game.api.ReadMemory(game.pid, positionAddress, 12);
                List<uint> found = game.FindMemory(position, CameraSearchRanges);
                List<uint> live = new List<uint>();
                if (lastFinds != null)
                {
                    foreach (uint address in found)
                    {
                        if (lastFinds.Contains(address) && address != positionAddress)
                        {
                            live.Add(address);
                        }
                    }
                }
                if (lastVisibleCandidate != 0 && !live.Contains(lastVisibleCandidate))
                {
                    live.Add(lastVisibleCandidate);
                }

                StringBuilder text = new StringBuilder();
                text.AppendLine($"Known camera copy: {positionAddress:X8}");
                text.AppendLine($"Position: {BitConverter.ToString(position).Replace("-", " ")}");
                text.AppendLine();
                text.AppendLine($"Found {found.Count} matching position(s):");
                foreach (uint address in found)
                {
                    bool again = lastFinds != null && lastFinds.Contains(address);
                    text.AppendLine($"{address:X8}{(again ? "  (also last time)" : "")}");
                }

                lastFinds = found;
                if (live.Count == 0)
                {
                    text.AppendLine();
                    text.AppendLine("Move the camera and search again to test persistent copies.");
                }
                else
                {
                    text.AppendLine();
                    text.AppendLine($"Testing {live.Count} persistent copy/copies (raise {CameraTestRaise.ToString(CultureInfo.InvariantCulture)}).");
                    foreach (uint address in live)
                    {
                        bool hasMatrix = game.IsCameraMatrixAt(address);
                        text.AppendLine($"{address:X8}: possible matrix at {address - 0x30:X8} = {hasMatrix}");
                        MessageBox.Show(this,
                            $"The candidate at 0x{address:X8} will be raised by {CameraTestRaise.ToString(CultureInfo.InvariantCulture)} for {CameraTestMs / 1000} seconds.\n\nWatch the game view.",
                            "Sly 2 camera research", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (hasMatrix)
                        {
                            DialogResult matrixTest = MessageBox.Show(this,
                                $"This candidate has a possible matrix at 0x{address - 0x30:X8}.\n\nTest the complete matrix instead of only its position?",
                                "Sly 2 camera research", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (matrixTest == DialogResult.Yes)
                            {
                                game.TestCameraMatrix(address, CameraTestRaise, CameraTestMs);
                            }
                            else
                            {
                                game.TestCameraCopy(address, CameraTestRaise, CameraTestMs);
                            }
                        }
                        else
                        {
                            game.TestCameraCopy(address, CameraTestRaise, CameraTestMs);
                        }
                        DialogResult moved = MessageBox.Show(this,
                            $"Did the rendered camera visibly rise while testing 0x{address:X8}?",
                            "Sly 2 camera research", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        text.AppendLine($"{address:X8}: rendered camera visibly moved = {moved == DialogResult.Yes}");
                        if (moved == DialogResult.Yes)
                        {
                            lastVisibleCandidate = address;
                        }
                    }
                }

                bool copied = true;
                try
                {
                    Clipboard.SetText(text.ToString());
                }
                catch
                {
                    copied = false;
                }
                text.AppendLine();
                text.AppendLine(copied ? "The result was copied to the clipboard." : "Couldn't copy the result to the clipboard.");
                MessageBox.Show(this, text.ToString(), "Sly 2 camera research",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Camera research failed: {ex.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
