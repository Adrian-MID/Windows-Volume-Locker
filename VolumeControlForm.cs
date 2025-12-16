using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace VolumeLocker;

public partial class VolumeControlForm : Form
{
    private const int FormWidth = 320;
    private const int FormHeight = 180;
    private const int ControlPadding = 20;
    private const int TrackBarWidth = 260;
    private const int TrackBarHeight = 45;
    private const int VolumeLabelTop = 20;
    private const int TrackBarTop = 45;
    private const int ValueLabelTop = 75;
    private const int CheckBoxTop = 110;
    private const int MaxVolumePercent = 100;
    private const int TickFrequency = 10;
    private const float FontSize = 9F;
    private const string FontFamily = "Segoe UI";

    private TrackBar volumeTrackBar = null!;
    private Label volumeValueLabel = null!;
    private CheckBox lockCheckBox = null!;
    private MMDevice? audioDevice;
    private bool updatingVolume;

    public event Action<float>? VolumeChanged;
    public event Action<bool>? LockToggled;

    public VolumeControlForm(MMDevice? device, float initialVolume, bool initialLockState)
    {
        audioDevice = device;
        InitializeComponent();
        
        if (volumeTrackBar != null)
        {
            volumeTrackBar.Value = (int)(initialVolume * MaxVolumePercent);
        }
        
        if (lockCheckBox != null)
        {
            lockCheckBox.Checked = initialLockState;
            UpdateSliderLockState(initialLockState);
        }
        
        UpdateVolumeLabel();
    }

    private void InitializeComponent()
    {
        volumeTrackBar = new TrackBar();
        volumeValueLabel = new Label();
        lockCheckBox = new CheckBox();
        SuspendLayout();

        Text = "Volume Locker";
        Size = new Size(FormWidth, FormHeight);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        var volumeLabel = new Label
        {
            AutoSize = true,
            Location = new Point(ControlPadding, VolumeLabelTop),
            Text = "Volume Level:",
            Font = new Font(FontFamily, FontSize)
        };

        volumeTrackBar.Location = new Point(ControlPadding, TrackBarTop);
        volumeTrackBar.Size = new Size(TrackBarWidth, TrackBarHeight);
        volumeTrackBar.Minimum = 0;
        volumeTrackBar.Maximum = MaxVolumePercent;
        volumeTrackBar.TickFrequency = TickFrequency;
        volumeTrackBar.ValueChanged += VolumeTrackBar_ValueChanged;

        volumeValueLabel.AutoSize = true;
        volumeValueLabel.Location = new Point(ControlPadding, ValueLabelTop);
        volumeValueLabel.Text = "0%";
        volumeValueLabel.Font = new Font(FontFamily, FontSize);

        lockCheckBox.AutoSize = true;
        lockCheckBox.Location = new Point(ControlPadding, CheckBoxTop);
        lockCheckBox.Text = "Lock Volume";
        lockCheckBox.Font = new Font(FontFamily, FontSize);
        lockCheckBox.CheckedChanged += LockCheckBox_CheckedChanged;

        Controls.Add(volumeLabel);
        Controls.Add(volumeTrackBar);
        Controls.Add(volumeValueLabel);
        Controls.Add(lockCheckBox);

        ResumeLayout(false);
    }

    private void VolumeTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        if (updatingVolume)
        {
            return;
        }

        float volume = volumeTrackBar.Value / (float)MaxVolumePercent;
        UpdateVolumeLabel();
        
        if (!lockCheckBox.Checked && audioDevice?.AudioEndpointVolume != null)
        {
            try
            {
                audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting volume: {ex.Message}");
            }
        }

        VolumeChanged?.Invoke(volume);
    }

    private void UpdateVolumeLabel()
    {
        volumeValueLabel.Text = $"{volumeTrackBar.Value}%";
    }

    private void LockCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        bool isLocked = lockCheckBox.Checked;
        UpdateSliderLockState(isLocked);
        LockToggled?.Invoke(isLocked);
    }

    private void UpdateSliderLockState(bool isLocked)
    {
        if (volumeTrackBar != null)
        {
            volumeTrackBar.Enabled = !isLocked;
        }
    }

    public void SetVolume(float volume)
    {
        updatingVolume = true;
        volumeTrackBar.Value = (int)(volume * MaxVolumePercent);
        UpdateVolumeLabel();
        updatingVolume = false;
    }

    public void SetLockState(bool isLocked)
    {
        if (lockCheckBox != null)
        {
            lockCheckBox.Checked = isLocked;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }
}

