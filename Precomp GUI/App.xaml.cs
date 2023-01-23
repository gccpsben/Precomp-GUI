using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Precomp_GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Action<string,string> onPreceed = new((sourceFile, outputFile) => 
                {
                    if (sourceFile != "" && outputFile == "")
                    {
                        var sourceDirectory = Directory.GetParent(sourceFile);
                        if (sourceDirectory != null)
                        {
                            outputFile = Path.Combine(sourceDirectory.FullName, Path.GetFileNameWithoutExtension(sourceFile));
                            outputFile += ".output";
                        }
                    }
                    (new DecompressWindow(sourceFile, outputFile)).ShowDialog();
                    Application.Current.Shutdown();
                });


                if (e.Args.Length == 1)
                {
                    if (e.Args[0].EndsWith(".pcf"))
                    {
                        string sourceFile = e.Args.Length >= 1 ? e.Args[0] : "";
                        string outputFile = e.Args.Length >= 2 ? e.Args[1] : "";

                        onPreceed(sourceFile, outputFile);
                    }
                }
                else
                {
                    MessageBox.Show("Please launch with args: \n<source> <output?>\n-compress <source> <output>");
                    foreach (string s in e.Args)
                    {
                        MessageBox.Show($"Args Received: {s}");
                    }
                    Application.Current.Shutdown();
                }
                base.OnStartup(e);
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
                Application.Current.Shutdown();
            }
        }
    }
}
