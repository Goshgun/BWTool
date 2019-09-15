﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBitcoin;

namespace BWTool
{
    public partial class BWForm : Form
    {
        BrainwalletMiner miner;
        RandomStringMiner StringMiner;
        Stopwatch stopWatch = new Stopwatch();
        public BWForm()
        {
            InitializeComponent();
        }


        private void PasswordLoadButton_Click(object sender, EventArgs e)
        {
            if (PasswordListOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                ProgressRichTextBox.AppendText($"Selected password file: {PasswordListOpenFileDialog.FileName}{Environment.NewLine}");
            }
        }

        private void SeparatorTextBox_TextChanged(object sender, EventArgs e)
        {
            if (SeparatorTextBox.Text.Length > 0 && SeparatorTextBox.Text.Length < 2)
            {
                SeparatorEnterCheckBox.Checked = false;
                

            }
            else
            {
                SeparatorEnterCheckBox.Checked = true;
                SeparatorTextBox.Clear();
            }
        }

        private void AddressLookupLoadButton_Click(object sender, EventArgs e)
        {
            if (AddressOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                ProgressRichTextBox.AppendText($"Selected lookup address list: {AddressOpenFileDialog.FileName}{Environment.NewLine}");
            }
        }
        private bool InputisValid()
        {
            if (AddressOpenFileDialog.FileName == "" && PasswordListOpenFileDialog.FileName == "" && !UseRandomCheckBox.Checked)
            {
                MessageBox.Show("Please select address file and password file first!");

                return false;

            }
            else if (AddressOpenFileDialog.FileName == "" && UseRandomCheckBox.Checked)
            {
                MessageBox.Show("Please select address file first!");
                return false;
            }
            else
            {
                return true;
            }

        }

        private void MiningStartButton_Click(object sender, EventArgs e)
        {
            if (UseRandomCheckBox.Checked)
            {
                if (InputisValid())
                {
                    if (MiningStartButton.Text == "Start Mining")
                    {
                        MiningStartButton.Text = "Stop Mining";
                        if (CharSetTextBox.Text.Length > 0)
                        {
                            StringMiner = new RandomStringMiner((int)CountOfCharsUpDown.Value, AddressOpenFileDialog.FileName, CompressedCheckBox.Checked, (int)Sha256NumericUpDown.Value, CharSetTextBox.Text);
                        }
                        else
                        {
                            StringMiner = new RandomStringMiner((int)CountOfCharsUpDown.Value, AddressOpenFileDialog.FileName, CompressedCheckBox.Checked, (int)Sha256NumericUpDown.Value);
                        }
                        StringMiner.StartRandomStringMiner();
                        MinerInfoUpdateTimer.Start();
                        stopWatch.Start();


                    }
                    else
                    {
                        stopMiner();
                    }
                }

            }
            else
            {
                if (InputisValid())
                {
                    char separator = '\0';
                    if (SeparatorEnterCheckBox.Checked)
                    {
                        separator = '\r';
                    }
                    else
                    {
                        separator = Convert.ToChar(SeparatorTextBox.Text);
                    }
                    if (MiningStartButton.Text == "Start Mining")
                    {
                        MiningStartButton.Text = "Stop Mining";

                        miner = new BrainwalletMiner(PasswordListOpenFileDialog.FileName, AddressOpenFileDialog.FileName, CompressedCheckBox.Checked, separator, (int)Sha256NumericUpDown.Value);
                        miner.Start();
                        MinerInfoUpdateTimer.Start();
                        stopWatch.Start();

                    }
                    else
                    {
                        stopMiner();
                    }
                }
            }
           


        }

        void stopMiner()
        {
            MiningStartButton.Text = "Start Mining";
            miner?.Stop();
            StringMiner?.StopRandomStringMiner();
            MinerInfoUpdateTimer.Stop();
            MiningProgressBar.Value = 0;
            ProgressLabel.Text = "";
            stopWatch.Reset();
            //If the job was done by miner thread minerStillRunning bool should be false this will be skipped
            //If the cancellation was requested by user this should be true
            while (MinerInfo.minerStillRunning)
            {
                //Cancellation was already requested in miner thread, waiting a few milliseconds to completing
                Thread.Sleep(10);
            }
            ProgressRichTextBox.AppendText(MinerInfo.minerThreadResults);
            MinerInfo.defaultingValues();
        }
        private void Uiupdate(object sender, EventArgs e)
        {
            double timeLeft;
            try
            {
                timeLeft = ((double)stopWatch.Elapsed.TotalSeconds / MinerInfo.currentlyProcessed) * (MinerInfo.lengthOfJob - MinerInfo.currentlyProcessed);
            }
            catch (DivideByZeroException)
            {

                timeLeft = double.NaN;
            }

            ProgressLabel.Text = $"Tried keys: {MinerInfo.countOfTriedKeys},elapsed seconds: {stopWatch.Elapsed.TotalSeconds.ToString("N1")}, average keys perc second: {(MinerInfo.countOfTriedKeys / stopWatch.Elapsed.TotalSeconds).ToString("N2")}, estimated time left: {Math.Round(timeLeft)} seconds, miner thread info: {MinerInfo.minerThreadInfo}";
            double progressValue = 100 / (MinerInfo.lengthOfJob / (double)MinerInfo.currentlyProcessed);
            int percentValue = (int)Math.Round(progressValue);
            MiningProgressBar.Value = percentValue;
            //Job was done by the thread already
            if (!MinerInfo.minerStillRunning)
            {
                stopMiner();


            }
        }
        private void BWForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MinerInfo.minerStillRunning)
            {
                stopMiner();
            }

        }

    }
}
