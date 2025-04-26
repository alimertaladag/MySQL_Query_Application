﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace MySql_Sorgu
{
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label textLabel = new Label()
            {
                Left = 20,
                Top = 20,
                Width = 260,
                Text = text
            };

            TextBox textBox = new TextBox()
            {
                Left = 20,
                Top = 50,
                Width = 250
            };

            Button confirmation = new Button()
            {
                Text = "Tamam",
                Left = 190,
                Width = 80,
                Top = 80,
                DialogResult = DialogResult.OK
            };

            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}