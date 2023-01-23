using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Precomp_GUI
{
    /// <summary>
    /// Interaction logic for DecompressWindow.xaml
    /// </summary>
    public partial class DecompressWindow : Window
    {
        public string PrecompDirectory { get { return PrecompDirTextBox.Text ?? ""; } }
        public string OutputPath { get { return OutputTextBox.Text ?? ""; } }
        public string SourceFile { get { return InputTextBox.Text ?? ""; } }

        public System.Diagnostics.Process? CurrentProcess = null;

        public static async Task ReadTextReaderAsync(TextReader reader, IProgress<string> progress)
        {
            char[] buffer = new char[1024];
            for (; ; )
            {
                int count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (count == 0) break;
                progress.Report(new string(buffer, 0, count));
            }
        }

        public DecompressWindow(string? sourceFile, string? outputFile)
        {
            InitializeComponent();
            if (sourceFile != null) InputTextBox.Text = sourceFile;
            if (outputFile != null) OutputTextBox.Text = outputFile;
        }

        public void Decompress()
        {
            Action<string> onError = x =>
            {
                MessageBox.Show($"Error while decompressing! {x}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SettingsPanel.Visibility = Visibility.Visible;
                ProgressPanel.Visibility = Visibility.Collapsed;
                ProgressBar.Value = 0;
                CurrentProcess?.Kill(true);
            };
            Progress<string> onStdErr = new(onError);

            if (!File.Exists(@$"{PrecompDirectory}/precomp.exe")) 
            {
                MessageBox.Show($"Cannot find precomp.exe.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string precompGUIExeDirectory = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName;
            // Create a temp folder
            Directory.CreateDirectory($"{precompGUIExeDirectory}/temp");

            // Copy the precomp.exe into the temp folder
            File.Copy(@$"{PrecompDirectory}/precomp.exe", $"{precompGUIExeDirectory}/temp/precomp.exe", true);

            ProgressBar.Value = 0;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            CurrentProcess = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new();
            startInfo.WorkingDirectory = precompGUIExeDirectory + "/temp";
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.Arguments = $"/C precomp.exe -r -o\"{OutputPath}\" \"{SourceFile}\"";
            startInfo.Verb = "runas";
            CurrentProcess.StartInfo = startInfo;
            CurrentProcess.Start();

            Progress<string> onStdOut = new(x =>
            {
                System.Diagnostics.Debug.WriteLine("LINE: " + x + "   : " + x.Length);
                bool isInitialLine = x.Contains("More about packJPG and packMP3");
                if (!isInitialLine) 
                {
                    if (x.Length == 28)
                    {
                        if (float.TryParse(x.Split(" ")[1].Replace("%", ""), out float currentProgress))
                        {
                            ProgressLabel.Content = currentProgress + "%";
                            VerboseProgressLabel.Content = OutputPath;
                            ProgressBar.Value = currentProgress;
                        }
                    }
                    else if (x.Contains("Done.")) OnDone();
                    else if (x.Contains("ERROR: ")) onError(x);
                }
                //if (isInitialLine) OutputTextBox.Text += x;
                //else
                //{
                //    if (x.Length == 12) return;
                //    OutputTextBox.Text = OutputTextBox.Text.Remove(OutputTextBox.Text.LastIndexOf("\n"));
                //    OutputTextBox.Text += "\n" + x;
                //    if (x.Length == 28) if (float.TryParse(x.Split(" ")[1].Replace("%", ""), out float currentProgress)) MainProgressBar.Value = currentProgress;
                //}
            });

            Task stdout = ReadTextReaderAsync(CurrentProcess.StandardOutput, onStdOut);
            Task stderr = ReadTextReaderAsync(CurrentProcess.StandardError, onStdErr);
        }

        public void OnDone() 
        {
            MessageBox.Show($"Decompression Done! File saved to {OutputPath}.", "Finished", MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsPanel.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;
        }

        public void StopProcess() 
        {
            CurrentProcess?.Kill(true);
            MessageBox.Show($"Process terminated.", "Finished", MessageBoxButton.OK, MessageBoxImage.Information);
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e) { Decompress(); }

        private void Window_Closed(object sender, EventArgs e) 
        {     
            CurrentProcess?.Kill(true);
            Application.Current.Shutdown();
        }

        private void StopProcessButton_Click(object sender, RoutedEventArgs e) { StopProcess(); }
    }
}
