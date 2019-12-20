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
        string[] loopModesHints = { "Loop: off", "Loop: one", "Loop: all"};
        string currently = "Currently Playing: ";
        string[] kbShortcuts =
        {
            "Ctrl + N : Add file(s) to current playlist",
            "Ctrl + O : Open a saved playlist",
            "Ctrl + S : Save current playlist",
            "Ctrl + Shift + J : Previous Song",
            "Ctrl + Shift + K : Play/Pause",
            "Ctrl + Shift + L : Next Song"
        };

        MediaPlayer _player = new MediaPlayer();
        DispatcherTimer _timer;
        BindingList<FileInfo> _fullPaths = new BindingList<FileInfo>();
        int _lastIndex = -1;
        private IKeyboardMouseEvents _hook;

        bool _isPlaying = false;
        bool _isRandomOrder = false;
        int _loopMode = 0; // 0 - loop off, 1 - loop One, 2 - loop All

        BitmapImage _playIcon;
        BitmapImage _pauseIcon;
        BitmapImage[] _loopModes;
        BitmapImage _randomOnIcon;
        BitmapImage _randomOffIcon;

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
            sliderSeeker.Minimum = 0;
            sliderSeeker.Maximum = _player.NaturalDuration.TimeSpan.TotalSeconds;
            //sliderSeeker.SmallChange = 1;
            //sliderSeeker.LargeChange = 10;
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
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
                btnPlayIcon.Source = _playIcon;
                _isPlaying = false;
            }
            else
            {
                if (_player.Source != null)
                {
                    _player.Play();
                    btnPlayIcon.Source = _pauseIcon;
                    _isPlaying = true;
                    _timer.Start();
                }
                else
                {
                    if (_lastIndex < 0)
                        _lastIndex = 0;
                    PlaySelectedIndex(_lastIndex);
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
                System.Windows.MessageBox.Show("The file you have chosen is not an MP3 file",
                    "Invalid Extension");
                return;
            }

            var name = _fullPaths[i].Name;
            var converter = new NameConverter();
            var shortname = converter.Convert(name, null, null, null);
            Title = appName + $" : { shortname}";
            labelCurrentPlay.Text = currently + shortname;

            _player.Open(new Uri(filename, UriKind.Absolute));

            // Tạm ngưng 0.5 s trước khi chuyển sang bài kế tiếp
            System.Threading.Thread.Sleep(500);
            TimeSpan duration;
            if (_player.NaturalDuration.HasTimeSpan)
            {
                duration = _player.NaturalDuration.TimeSpan;
                //sliderSeeker.Minimum = 0;
                //sliderSeeker.Maximum = duration.TotalSeconds;
            }
            else
            {
                System.Windows.MessageBox.Show("Error occured!");
                return;
            }
            //var testDuration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds - 20);
            //_player.Position = testDuration;

            ButtonPlay_Click(buttonPlay, null);
            //_player.Play();
            //_isPlaying = true;
            //_timer.Start();

        }

        private void _player_MediaEnded(object sender, EventArgs e)
        {
            _isPlaying = false;
            sliderSeeker.Value = 0;
            if (_isRandomOrder == false)
            {
                switch (_loopMode)
                {
                    case 0: // loop off
                        if (_lastIndex < _fullPaths.Count - 1)
                            _lastIndex++;
                        else
                        {
                            return;
                        }
                        break;
                    case 1: // loop one
                        break;
                    case 2: // loop all
                        _lastIndex = (_lastIndex + 1) % _fullPaths.Count;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Random rng = new Random();
                _lastIndex = rng.Next(_fullPaths.Count);
            }
            PlaySelectedIndex(_lastIndex);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (_player.Source != null)
            {
                sliderSeeker.Value = _player.Position.TotalSeconds;

                //var filename = _fullPaths[_lastIndex].Name;
                //var converter = new NameConverter();
                //var shortname = converter.Convert(filename, null, null, null);

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
                //Title = appName + $" : { shortname}";
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
            LoadRecentPlayList();
            _playIcon = new BitmapImage(new Uri("Images/play_1.png", UriKind.Relative));
            _pauseIcon = new BitmapImage(new Uri("Images/pause_1.png", UriKind.Relative));
            _loopModes = new BitmapImage[3];
            _loopModes[0] = new BitmapImage(new Uri("Images/repeat_off.png", UriKind.Relative));
            _loopModes[1] = new BitmapImage(new Uri("Images/repeat_one.png", UriKind.Relative));
            _loopModes[2] = new BitmapImage(new Uri("Images/repeat_all.png", UriKind.Relative));
            _randomOnIcon = new BitmapImage(new Uri("Images/shuffle_on.png", UriKind.Relative));
            _randomOffIcon = new BitmapImage(new Uri("Images/shuffle_off.png", UriKind.Relative));
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void KeyUp_hook(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control && e.Shift && (e.KeyCode == Keys.J))
            {
                ButtonPrevious_Click(null, null);
            }

            if (e.Control && e.Shift && (e.KeyCode == Keys.L))
            {
                //System.Windows.MessageBox.Show("Ctrl + Shift + L pressed"); ;
                ButtonNext_Click(null, null);   
            }

            if (e.Control && e.Shift && (e.KeyCode == Keys.K))
            {
                ButtonPlay_Click(null, null);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.O))
            {
                ButtonLoad_Click(buttonLoad, null);
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.S))
            {
                ButtonSave_Click(buttonSave, null);
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.N))
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
                SaveRecentPlayList();
                _hook.KeyUp -= KeyUp_hook;
                _hook.Dispose();
            }
        }

        private void SaveRecentPlayList()
        {
            string filename = "recent.txt";
            var writer = new StreamWriter(filename);
            writer.WriteLine(_lastIndex);
            writer.WriteLine(_fullPaths.Count);
            foreach (var path in _fullPaths)
            {
                writer.WriteLine(path);
            }
            writer.Close();
        }

        private void LoadRecentPlayList()
        {
			StreamReader reader;
            try
            {
                reader = new StreamReader("recent.txt");
            }
            catch (FileNotFoundException)
            {
                //System.Windows.MessageBox.Show(ex.Message, "File Not Found!");
                var writer = new StreamWriter("recent.txt");
                writer.Close();
                return;
            }
            

            // first line is the index last played music file
            _lastIndex = int.Parse(reader.ReadLine());
            

            // second line is the number of files in the playlist
            int count = int.Parse(reader.ReadLine());
            for (int i = 0; i < count; i++)
            {
                string filename = reader.ReadLine();
                FileInfo info = new FileInfo(filename);
                _fullPaths.Add(info);
            }

            if (_lastIndex >= 0)
            {
                var filename = _fullPaths[_lastIndex].Name;
                var converter = new NameConverter();
                var shortname = converter.Convert(filename, null, null, null);
                labelCurrentPlay.Text = currently + shortname;
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
            ButtonStop_Click(null, null);
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
            _isRandomOrder = true;
            randomModeIcon.Source = _randomOnIcon;
            buttonShuffle.ToolTip = "Random: on";
        }

        private void ButtonShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            _isRandomOrder = false;
            randomModeIcon.Source = _randomOffIcon;
            buttonShuffle.ToolTip = "Random: off";
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
            _fullPaths.Clear();
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

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            sliderSeeker.Value = _player.Position.TotalSeconds;
            _isPlaying = false;
            _timer.Stop();
            btnPlayIcon.Source = _playIcon;
            string tmp = labelDuration.Content.ToString();
            tmp = tmp.Remove(0, tmp.IndexOf('/'));
            tmp = tmp.Insert(0, "00:00 ");
            labelDuration.Content = tmp;
        }

        private void ButtonPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_lastIndex > 0)
            {
                _lastIndex--;
                ButtonStop_Click(null, null);
                PlaySelectedIndex(_lastIndex);
            }
            else
            {
                if (_loopMode == 2) // Loop All
                {
                    _lastIndex = _fullPaths.Count - 1;
                    ButtonStop_Click(null, null);
                    PlaySelectedIndex(_lastIndex);
                }
            }
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (_lastIndex < _fullPaths.Count - 1)
            {
                _lastIndex++;
                ButtonStop_Click(null, null);
                PlaySelectedIndex(_lastIndex);
            }
            else
            {
                if (_loopMode == 2) // Loop All
                {
                    _lastIndex = 0;
                    ButtonStop_Click(null, null);
                    PlaySelectedIndex(_lastIndex);
                }
            }
        }

        private void ButtonLoopMode_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonLoopMode_Unchecked(object sender, RoutedEventArgs e)
        {

        }
        
        private void ButtonLoopMode_Indeterminate(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonLoopMode_Click(object sender, RoutedEventArgs e)
        {
            _loopMode = (_loopMode + 1 ) % 3;
            btnLoopIcon.Source = _loopModes[_loopMode];
            buttonLoopMode.ToolTip = loopModesHints[_loopMode];
        }

        private void HelpShortcuts_Click(object sender, RoutedEventArgs e)
        {
            string toShow = "";
            foreach (var sc in kbShortcuts)
            {
                toShow = toShow + sc + "\n";
            }
            System.Windows.MessageBox.Show(toShow, "Keyboard Shortcuts");
        }

        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonAdd_Click(buttonAdd, null);
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonLoad_Click(buttonLoad, null);
        }

        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ButtonSave_Click(buttonSave, null);
        }
    }
}