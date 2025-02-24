using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace WpfApp10
{
    public partial class MainWindow : Window
    {

        private List<string> forbidden_words = new List<string>();

        static List<string> GetTxtFilePaths(string path)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path, "*.txt"));
                foreach (var directory in Directory.GetDirectories(path))
                {
                    files.AddRange(GetTxtFilePaths(directory));
                }
            }
            catch { }

            return files;
        }

        static async Task ProcessFileAsync(string file_path, string output_folder, List<string> forbiddenWords)
        {
            try
            {
                string file_content = File.ReadAllText(file_path);
                int count = 0;
                foreach (var word in forbiddenWords)
                {
                    if (file_content.Contains(word))
                    {
                        file_content = file_content.Replace(word, "*******");
                        count++;
                    }
                }
                if (count > 0)
                {
                    string fileName = Path.GetFileName(file_path);
                    string output_file_pth = Path.Combine(output_folder, "Mod_" + fileName);
                    File.WriteAllText(output_file_pth, file_content);
                    output_file_pth = Path.Combine(output_folder, fileName);
                    File.WriteAllText(output_file_pth, File.ReadAllText(file_path));
                    System.IO.FileInfo file = new System.IO.FileInfo(file_path);
                    file_content = File.ReadAllText(Path.Combine(output_folder, "Otchet.txt"));
                    file_content += "Путь: " + file_path + " Размер в байтах: " + file.Length.ToString() + "\n";
                    File.WriteAllText(Path.Combine(output_folder, "Otchet.txt"), file_content);
                }
            }
            catch { }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadWordsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                forbidden_words = File.ReadAllLines(dialog.FileName).ToList();
                ForbiddenWordsTextBox.Text = string.Join(Environment.NewLine, forbidden_words);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                forbidden_words = ForbiddenWordsTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (forbidden_words.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, введите или загрузите запрещенные слова.");
                    return;
                }
                if (!Directory.Exists(FilePathTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите полный путь к существующей директории.");
                    return;
                }
                OverallProgressBar.Value = 10; 
                Thread.Sleep(2000);
                string outputFolder = FilePathTextBox.Text;
                FilePathListBox.Items.Clear();
                StartButton.IsEnabled = false;
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                List<string> txt_fle_paths = new List<string>();
                foreach (var drive in drives)
                {
                    try
                    {
                        txt_fle_paths.AddRange(GetTxtFilePaths(drive.RootDirectory.FullName));
                    }
                    catch { }
                }
                OverallProgressBar.Value = 75;
                File.Create(Path.Combine(outputFolder, "Otchet.txt")).Close();
                var tasks = txt_fle_paths.Select(filePath => ProcessFileAsync(filePath, outputFolder, forbidden_words)).ToList();
                OverallProgressBar.Value = 95;
                List<string> strings = File.ReadAllText(Path.Combine(outputFolder, "Otchet.txt")).Split('\n').ToList();
                FilePathListBox.ItemsSource = strings;
                StartButton.IsEnabled = true;
                StatusTextBlock.Text = "Status: Completed";
            }
            catch { }
        }
    }

    public partial class App : Application
    {
        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "WpfApp10";
            bool createdNew;
            _mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Приложение уже запущено!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}