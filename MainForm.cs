using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.CoreAudioApi;

namespace VolumeLocker;

public partial class MainForm : Form
{
    private const float VolumeChangeThreshold = 0.01f;
    private const int IconSize = 16;
    private const int IconPadding = 2;
    private const int IconCircleSize = 12;

    private NotifyIcon? trayIcon;
    private ContextMenuStrip? trayMenu;
    private ToolStripMenuItem? startupMenuItem;
    private VolumeControlForm? volumeForm;
    private MMDevice? audioDevice;
    private AudioEndpointVolume? audioEndpointVolume;
    private bool isLocked;
    private float lockedVolume = 0.5f;
    private bool isRestoringVolume;
    private ToolStripMenuItem? startLockedMenuItem;
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegistryValueName = "VolumeLocker";
    private const string SettingsRegistryKey = @"Software\VolumeLocker";
    private const string StartLockedValueName = "StartLocked";

    public MainForm()
    {
        InitializeComponent();
        InitializeAudio();
        InitializeTrayIcon();
        ApplyStartLockedPreference();
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(0, 0);
        FormBorderStyle = FormBorderStyle.None;
        Name = "MainForm";
        Text = "Volume Locker";
        ResumeLayout(false);
    }

    private void InitializeAudio()
    {
        try
        {
            using var deviceEnumerator = new MMDeviceEnumerator();
            audioDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            audioEndpointVolume = audioDevice?.AudioEndpointVolume;
            
            if (audioEndpointVolume == null)
            {
                MessageBox.Show(
                    "Unable to access audio device. The application may not function correctly.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            lockedVolume = audioEndpointVolume.MasterVolumeLevelScalar;
            audioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error initializing audio: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void InitializeTrayIcon()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Show Volume Control", null, ShowVolumeControl);
        var lockMenuItem = new ToolStripMenuItem("Lock Volume", null, ToggleLock)
        {
            Tag = "lock"
        };
        trayMenu.Items.Add(lockMenuItem);
        trayMenu.Items.Add("-");
        startupMenuItem = new ToolStripMenuItem("Start on Login", null, ToggleStartup)
        {
            Checked = IsStartupEnabled()
        };
        trayMenu.Items.Add(startupMenuItem);
        startLockedMenuItem = new ToolStripMenuItem("Start Locked", null, ToggleStartLocked)
        {
            Checked = GetStartLockedPreference()
        };
        trayMenu.Items.Add(startLockedMenuItem);
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("About", null, ShowAbout);
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, Exit);

        Icon? icon = null;
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        
        // Try to find the icon resource by checking all manifest resources
        string[] possibleResourceNames = 
        {
            "VolumeLocker.app.ico",
            "app.ico"
        };
        
        // First, try the known resource names
        foreach (var resourceName in possibleResourceNames)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    icon = new Icon(stream);
                    break;
                }
            }
        }
        
        // If not found, search all manifest resources for .ico files
        if (icon == null)
        {
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (var resourceName in resourceNames)
            {
                if (resourceName.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            icon = new Icon(stream);
                            break;
                        }
                    }
                }
            }
        }
        
        // Try loading from the executable directory (for single-file mode fallback)
        if (icon == null)
        {
            var exePath = Application.ExecutablePath;
            var exeDir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(exeDir))
            {
                var iconPath = Path.Combine(exeDir, "app.ico");
                if (File.Exists(iconPath))
                {
                    icon = new Icon(iconPath);
                }
            }
        }
        
        // Try current directory as last resort
        if (icon == null && File.Exists("app.ico"))
        {
            icon = new Icon("app.ico");
        }

        if (icon == null)
        {
            using (var bitmap = new Bitmap(IconSize, IconSize))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.FillEllipse(Brushes.Blue, IconPadding, IconPadding, IconCircleSize, IconCircleSize);
                }
                
                IntPtr hIcon = bitmap.GetHicon();
                icon = Icon.FromHandle(hIcon);
            }
        }

        // Set the form icon as well (helps with taskbar in some scenarios)
        if (icon != null)
        {
            Icon = icon;
        }

        trayIcon = new NotifyIcon
        {
            Icon = icon,
            ContextMenuStrip = trayMenu,
            Text = "Volume Locker",
            Visible = true
        };

        trayIcon.DoubleClick += (sender, e) => ShowVolumeControl(sender, e);
    }

    private void ShowVolumeControl(object? sender, EventArgs e)
    {
        if (volumeForm == null || volumeForm.IsDisposed)
        {
            volumeForm = new VolumeControlForm(audioDevice, lockedVolume, isLocked);
            volumeForm.VolumeChanged += OnVolumeChanged;
            volumeForm.LockToggled += OnLockToggled;
        }

        volumeForm.Show();
        volumeForm.BringToFront();
        volumeForm.WindowState = FormWindowState.Normal;
    }

    private void ShowAbout(object? sender, EventArgs e)
    {
        using var aboutForm = new AboutForm();
        aboutForm.ShowDialog();
    }

    private bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            if (key == null)
                return false;

            var value = key.GetValue(StartupRegistryValueName);
            if (value == null)
                return false;

            var exePath = value.ToString();
            if (string.IsNullOrEmpty(exePath))
                return false;

            var currentExePath = Application.ExecutablePath;
            return exePath.Equals(currentExePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null)
                return;

            if (enabled)
            {
                key.SetValue(StartupRegistryValueName, Application.ExecutablePath);
            }
            else
            {
                key.DeleteValue(StartupRegistryValueName, false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to {(enabled ? "enable" : "disable")} startup: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ToggleStartup(object? sender, EventArgs e)
    {
        bool currentState = IsStartupEnabled();
        bool newState = !currentState;
        SetStartupEnabled(newState);
        
        if (startupMenuItem != null)
        {
            startupMenuItem.Checked = IsStartupEnabled();
        }
    }

    private bool GetStartLockedPreference()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsRegistryKey, false);
            if (key == null)
                return false;

            var value = key.GetValue(StartLockedValueName);
            if (value == null)
                return false;

            return Convert.ToBoolean(value);
        }
        catch
        {
            return false;
        }
    }

    private void SetStartLockedPreference(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsRegistryKey, true);
            if (key == null)
                return;

            key.SetValue(StartLockedValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save preference: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ApplyStartLockedPreference()
    {
        bool shouldStartLocked = GetStartLockedPreference();
        if (shouldStartLocked && audioEndpointVolume != null)
        {
            isLocked = true;
            lockedVolume = audioEndpointVolume.MasterVolumeLevelScalar;
            UpdateLockMenuItem();
        }
    }

    private void ToggleStartLocked(object? sender, EventArgs e)
    {
        bool currentState = GetStartLockedPreference();
        bool newState = !currentState;
        SetStartLockedPreference(newState);
        
        if (startLockedMenuItem != null)
        {
            startLockedMenuItem.Checked = newState;
        }
    }

    private void OnVolumeChanged(float volume)
    {
        lockedVolume = volume;
        if (!isLocked && audioEndpointVolume != null)
        {
            isRestoringVolume = true;
            try
            {
                audioEndpointVolume.MasterVolumeLevelScalar = volume;
            }
            finally
            {
                isRestoringVolume = false;
            }
        }
    }

    private void OnLockToggled(bool locked)
    {
        isLocked = locked;
        UpdateLockMenuItem();
        
        if (volumeForm is { IsDisposed: false })
        {
            volumeForm.SetLockState(locked);
        }
        
        if (isLocked && audioEndpointVolume != null)
        {
            lockedVolume = audioEndpointVolume.MasterVolumeLevelScalar;
            volumeForm?.SetVolume(lockedVolume);
        }
    }

    private void ToggleLock(object? sender, EventArgs e)
    {
        isLocked = !isLocked;
        OnLockToggled(isLocked);
    }

    private void UpdateLockMenuItem()
    {
        if (trayMenu?.Items.Count > 1 && trayMenu.Items[1] is ToolStripMenuItem menuItem)
        {
            menuItem.Text = isLocked ? "Lock Volume" : "Lock Volume";
            menuItem.Checked = isLocked;
        }
    }

    private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
    {
        if (!isLocked || isRestoringVolume)
        {
            return;
        }

        float currentVolume = data.MasterVolume;
        
        if (Math.Abs(currentVolume - lockedVolume) > VolumeChangeThreshold)
        {
            isRestoringVolume = true;
            
            if (InvokeRequired)
            {
                Invoke(RestoreVolume);
            }
            else
            {
                RestoreVolume();
            }
        }
    }

    private void RestoreVolume()
    {
        try
        {
            if (audioEndpointVolume != null)
            {
                audioEndpointVolume.MasterVolumeLevelScalar = lockedVolume;
                if (volumeForm is { IsDisposed: false })
                {
                    volumeForm.SetVolume(lockedVolume);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring volume: {ex.Message}");
        }
        finally
        {
            isRestoringVolume = false;
        }
    }

    private void Exit(object? sender, EventArgs e)
    {
        if (audioEndpointVolume != null)
        {
            audioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
        }
        trayIcon?.Dispose();
        volumeForm?.Close();
        Application.Exit();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (audioEndpointVolume != null)
            {
                audioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
            }
            audioDevice?.Dispose();
            trayIcon?.Dispose();
            volumeForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}

