﻿using System;
using System.Windows.Forms;

namespace ShareInvest.BackTesting.SettingsScreen
{
    public partial class Progress : UserControl
    {
        public Progress()
        {
            InitializeComponent();
        }
        public int ProgressBarValue
        {
            get; set;
        }
        public int Maximum
        {
            get; set;
        }
        public int Rate(int max)
        {
            progressBar.Maximum = max;
            timer.Interval = 15;
            timer.Start();

            return max / 155;
        }
        public void Retry()
        {
            ProgressBarValue = 0;
            timer.Interval = 15;
            timer.Start();
        }
        private void TimerTick(object sender, EventArgs e)
        {
            if (Maximum > 0 && Swap == false)
            {
                Swap = true;
                progressBar.Maximum = Maximum;
                ProgressBarValue = 0;
            }
            progressBar.Value = ProgressBarValue;
            Application.DoEvents();
        }
        private bool Swap
        {
            get; set;
        }
    }
}