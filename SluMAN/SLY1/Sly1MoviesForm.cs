using System;
using System.Drawing;
using System.Windows.Forms;

namespace SluMAN
{
    /// <summary>
    /// Sly 1's movies: which movies of the in-game movie list are watched (unlocked). A watched
    /// movie can be skipped when it plays again; one that isn't watched can't. Every change is
    /// written to the game straight away. The controls are built in code.
    /// </summary>
    public class Sly1MoviesForm : Form, IRefreshOnShow
    {
        private const int RowHeight = 24;
        private const int ColumnWidth = 312;

        private readonly sly1 game;
        private readonly StatusLine statusLine;

        private readonly CheckBox[] movieCheckBoxes = new CheckBox[sly1.Movies.Length];
        private readonly Label valueLabel = new Label();
        private Label noteLabel;

        public Sly1MoviesForm(sly1 game, StatusLine statusLine)
        {
            this.game = game;
            this.statusLine = statusLine;
            Name = "Sly1MoviesForm";
            Text = "SluMAN :: Movies";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft Sans Serif", 8.25F);

            BuildControls();
            ClientSize = new Size(652, noteLabel.Bottom + 8);
            RefreshFromGame();
        }

        /// <summary>The bits of the movies in the list.</summary>
        private static uint ListedMovies
        {
            get { return (uint)((1L << sly1.Movies.Length) - 1); }
        }

        private void BuildControls()
        {
            ToolTip tip = new ToolTip();
            FormClosed += (sender, e) => tip.Dispose();

            int rows = (sly1.Movies.Length + 1) / 2;
            GroupBox moviesGroupBox = new GroupBox();
            moviesGroupBox.Text = "Watched movies";
            moviesGroupBox.Location = new Point(8, 8);
            moviesGroupBox.Size = new Size(636, 22 + rows * RowHeight + 4);
            Controls.Add(moviesGroupBox);

            for (int i = 0; i < sly1.Movies.Length; i++)
            {
                uint bit = 1u << i;
                CheckBox checkBox = new CheckBox();
                checkBox.Text = sly1.Movies[i].name;
                checkBox.AutoSize = true;
                // Down the left column, then the right, in the in-game list's order.
                checkBox.Location = new Point(10 + (i / rows) * ColumnWidth, 20 + (i % rows) * RowHeight);
                checkBox.Click += (sender, e) => SetMovie(bit, ((CheckBox)sender).Checked);
                tip.SetToolTip(checkBox, $"{sly1.Movies[i].file}, bit {i}");
                moviesGroupBox.Controls.Add(checkBox);
                movieCheckBoxes[i] = checkBox;
            }

            int buttonsY = moviesGroupBox.Bottom + 8;
            AddButton("All Watched", 8, buttonsY, (sender, e) => SetListedMovies(true));
            AddButton("None Watched", 104, buttonsY, (sender, e) => SetListedMovies(false));
            AddButton("Refresh", 200, buttonsY, (sender, e) => RefreshFromGame());
            valueLabel.AutoSize = true;
            valueLabel.Location = new Point(300, buttonsY + 5);
            Controls.Add(valueLabel);
            tip.SetToolTip(valueLabel, "The movie flags as the game stores them, at game state + 0x19F4.");

            noteLabel = new Label();
            noteLabel.AutoSize = true;
            noteLabel.Location = new Point(8, buttonsY + 31);
            noteLabel.ForeColor = SystemColors.GrayText;
            noteLabel.Text = "A watched movie can be skipped when it plays. Changes are written to the game straight away.";
            Controls.Add(noteLabel);
        }

        private void AddButton(string text, int x, int y, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(90, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            Controls.Add(button);
        }

        /// <summary>Reads the watched movies from the game.</summary>
        public void RefreshFromGame()
        {
            try
            {
                ShowMovies(game.GetMovies());
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't read the movies: {ex.Message}");
            }
        }

        private void ShowMovies(uint movies)
        {
            for (int i = 0; i < movieCheckBoxes.Length; i++)
            {
                movieCheckBoxes[i].Checked = (movies & (1u << i)) != 0;
            }
            valueLabel.Text = $"Value: 0x{movies:X8}";
        }

        private void SetMovie(uint bit, bool watched)
        {
            try
            {
                uint movies = game.GetMovies();
                movies = watched ? movies | bit : movies & ~bit;
                game.SetMovies(movies);
                ShowMovies(movies);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the movie: {ex.Message}");
                RefreshFromGame();
            }
        }

        /// <summary>Marks every listed movie watched or not, leaving any other bits as they are.</summary>
        private void SetListedMovies(bool watched)
        {
            try
            {
                uint movies = game.GetMovies();
                movies = watched ? movies | ListedMovies : movies & ~ListedMovies;
                game.SetMovies(movies);
                ShowMovies(movies);
            }
            catch (Exception ex)
            {
                statusLine.Error($"Couldn't change the movies: {ex.Message}");
            }
        }
    }
}
