﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;

namespace Sudoku
{
    public partial class Form1 : Form
    {
        const int CellSize = 75;

        static readonly Random Random = new();
        
        ArchipelagoSession session;
        DeathLinkService deathLinkService;

        public Form1()
        {
            AutoScaleMode = AutoScaleMode.None;

            InitializeComponent();
            
            createCells();

            startNewGame();
        }

        SudokuCell[,] cells = new SudokuCell[9, 9];

        void createCells()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    var cell = new SudokuCell();

                    // Create 81 cells for with styles and locations based on the index
                    cell.Font = new Font(SystemFonts.DefaultFont.FontFamily, 20);
                    cell.Size = new Size(CellSize, CellSize);
                    cell.ForeColor = SystemColors.ControlDarkDark;
                    cell.Location = new Point(i * CellSize, j * CellSize);
                    cell.BackColor = ((i / 3) + (j / 3)) % 2 == 0 ? SystemColors.Control : Color.LightGray;
                    cell.FlatStyle = FlatStyle.Flat;
                    cell.FlatAppearance.BorderColor = Color.Black;
                    cell.X = i;
                    cell.Y = j;

                    // Assign key press event for each cells
                    cell.KeyPress += cell_keyPressed;
                    cell.MouseEnter += Cell_MouseEnter;

                    cells[i, j] = cell;
                    panel1.Controls.Add(cell);
                }
            }
        }

        SudokuCell activeCell;

        void Cell_MouseEnter(object sender, EventArgs e)
        {
            if (sender is SudokuCell sudokuCell)
                activeCell = sudokuCell;
        }

        void cell_keyPressed(object sender, KeyPressEventArgs e)
        {
            var cell = activeCell;

            // Do nothing if the cell is locked
            if (cell.IsLocked)
                return;

            if (e.KeyChar == '\b' && cell.Text.Length >= 1)
                cell.Text = cell.Text.Remove(cell.Text.Length - 1);
            else if (e.KeyChar >= '1' && e.KeyChar <= '9')
            {
                var number = int.Parse(e.KeyChar.ToString()).ToString();

                if (!cell.Text.Contains(number))
                    cell.Text += number;
            }
            
            if (cell.Text.Length <= 1)
            {
                cell.Font = new Font(SystemFonts.DefaultFont.FontFamily, 20);
                cell.ForeColor = SystemColors.ControlDarkDark;
            }
            else if (cell.Text.Length <= 6)
            {
                cell.Font = new Font(SystemFonts.DefaultFont.FontFamily, 14, FontStyle.Italic);
                cell.ForeColor = Color.DarkCyan;
            }
            else
            {
                cell.Font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Italic);
                cell.ForeColor = Color.DarkCyan;
            }
        }
        
        void startNewGame()
        {
            loadValues();

            var hintsCount = 0;

            // Assign the hints count based on the 
            // level player chosen
            if (beginnerLevel.Checked)
                hintsCount = 55;
            else if (IntermediateLevel.Checked)
                hintsCount = 43;
            else if (AdvancedLevel.Checked)
                hintsCount = 20;

            showRandomValuesHints(hintsCount);

            checkButton.Enabled = true;
        }

        void showRandomValuesHints(int hintsCount)
        {
            // Show value in radom cells
            // The hints count is based on the level player choose
            for (int i = 0; i < hintsCount; i++)
            {
                var rX = Random.Next(9);
                var rY = Random.Next(9);

                // Style the hint cells differently and
                // lock the cell so that player can't edit the value
                cells[rX, rY].Text = cells[rX, rY].Value.ToString();
                cells[rX, rY].ForeColor = Color.Black;
                cells[rX, rY].IsLocked = true;
            }
        }

        void loadValues()
        {
            // Clear the values in each cells
            foreach (var cell in cells)
            {
                cell.Value = 0;
                cell.Clear();
                cell.Font = new Font(SystemFonts.DefaultFont.FontFamily, 20);
                cell.ForeColor = SystemColors.ControlDarkDark;
            }

            // This method will be called recursively 
            // until it finds suitable values for each cells
            findValueForNextCell(0, -1);
        }

        bool findValueForNextCell(int i, int j)
        {
            // Increment the i and j values to move to the next cell
            // and if the columsn ends move to the next row
            if (++j > 8)
            {
                j = 0;

                // Exit if the line ends
                if (++i > 8)
                    return true;
            }

            int value;
            var numsLeft = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Find a random and valid number for the cell and go to the next cell 
            // and check if it can be allocated with another random and valid number
            do
            {
                // If there is not numbers left in the list to try next, 
                // return to the previous cell and allocate it with a different number
                if (numsLeft.Count < 1)
                {
                    cells[i, j].Value = 0;
                    return false;
                }

                // Take a random number from the numbers left in the list
                value = numsLeft[Random.Next(0, numsLeft.Count)];
                cells[i, j].Value = value;

                // Remove the allocated value from the list
                numsLeft.Remove(value);
            }
            while (!isValidNumber(value, i, j) || !findValueForNextCell(i, j));
            
            return true;
        }

        bool isValidNumber(int value, int x, int y)
        {
            for (int i = 0; i < 9; i++)
            {
                // Check all the cells in vertical direction
                if (i != y && cells[x, i].Value == value)
                    return false;

                // Check all the cells in horizontal direction
                if (i != x && cells[i, y].Value == value)
                    return false;
            }
            
            // Check all the cells in the specific block
            for (int i = x - (x % 3); i < x - (x % 3) + 3; i++)
            {
                for (int j = y - (y % 3); j < y - (y % 3) + 3; j++)
                {
                    if (i != x && j != y && cells[i, j].Value == value)
                        return false;
                }
            }

            return true;
        }
        
        void checkButton_Click(object sender, EventArgs e)
        {
            bool hasError = false;
            bool isFilled = true;

            foreach (var cell in cells)
            {
                if (cell.Value == 0)
                {
                    isFilled = false;
                    break;
                }

                if (!string.Equals(cell.Value.ToString(), cell.Text))
                {
                    hasError = true;
                }
            }

            if (!isFilled)
            {
                MessageBox.Show("Not all fields are filled yet", "Result");
            }

            if (hasError)
            {
                if (deathLinkService != null && DeathLinkCheckBox.Checked)
                {
                    var deathLink = new DeathLink(session.Players.GetPlayerAlias(session.ConnectionInfo.Slot), "Failed to solve a Sudoku");
                    deathLinkService.SendDeathLink(deathLink);
                }

                MessageBox.Show("Wrong inputs", "Result");
            }
            else
            {
                if (session != null && session.Socket.Connected)
                {
                    checkButton.Enabled = false;

                    var missing = session.Locations.AllMissingLocations;

                    if (missing.Any())
                    {
                        var locationId = missing[Random.Next(0, missing.Count)];
                        session.Locations.ScoutLocationsAsync(true, locationId);

                        MessageBox.Show("Correct, unlocked 1 hint", "Result");
                    }
                    else
                    {
                        MessageBox.Show("Correct, no missing locations left to hint for", "Result");
                    }
                }

            }
        }

        void clearButton_Click(object sender, EventArgs e)
        {
            foreach (var cell in cells)
                if (cell.IsLocked == false)
                    cell.Clear();
        }

        void newGameButton_Click(object sender, EventArgs e)
        {
            startNewGame();
        }

        void ConnectButton_Click(object sender, EventArgs e)
        {
            if (session != null)
            {
                session = null;
                deathLinkService = null;
                ConnectButton.Text = "Connect";
                UserText.Enabled = true;
                ServerText.Enabled = true;
                APLog.Clear();
            }

            var serverUri = ServerText.Text;

            if (!serverUri.Contains(':'))
                serverUri += ":38281";
            if (serverUri.EndsWith(':'))
                serverUri += "38281";
            if (!serverUri.StartsWith("ws://"))
                serverUri = "ws://" + serverUri;

            try
            {
                session = ArchipelagoSessionFactory.CreateSession(new Uri(serverUri));
                session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
                var result = session.TryConnectAndLogin("", UserText.Text, ItemsHandlingFlags.NoItems,
                    tags: new[] { "BK_Sudoku", "TextOnly" });

                if (!result.Successful)
                {
                    MessageBox.Show(string.Join(',', ((LoginFailure)result).Errors), "Login Failed");
                }
                else
                {
                    ConnectButton.Text = "Disconnect";
                    UserText.Enabled = false;
                    ServerText.Enabled = false;

                    deathLinkService = session.CreateDeathLinkService();
                    deathLinkService.OnDeathLinkReceived += (deathLink) =>
                    {
                        startNewGame();
                        MessageBox.Show($"DeathLink recieved from: {deathLink.Source}, reason: {deathLink.Cause}", "DeathLink");
                    };
                    
                    DeathLinkCheckBox_CheckedChanged(sender, e);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        void MessageLog_OnMessageReceived(LogMessage message)
        {
            if (message is not HintItemSendLogMessage hintMessage || hintMessage.SendingPlayerSlot != session.ConnectionInfo.Slot)
                return;

            Invoke(() =>
            {
                foreach (var part in hintMessage.Parts)
                    AddToLog(part.Text, part.Color);

                APLog.Text += Environment.NewLine;
            });
        }

        public void AddToLog(string text, Color color)
        {
            APLog.SelectionStart = APLog.TextLength;
            APLog.SelectionLength = 0;

            APLog.SelectionColor = color;
            APLog.AppendText(text);
            APLog.SelectionColor = APLog.ForeColor;
        }

        void DeathLinkCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (session == null || deathLinkService == null)
                return;

            if (DeathLinkCheckBox.Checked)
                deathLinkService.EnableDeathLink();
            else
                deathLinkService.DisableDeathLink();
        }
    }
}
