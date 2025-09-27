using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace m4aToMp3;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void FileListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (System.IO.Path.GetExtension(file).ToLower() == ".m4a")
                {
                    this.FileListBox.Items.Add(file);
                }
            }
        }
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog()
        {
            Filter = "M4A文件 (*.m4a)|*.m4a",
            Multiselect = true
        };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!FileListBox.Items.Contains(file))
                    FileListBox.Items.Add(file);
            }
        }
    }

    private void SelectOutputDir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择输出目录",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "选择文件夹",
            Filter = "文件夹|*.folder",
            ValidateNames = false
        };
        if (dialog.ShowDialog() == true)
        {
            OutputDirTextBox.Text = System.IO.Path.GetDirectoryName(dialog.FileName);
        }
    }

    private async void StartConvert_Click(object sender, RoutedEventArgs e)
    {
        if (FileListBox.Items.Count == 0)
        {
            MessageBox.Show("请先添加要转换的m4a文件。", "提示");
            return;
        }
        if (string.IsNullOrWhiteSpace(OutputDirTextBox.Text))
        {
            MessageBox.Show("请先选择输出目录。", "提示");
            return;
        }
        ProgressBar.Value = 0;
        int total = FileListBox.Items.Count;
        string targetFormat = (FormatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "opus";
        for (int i = 0; i < total; i++)
        {
            string m4aFile = FileListBox.Items[i].ToString();
            string outputFile = System.IO.Path.Combine(OutputDirTextBox.Text, System.IO.Path.GetFileNameWithoutExtension(m4aFile) + "." + targetFormat);
            await ConvertM4aToMp3(m4aFile, outputFile, targetFormat);
            Dispatcher.Invoke(() => ProgressBar.Value = (i + 1) * 100 / total, DispatcherPriority.Background);
        }
        MessageBox.Show("全部转换完成！", "完成");
    }

    private Task ConvertM4aToMp3(string m4aPath, string outputPath, string format)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(m4aPath) || string.IsNullOrEmpty(outputPath))
            {
                Dispatcher.Invoke(() => MessageBox.Show("文件路径无效。", "错误"));
                return;
            }
            // 假设ffmpeg.exe已放在程序目录下
            string ffmpegPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (!System.IO.File.Exists(ffmpegPath))
            {
                Dispatcher.Invoke(() => MessageBox.Show("未找到ffmpeg.exe，请将其放在程序目录下。", "错误"));
                return;
            }
            string arguments;
            if (format == "opus")
            {
                arguments = $"-y -i \"{m4aPath}\" -c:a libopus \"{outputPath}\"";
            }
            else // mp3
            {
                arguments = $"-y -i \"{m4aPath}\" -c:a libmp3lame -q:a 2 \"{outputPath}\"";
            }
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var proc = System.Diagnostics.Process.Start(psi))
            {
                proc?.WaitForExit();
            }
        });
    }
}