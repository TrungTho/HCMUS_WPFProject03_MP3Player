using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MP3_MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string appName = "MP3 Music Player";

        MediaPlayer _player = new MediaPlayer();
        DispatcherTimer _timer;
        BindingList<FileInfo> _fullPaths = new BindingList<FileInfo>();
        int _lastIndex = -1;
        private IKeyboardMouseEvents _hook;

        bool _isPlaying = false;
        bool _isRandomOrder = false;

        BitmapImage playIcon;
        BitmapImage pauseIcon;
        

        public MainWindow()
        {
            InitializeComponent();
            _player.MediaOpened += _player_MediaOpened;
            _player.MediaEnded += _player_MediaEnded;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += timer_Tick;

            // Dang ky su kien hook
            _hook = Hook.GlobalEvents();
            _hook.KeyUp += KeyUp_hook;
        }

        private void _player_MediaOpened(object sender, EventArgs e)
        {
            var position = _player.NaturalDuration.TimeSpan;
            sliderSeeker.Minimum = 0;
            sliderSeeker.Maximum = position.TotalSeconds;

        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            //if (listBoxPlaylist.SelectedIndex >= 0)
            //{

            //    _lastIndex = listBoxPlaylist.SelectedIndex;
            //    PlaySelectedIndex(_lastIndex);
            //}
            //else
            //{
            //    System.Windows.MessageBox.Show("No file selected!");
            //    return;
            //}

            if (_isPlaying)
            {
                _player.Pause();
                btnPlayIcon.Source = pauseIcon;
                _isPlaying = false;
            }
            else
            {
                if (_player.Source != null)
                {
                    _player.Play();
                    btnPlayIcon.Source = playIcon;
                    _isPlaying = true;
                }
            }
            
            
        }

        private void PlaySelectedIndex(int i)
        {
            string filename;
            if (_fullPaths[i].Extension == ".mp3")
            {
                filename = _fullPaths[i].FullName;
            }
            else
            {
                System.Windows.MessageBox.Show("The file you have chosen is not an MP3 file", "Invalid Extension");
                return;
            }

            
            _player.Open(new Uri(filename, UriKind.Absolute));

            // Tạm ngưng 0.5 s trước khi chuyển sang bài kế tiếp
            System.Threading.Thread.Sleep(500);
            TimeSpan duration;
            if (_player.NaturalDuration.HasTimeSpan)
                duration = _player.NaturalDuration.TimeSpan;
            else
            {
                System.Windows.MessageBox.Show("Error occured!");
                return;
            }
            //var testDuration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds - 20);
            //_player.Position = testDuration;

            _player.Play();
            _isPlaying = true;
            _timer.Start();
        }

        private void _player_MediaEnded(object sender, EventArgs e)
        {
            _lastIndex++;
            PlaySelectedIndex(_lastIndex);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (_player.Source != null)
            {
                sliderSeeker.Value = _player.Position.TotalSeconds;

                var filename = _fullPaths[_lastIndex].Name;
                var converter = new NameConverter();
                var shortname = converter.Convert(filename, null, null, null);

                var currentPos = _player.Position.ToString(@"mm\:ss");
                string duration = "";
                if (_player.NaturalDuration.HasTimeSpan)
                    duration = _player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                else
                {
                    System.Windows.MessageBox.Show("Error occured!");
                    return;
                }
                labelDuration.Content = String.Format($"{currentPos} / {duration}");
                Title = appName + $" : { shortname}";
            }
            else
                labelDuration.Content = "No file selected...";
        }
        
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog();
            screen.Title = "Add new music files to current playlist";
            screen.Multiselect = true;
            if (screen.ShowDialog() == true)
            {
                foreach (var filename in screen.FileNames)
                {
                    var info = new FileInfo(filename);
                    _fullPaths.Add(info);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listBoxPlaylist.ItemsSource = _fullPaths;
            playIcon = new BitmapImage(new Uri("Images/play_1.png", UriKind.Relative));
            pauseIcon = new BitmapImage(new Uri("Images/pause_1.png", UriKind.Relative));
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void KeyUp_hook(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control && e.Shift && (e.KeyCode == Keys.E))
            {
                //System.Windows.MessageBox.Show("Ctrl + Shift + E pressed"); ;
                _lastIndex++;
                PlaySelectedIndex(_lastIndex);
            }
            if (e.Control && (e.KeyCode == Keys.O))
            {
                ButtonLoad_Click(buttonLoad, null);
            }
            if (e.Control && (e.KeyCode == Keys.S))
            {
                ButtonSave_Click(buttonSave, null);
            }
            if (e.Control && (e.KeyCode == Keys.A))
            {
                ButtonAdd_Click(buttonAdd, null);
            }
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to exit?", "Exiting...", System.Windows.MessageBoxButton.YesNo)
                != System.Windows.MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                //SaveCurrentPlayList();
                _hook.KeyUp -= KeyUp_hook;
                _hook.Dispose();
            }
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(this,
                "Nguyễn Khánh Hoàng - 1712457\n      Trần Trung Thọ      - 1712798",
                "About Us");
        }

        private void listBoxItemPlay_Click(object sender, RoutedEventArgs e)
        {
            var clickedItem = sender as System.Windows.Controls.MenuItem;
            var selected = clickedItem.DataContext as FileInfo;
            _lastIndex = _fullPaths.IndexOf(selected);
            PlaySelectedIndex(_lastIndex);           
        }

        private void ButtonRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selecteds = new List<object>();
            foreach (var item in listBoxPlaylist.SelectedItems)
            {
                selecteds.Add(item);
            }
            foreach (var selected in selecteds)
            {
                _fullPaths.Remove(selected as FileInfo);
            }
        }

        private void ButtonRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            _fullPaths.Clear();
        }

        private void ButtonShuffle_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SliderSeeker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //int pos = Convert.ToInt32(sliderSeeker.Value);
            double pos = sliderSeeker.Value;
            _player.Position = TimeSpan.FromSeconds(pos);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.SaveFileDialog();
            //screen.AddExtension = true;
            if (screen.ShowDialog() == true)
            {
                string playlist = screen.FileName;
                var writer = new StreamWriter(playlist);
                foreach (var path in _fullPaths)
                {
                    writer.WriteLine(path);
                }

                writer.Close();
                //if (fileinfo.Exists)
                //{
                //    System.Windows.MessageBox.Show("Please specify a different filename", "Duplicate Filename Found");
                //}
                //else { }
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog();
            if (screen.ShowDialog() == true)
            {
                string playlist = screen.FileName;
                var reader = new StreamReader(playlist);
                do
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    FileInfo info = new FileInfo(line);
                    if (info.Exists)
                    {
                        _fullPaths.Add(info);
                    }
                } while (true);
                    
            }
        }
    }
}