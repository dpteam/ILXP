using System;
using System.Drawing;
using System.Windows.Forms;

namespace ILXP
{
    /// <summary>About box: product name, version, warning and copyright.</summary>
    public sealed class AboutForm : Form
    {
        private Button closeButton;
        private PictureBox logoBox;
        private Label infoLabel;
        private Label productLabel;
        private FlowLayoutPanel copyrightPanel;
        private Label copyrightLabel;

        public AboutForm()
        {
            InitializeComponent();

            string product = AppInfo.ProductName;
            string version = AppInfo.VersionText;
            string copyright = AppInfo.Copyright;

            productLabel.Text = productLabel.Text
                .Replace("<ProductName>", product)
                .Replace("<Version>", version)
                .Replace("<CopyRight>", copyright);
            copyrightLabel.Text = copyrightLabel.Text
                .Replace("<ProductName>", product)
                .Replace("<Version>", version)
                .Replace("<CopyRight>", copyright);
            productLabel.Font = new Font(productLabel.Font, FontStyle.Bold);
        }

        private void InitializeComponent()
        {
            closeButton = new Button();
            infoLabel = new Label();
            productLabel = new Label();
            logoBox = new PictureBox();
            copyrightPanel = new FlowLayoutPanel();
            copyrightLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)logoBox).BeginInit();
            copyrightPanel.SuspendLayout();
            SuspendLayout();

            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            closeButton.DialogResult = DialogResult.Cancel;
            closeButton.Location = new Point(375, 270);
            closeButton.Name = "Close";
            closeButton.Size = new Size(75, 23);
            closeButton.TabIndex = 0;
            closeButton.Text = "Close";
            closeButton.UseVisualStyleBackColor = true;

            infoLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            infoLabel.AutoSize = true;
            infoLabel.Location = new Point(9, 212);
            infoLabel.Name = "LabelInfo";
            infoLabel.Size = new Size(427, 52);
            infoLabel.TabIndex = 2;
            infoLabel.Text = "Warning: This computer program is protected by copyright law and international treaties.";

            productLabel.AutoSize = true;
            productLabel.Location = new Point(3, 0);
            productLabel.Name = "Label1";
            productLabel.Size = new Size(140, 13);
            productLabel.TabIndex = 3;
            productLabel.Text = "<ProductName> (<Version>)";

            logoBox.BackColor = Color.White;
            logoBox.Dock = DockStyle.Top;
            logoBox.Image = AppResources.CompanyLogo;
            logoBox.Location = new Point(0, 0);
            logoBox.Margin = new Padding(0);
            logoBox.Name = "PictureBox1";
            logoBox.Size = new Size(459, 128);
            logoBox.TabIndex = 1;
            logoBox.TabStop = false;

            copyrightPanel.Controls.Add(productLabel);
            copyrightPanel.Controls.Add(copyrightLabel);
            copyrightPanel.FlowDirection = FlowDirection.TopDown;
            copyrightPanel.Location = new Point(8, 137);
            copyrightPanel.Name = "FlowLayoutPanel1";
            copyrightPanel.Size = new Size(439, 67);
            copyrightPanel.TabIndex = 4;

            copyrightLabel.AutoSize = true;
            copyrightLabel.Location = new Point(3, 13);
            copyrightLabel.Name = "LabelCopyright";
            copyrightLabel.Size = new Size(68, 13);
            copyrightLabel.TabIndex = 4;
            copyrightLabel.Text = "<CopyRight>";

            AcceptButton = closeButton;
            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            CancelButton = closeButton;
            ClientSize = new Size(459, 302);
            Controls.Add(copyrightPanel);
            Controls.Add(infoLabel);
            Controls.Add(logoBox);
            Controls.Add(closeButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About";
            ((System.ComponentModel.ISupportInitialize)logoBox).EndInit();
            copyrightPanel.ResumeLayout(false);
            copyrightPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
