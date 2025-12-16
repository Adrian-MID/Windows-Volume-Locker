using System;
using System.Drawing;
using System.Windows.Forms;

namespace VolumeLocker;

public partial class AboutForm : Form
{
    private const int FormWidth = 400;
    private const int FormHeight = 290;
    private const int FormPadding = 20;
    private const string FontFamily = "Segoe UI";
    private const float TitleFontSize = 14F;
    private const float BodyFontSize = 9F;

    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "About Volume Locker";
        Size = new Size(FormWidth, FormHeight);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        var titleLabel = new Label
        {
            AutoSize = true,
            Location = new Point(FormPadding, FormPadding),
            Text = "Volume Locker",
            Font = new Font(FontFamily, TitleFontSize, FontStyle.Bold)
        };

        var descriptionLabel = new Label
        {
            AutoSize = false,
            Location = new Point(FormPadding, FormPadding + 35),
            Size = new Size(FormWidth - (FormPadding * 2), 50),
            Text = "A Windows application that allows you to lock your system volume at a specific level.",
            Font = new Font(FontFamily, BodyFontSize)
        };

        var attributionLabel = new Label
        {
            AutoSize = true,
            Location = new Point(FormPadding, FormPadding + 100),
            Text = "Icon designed by Freepik",
            Font = new Font(FontFamily, BodyFontSize),
            ForeColor = Color.Gray
        };

        var versionLabel = new Label
        {
            AutoSize = true,
            Location = new Point(FormPadding, FormPadding + 130),
            Text = "Built with .NET 8.0 and Windows Forms",
            Font = new Font(FontFamily, BodyFontSize),
            ForeColor = Color.Gray
        };

        var okButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(FormWidth - FormPadding - 75, FormHeight - FormPadding - 50),
            Size = new Size(75, 23),
            Text = "OK",
            UseVisualStyleBackColor = true
        };

        Controls.Add(titleLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(attributionLabel);
        Controls.Add(versionLabel);
        Controls.Add(okButton);

        AcceptButton = okButton;

        ResumeLayout(false);
    }
}

