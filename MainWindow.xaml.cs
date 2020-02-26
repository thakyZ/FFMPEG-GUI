using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System;
using System.Windows.Media;
using System.Threading;

namespace FFMPEG_GUI
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private StreamReader _standardError;

    private readonly string _standardErrorFileName = AppDomain.CurrentDomain.BaseDirectory + "error.log";
    private readonly ProcessStartInfo _processStartInfo = new ProcessStartInfo
    {
      CreateNoWindow = true,
      UseShellExecute = false,
      WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
      RedirectStandardError = true,
      FileName = "ffmpeg.exe"
    };

    public MainWindow() => InitializeComponent();

    private void Input_Button_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog
      {
        Title = "Open Media File...", // Window Title
        Filter = "All Files (*.*)|*.*" // Filter Nothing
      };

      // Show open file dialog box
      bool? result = dlg.ShowDialog();

      if (result == true)
      {
        // Copy file path.
        Input_TextBox.Text = dlg.FileName;
      }
    }

    private void Output_Button_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog dlg = new SaveFileDialog
      {
        Title = "Save Media File...", // Window Title
        Filter = "All Files (*.*)|*.*" // Filter Nothing
      };

      // Show save file dialog box
      bool? result = dlg.ShowDialog();

      if (result == true)
      {
        // Copy file path.
        Output_TextBox.Text = dlg.FileName;
      }
    }

    private void Convert_Button_Click(object sender, RoutedEventArgs e)
    {
      if (Directory.Exists(Path.GetDirectoryName(Output_TextBox.Text)) && File.Exists(Input_TextBox.Text))
      {
        int processCode = RunFFMPEG(Input_TextBox.Text, Output_TextBox.Text);
        if (processCode == 0)
        {
          Message_TextBox.Text = "Sucess!";
          Message_TextBox.Foreground = Brushes.Black;
        }
        else
        {
          Message_TextBox.Text = "Error!";
          Message_TextBox.Foreground = Brushes.Red;
        }
      }
      else if (!Directory.Exists(Path.GetDirectoryName(Output_TextBox.Text)))
      {
        Message_TextBox.Text = "Output file is not a valid file or directory.";
        Message_TextBox.Foreground = Brushes.Red;
      }
      else if (!File.Exists(Input_TextBox.Text))
      {
        Message_TextBox.Text = "Input file is not a valid file or directory.";
        Message_TextBox.Foreground = Brushes.Red;
      }
    }

    private int RunFFMPEG(string path1, string path2)
    {
      Thread standardErrorThread = null;

      _standardError = null;

      int exitCode = -1;

      try
      {
        using (Process process = new Process())
        {
          _processStartInfo.Arguments = string.Format(" -loglevel verbose -i \"{0}\" -y \"{1}\"", path1, path2);
          process.StartInfo = _processStartInfo;
          process.Start();

          if (process.StartInfo.RedirectStandardError)
          {
            _standardError = process.StandardError;
            standardErrorThread = StartThread(new ThreadStart(WriteStandardError), "StandardError");
          }

          process.WaitForExit();
          exitCode = process.ExitCode;
        }
      }
      finally // Ensure that the threads do not persist beyond the process being run
      {
        if (standardErrorThread != null)
        {
          standardErrorThread.Join();
        }
      }

      return exitCode;
    }

    private static Thread StartThread(ThreadStart startInfo, string name)
    {
      Thread t = new Thread(startInfo)
      {
        IsBackground = true,
        Name = name
      };
      t.Start();
      return t;
    }

    private void WriteStandardError()
    {
      using (StreamWriter writer = File.CreateText(_standardErrorFileName))
      using (StreamReader reader = _standardError)
      {
        writer.AutoFlush = true;

        for (; ; )
        {
          string textLine = reader.ReadLine();

          if (textLine == null)
          {
            break;
          }

          writer.WriteLine(textLine);
        }
      }

      if (File.Exists(_standardErrorFileName))
      {
        FileInfo info = new FileInfo(_standardErrorFileName);

        if (info.Length < 4)
        {
          info.Delete();
        }
      }
    }
  }
}
