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
    /// <summary>
    /// The error output stream reader.
    /// This is used to read the output of FFMPEG.
    /// </summary>
    private StreamReader _standardError;

    /// <summary>
    /// The path of the error log from the executable.
    /// </summary>
    private readonly string _standardErrorFileName = AppDomain.CurrentDomain.BaseDirectory + "error.log";
    /// <summary>
    /// The default information for the process to start with.
    /// </summary>
    private readonly ProcessStartInfo _processStartInfo = new ProcessStartInfo
    {
      // Do not show the window by default.
      CreateNoWindow = true,
      // This is needed to be set to false in order to be able to output to file.
      UseShellExecute = false,
      // This is to set the directory to the executable directory instead of the user folder.
      WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
      // This is needed to be set to true in order to be able to output to a file.
      RedirectStandardError = true,
      // This is the file name and extension of the file that we would like to run.
      // And of course it needs to be "ffmpeg.exe".
      FileName = "ffmpeg.exe"
    };

    /// <summary>
    /// Start the main window, there is really nothing to do here.
    /// </summary>
    public MainWindow() => InitializeComponent();

    /// <summary>
    /// This is the event if the input "Browse..." button gets clicked.
    /// </summary>
    /// <param name="sender">The sender of the event, usually the cursor.</param>
    /// <param name="e">The event arguments, which is the data sent by the cursor.</param>
    private void Input_Button_Click(object sender, RoutedEventArgs e)
    {
      // Set a open file dialog.
      OpenFileDialog dlg = new OpenFileDialog
      {
        Title = "Open Media File...", // Window Title
        Filter = "All Files (*.*)|*.*" // Filter Nothing
      };

      // Show open file dialog box
      bool? result = dlg.ShowDialog();

      // If the open file dialog was not canceled
      if (result == true)
      {
        // Copy file path.
        Input_TextBox.Text = dlg.FileName;
      }
    }

    /// <summary>
    /// This is the event if the output "Brose..." button gets clicked.
    /// </summary>
    /// <param name="sender">The sender of the event, usually the cursor.</param>
    /// <param name="e">The event arguments, which is the data sent by the cursor.</param>
    private void Output_Button_Click(object sender, RoutedEventArgs e)
    {
      // Set a save file dialog.
      SaveFileDialog dlg = new SaveFileDialog
      {
        Title = "Save Media File...", // Window Title
        Filter = "All Files (*.*)|*.*" // Filter Nothing
      };

      // Show save file dialog box
      bool? result = dlg.ShowDialog();

      // If the save file dialog was not canceled.
      if (result == true)
      {
        // Copy file path.
        Output_TextBox.Text = dlg.FileName;
      }
    }

    /// <summary>
    /// The event if the "Convert" button was pressed.
    /// </summary>
    /// <param name="sender">The sender of the event, usually the cursor.</param>
    /// <param name="e">The event arguments, which is the data sent by the cursor.</param>
    private void Convert_Button_Click(object sender, RoutedEventArgs e)
    {
      // Check if the directory that the file we want to output to exists. And make sure that the file we want to open exists.
      if (Directory.Exists(Path.GetDirectoryName(Output_TextBox.Text)) && File.Exists(Input_TextBox.Text))
      {
        // Get the output code for the function RunFFMPEG.
        int processCode = RunFFMPEG(Input_TextBox.Text, Output_TextBox.Text);
        // If the process succeed.
        if (processCode == 0)
        {
          // Set the message text box.
          Message_TextBox.Text = "Success!";
          // Set the text color to black.
          Message_TextBox.Foreground = Brushes.Black;
        }
        else
        {
          Message_TextBox.Text = "Error!";
          Message_TextBox.Foreground = Brushes.Red;
        }
      }
      // Check if the directory that we want to output to doesn't exist.
      else if (!Directory.Exists(Path.GetDirectoryName(Output_TextBox.Text)))
      {
        Message_TextBox.Text = "Output file is not a valid file or directory.";
        Message_TextBox.Foreground = Brushes.Red;
      }
      // Check if the file that we want to open exists.
      else if (!File.Exists(Input_TextBox.Text))
      {
        Message_TextBox.Text = "Input file is not a valid file or directory.";
        Message_TextBox.Foreground = Brushes.Red;
      }
    }

    /// <summary>
    /// Runs the Threads for FFMPEG and logging the errors.
    /// </summary>
    /// <param name="path1">Input file path</param>
    /// <param name="path2">Output file path</param>
    /// <returns>The exit code of FFMPEG</returns>
    private int RunFFMPEG(string path1, string path2)
    {
      // Sets a new thread for the error log handling to null.
      Thread standardErrorThread = null;

      // Sets the stream reader for the error log handling to null.
      _standardError = null;

      // Resets the exit code to negative one.
      int exitCode = -1;

      // try the contents, and fail if it doesn't work.
      try
      {
        // This is for using the code inside the keyword using while creating a new process.
        using (Process process = new Process())
        {
          // Set the process information arguments to include the input and output file paths.
          _processStartInfo.Arguments = string.Format(" -loglevel verbose -i \"{0}\" -y \"{1}\"", path1, path2);
          // Set the start info of the process to the process information.
          process.StartInfo = _processStartInfo;
          // Start the process.
          process.Start();

          // If the process information has RedirectStandardError set to true.
          if (process.StartInfo.RedirectStandardError)
          {
            // Set the stream reader for the error logging to the process's standard error.
            _standardError = process.StandardError;
            // Output the WriteStandardError thread to the new thread we created for error log handling.
            standardErrorThread = StartThread(new ThreadStart(WriteStandardError), "StandardError");
          }

          // Wait for the process to exit.
          process.WaitForExit();
          // If the process exists we set the exit code to the variable we created.
          exitCode = process.ExitCode;
        }
      }
      finally // Ensure that the threads do not persist beyond the process being run
      {
        // If the thread was created.
        if (standardErrorThread != null)
        {
          // Block the thread from being called again.
          standardErrorThread.Join();
        }
      }

      // And finally return the error code.
      return exitCode;
    }

    /// <summary>
    /// Starting a new thread so we can stream the output of the program to the file.
    /// </summary>
    /// <param name="startInfo">The function that is used to stream the messages.</param>
    /// <param name="name">The name of the type of messages.</param>
    /// <returns>A new Thread for handling.</returns>
    private static Thread StartThread(ThreadStart startInfo, string name)
    {
      // Create a temporary Thread.
      Thread t = new Thread(startInfo)
      {
        // Set the thread to run in the background.
        IsBackground = true,
        // Set the thread's name.
        Name = name
      };
      // Start the thread.
      t.Start();
      // And return the thread.
      return t;
    }

    /// <summary>
    /// This is to read and write the standard error messages from FFMPEG
    /// </summary>
    private void WriteStandardError()
    {
      // While using the file we want to output the log to and the console window's output.
      using (StreamWriter writer = File.CreateText(_standardErrorFileName))
      using (StreamReader reader = _standardError)
      {
        // After every WriteLine we want to flush, so we'll automatically do this.
        writer.AutoFlush = true;

        // While every line we get. We could use While but we don't want any termination condition.
        while(true)
        {
          // Read the text line
          string textLine = reader.ReadLine();

          // If the text line is nothing we will end the loop.
          if (textLine == null)
          {
            break;
          }

          // Then write the line of text to file if we didn't already break.
          writer.WriteLine(textLine);
        }
      }
      // If the output file exists after the loop.
      if (File.Exists(_standardErrorFileName))
      {
        // Get the file information
        FileInfo info = new FileInfo(_standardErrorFileName);

        // And if the file is less than 4 bytes (header length) then just delete.
        if (info.Length < 4)
        {
          info.Delete();
        }
      }
    }
  }
}
