using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private const string AppName = "Restricted Site Control";
    private const string Version = "1.0.0";

    private static readonly string InstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RestrictedSiteControl");

    private static readonly string InstallCore = Path.Combine(InstallRoot, "Core");
    private static readonly string InstallPolicy = Path.Combine(InstallCore, "Policy");
    private static readonly string ScriptPath = Path.Combine(InstallPolicy, "Block-RestrictedSites.ps1");
    private static readonly string MarkerPath = Path.Combine(InstallPolicy, "installed.flag");

    private const string RegistryPath = @"SOFTWARE\RestrictedSiteControl";

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (IsInstalled())
            ShowInstalledMenu();
        else
            Install();
    }

    private static bool IsInstalled()
    {
        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryPath))
            {
                if (key != null && File.Exists(ScriptPath))
                    return true;
            }
        }
        catch { }

        return File.Exists(ScriptPath) && File.Exists(MarkerPath);
    }

    private static void ShowInstalledMenu()
    {
        using (Form form = CreateForm("Restricted Site Control"))
        {
            Label title = new Label
            {
                Text = AppName,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, 25)
            };

            Label info = new Label
            {
                Text = "This program is already installed.\r\n\r\nChoose an action:",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(32, 70)
            };

            Button update = new Button
            {
                Text = "Update",
                Width = 110,
                Height = 38,
                Location = new Point(30, 140)
            };

            Button uninstall = new Button
            {
                Text = "Uninstall",
                Width = 110,
                Height = 38,
                Location = new Point(150, 140)
            };

            Button cancel = new Button
            {
                Text = "Cancel",
                Width = 110,
                Height = 38,
                Location = new Point(270, 140)
            };

            update.Click += (s, e) =>
            {
                form.Hide();
                UpdateInstallation();
                form.Close();
            };

            uninstall.Click += (s, e) =>
            {
                form.Hide();
                Uninstall();
                form.Close();
            };

            cancel.Click += (s, e) => form.Close();

            form.Controls.Add(title);
            form.Controls.Add(info);
            form.Controls.Add(update);
            form.Controls.Add(uninstall);
            form.Controls.Add(cancel);

            Application.Run(form);
        }
    }

    private static void Install()
    {
        DialogResult confirm = MessageBox.Show(
            "Restricted Site Control is not installed.\r\n\r\n" +
            "The installer will configure the computer to block the restricted sites.\r\n\r\n" +
            "Continue?",
            AppName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            Directory.CreateDirectory(InstallRoot);
            Directory.CreateDirectory(InstallCore);
            Directory.CreateDirectory(InstallPolicy);

            HideDirectory(InstallRoot);
            HideDirectory(InstallCore);
            HideDirectory(InstallPolicy);

            WriteEmbeddedScript();

            File.WriteAllText(
                MarkerPath,
                "Restricted Site Control\r\nVersion=" + Version + "\r\n");

            HideFile(MarkerPath);

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPath))
            {
                key.SetValue("Version", Version, RegistryValueKind.String);
                key.SetValue("InstallPath", InstallPolicy, RegistryValueKind.String);
            }

            RunPowerShell("Block");

            MessageBox.Show(
                "Restricted Site Control was installed successfully.\r\n\r\n" +
                "The restricted-site protection is now active.",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Installation failed.\r\n\r\n" + ex.Message,
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void UpdateInstallation()
    {
        DialogResult confirm = MessageBox.Show(
            "Update Restricted Site Control to version " + Version + "?\r\n\r\n" +
            "The current installation will be replaced while keeping the existing HOSTS backup.",
            AppName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            Directory.CreateDirectory(InstallRoot);
            Directory.CreateDirectory(InstallCore);
            Directory.CreateDirectory(InstallPolicy);

            WriteEmbeddedScript();

            if (File.Exists(MarkerPath))
                File.SetAttributes(MarkerPath, File.GetAttributes(MarkerPath) & ~FileAttributes.Hidden);
            File.WriteAllText(
                MarkerPath,
                "Restricted Site Control\r\nVersion=" + Version + "\r\n");

            HideDirectory(InstallRoot);
            HideDirectory(InstallCore);
            HideDirectory(InstallPolicy);
            HideFile(MarkerPath);

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPath))
            {
                key.SetValue("Version", Version, RegistryValueKind.String);
                key.SetValue("InstallPath", InstallPolicy, RegistryValueKind.String);
            }

            RunPowerShell("Block");

            MessageBox.Show(
                "Restricted Site Control was updated successfully.",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Update failed.\r\n\r\n" + ex.Message,
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void Uninstall()
    {
        DialogResult confirm = MessageBox.Show(
            "Uninstall Restricted Site Control?\r\n\r\n" +
            "The program will restore the original HOSTS file and remove its installed files.",
            AppName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            if (File.Exists(ScriptPath))
                RunPowerShell("Unblock");

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryPath))
            {
                if (key != null)
                    Registry.LocalMachine.DeleteSubKeyTree(RegistryPath, false);
            }

            RemoveHiddenAttribute(MarkerPath);
            RemoveHiddenAttribute(InstallPolicy);
            RemoveHiddenAttribute(InstallCore);
            RemoveHiddenAttribute(InstallRoot);

            if (Directory.Exists(InstallRoot))
                Directory.Delete(InstallRoot, true);

            MessageBox.Show(
                "Restricted Site Control was uninstalled successfully.\r\n\r\n" +
                "The original HOSTS file has been restored.",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Uninstallation failed.\r\n\r\n" + ex.Message,
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void WriteEmbeddedScript()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using (Stream stream = assembly.GetManifestResourceStream("Block-RestrictedSites.ps1"))
        {
            if (stream == null)
                throw new InvalidOperationException("The embedded PowerShell script could not be found.");

            using (FileStream output = new FileStream(ScriptPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(output);
            }
        }
    }

    private static void RunPowerShell(string action)
    {
        if (!File.Exists(ScriptPath))
            throw new FileNotFoundException("Installed script was not found.", ScriptPath);

        string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string powershell = Path.Combine(systemRoot, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");

        if (!File.Exists(powershell))
            throw new FileNotFoundException("Windows PowerShell was not found.", powershell);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = powershell,
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" +
                        ScriptPath + "\" -Action " + action,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = InstallPolicy
        };

        using (Process process = Process.Start(psi))
        {
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    "The protection script returned exit code " + process.ExitCode + ".");
        }
    }

    private static void HideDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            File.SetAttributes(
                path,
                File.GetAttributes(path) | FileAttributes.Hidden);
        }
    }

    private static void HideFile(string path)
    {
        if (File.Exists(path))
        {
            File.SetAttributes(
                path,
                File.GetAttributes(path) | FileAttributes.Hidden);
        }
    }

    private static void RemoveHiddenAttribute(string path)
    {
        if (File.Exists(path))
        {
            File.SetAttributes(
                path,
                File.GetAttributes(path) & ~FileAttributes.Hidden);
        }
        else if (Directory.Exists(path))
        {
            File.SetAttributes(
                path,
                File.GetAttributes(path) & ~FileAttributes.Hidden);
        }
    }

    private static Form CreateForm(string title)
    {
        Form form = new Form
        {
            Text = title,
            Width = 410,
            Height = 235,
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = true
        };

        try
        {
            using (Stream iconStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("RestritedSiteControl.ico"))
            {
                if (iconStream != null)
                    form.Icon = new Icon(iconStream);
            }
        }
        catch { }

        return form;
    }
}
