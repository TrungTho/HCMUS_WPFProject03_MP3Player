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
using System.Windows.Media.Animation;
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
        BindingList<TagLib.File> _fullPaths = new BindingList<TagLib.File>();
        int _playingSong = -1;
        int _lastPlayedSong = -1;
        private IKeyboardMouseEvents _hook;
        Storyboard story;
        BindingList<string> listOldPlaylist;
        Object playedItem;

        bool _isPlaying = false;
        bool _isRandomOrder = false;
        int _loopMode = 0; // 0 - loop off, 1 - loop One, 2 - loop All

        BitmapImage _playIcon;
        BitmapImage _pauseIcon;
        BitmapImage[] _loopModes;
        BitmapImage[] _speaker;
        BitmapImage _randomOnIcon;
        BitmapImage _randomOffIcon;
        BitmapImage defaultSongImage;

        public MainWindow()
        {
            InitializeComponent();

            //init model
            _player.MediaOpened += _player_MediaOpened;
            _player.MediaEnded += _player_MediaEnded;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += timer_Tick;
            story = new Storyboard();
            listOldPlaylist = new BindingList<string>();
            playedItem = new object();


            // Dang ky su kien hook
            _hook = Hook.GlobalEvents();
            _hook.KeyUp += KeyUp_hook;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listViewPlaylist.ItemsSource = _fullPaths;

            LoadImages();

            LoadRecentPlayList("recent.txt");
        }

        /*UI function*/
        private void LoadImages()
        {
            try
            {
                //button
                _playIcon = new BitmapImage(new Uri("Images/play_1.png", UriKind.Relative));
                _pauseIcon = new BitmapImage(new Uri("Images/pause_1.png", UriKind.Relative));
                _loopModes = new BitmapImage[3];
                _loopModes[0] = new BitmapImage(new Uri("Images/repeat_off.png", UriKind.Relative));
                _loopModes[1] = new BitmapImage(new Uri("Images/repeat_one.png", UriKind.Relative));
                _loopModes[2] = new BitmapImage(new Uri("Images/repeat_all.png", UriKind.Relative));
                _speaker = new BitmapImage[2];
                _speaker[0] = new BitmapImage(new Uri("Images/speaker.png", UriKind.Relative));
                _speaker[1] = new BitmapImage(new Uri("Images/mutespeaker.png", UriKind.Relative));
                _randomOnIcon = new BitmapImage(new Uri("Images/shuffle_on.png", UriKind.Relative));
                _randomOffIcon = new BitmapImage(new Uri("Images/shuffle_off.png", UriKind.Relative));

                //default song's image
                defaultSongImage = new BitmapImage(new Uri("Images/appIcon.png", UriKind.Relative));
                imageAnimation.Fill = new ImageBrush(defaultSongImage);
            }
            catch
            {
                System.Windows.MessageBox.Show("Some images can not be found, make sure you put them in same folder of execute file!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void startAnimation()
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(10)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            story.Children.Add(animation);
            Storyboard.SetTargetName(animation, imageAnimation.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            story.Begin(this,true);
        }
               
        private void pauseAnimation()
        {
            if (story != null)
            {
                story.Pause(this);
            }
        }

        private void resumeAnimation()
        {
            if (story!=null)
            {
                story.Resume(this);
            }
        }

        /*BUS function*/
        /// <summary>
        /// main function to play a song in listview
        /// </summary>
        /// <param name="i">index of song in source of truth - fullpath[]</param>
        private void PlaySelectedIndex(int i)
        {
            //highlight playing song
            listViewPlaylist.SelectedIndex = i;
            listViewPlaylist.ScrollIntoView(listViewPlaylist.SelectedItem);
            _lastPlayedSong = i;
            playedItem = listViewPlaylist.SelectedItem;

            string filename;
            if (_fullPaths[i].Properties.MediaTypes.ToString() == "Audio")
            {
                filename = _fullPaths[i].Name;
            }
            else
            {
                System.Windows.MessageBox.Show("The file you have chosen is not an MP3 file",
                    "Invalid Extension", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var converter = new NameConverter();
            var shortname = converter.Convert(filename, null, null, null);
            Title = appName + $" : { shortname}";
            //labelCurrentPlay.Text = currently + shortname;

            loadAlbumCover(i);

            startAnimation();

            _player.Open(new Uri(filename, UriKind.Absolute));

            // Tạm ngưng 0.5 s trước khi chuyển sang bài kế tiếp
            System.Threading.Thread.Sleep(500);
            TimeSpan duration;
            if (_player.NaturalDuration.HasTimeSpan)
            {
                duration = _player.NaturalDuration.TimeSpan;
            }
            else
            {
                System.Windows.MessageBox.Show("Error occured!");
                return;
            }

            ButtonPlay_Click(buttonPlay, null);
        }

        private void loadAlbumCover(int i)
        {
            //load image of song to animation section
            try
            {
                // Load image data in MemoryStream
                TagLib.IPicture pic = _fullPaths[i].Tag.Pictures[0];
                MemoryStream ms = new MemoryStream(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                // ImageSource for System.Windows.Controls.Image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                imageAnimation.Fill = new ImageBrush(bitmap);
            }
            catch (Exception e) //can't find image of song
            {
                imageAnimation.Fill = new ImageBrush(defaultSongImage);
            }
        }

        private void getNextSong(bool isNextSong)
        {
            if (_isRandomOrder == false)
            {
                switch (_loopMode)
                {
                    case 0: // loop off
                        if (isNextSong==true)
                        {
                            if (_playingSong < _fullPaths.Count - 1)
                                _playingSong++;
                            else
                            {
                                return;
                            }
                        }
                        else //get previous song
                        {
                            if (_playingSong > 0)
                                _playingSong--;
                            else
                            {
                                return;
                            }
                        }
                        break;
                    case 1: // loop one
                        break;
                    case 2: // loop all
                        if (isNextSong == true)
                        {
                            _playingSong = (_playingSong +1 ) % _fullPaths.Count;
                        }
                        else
                            _playingSong = (_playingSong - 1 + _fullPaths.Count) % _fullPaths.Count;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Random rng = new Random();
                _playingSong = rng.Next(_fullPaths.Count);
            }
        }

        private void _player_MediaEnded(object sender, EventArgs e)
        {
            //_isPlaying = false;
            //sliderSeeker.Value = 0;
            ButtonStop_Click(null, null);

            getNextSong(true);

            PlaySelectedIndex(_playingSong);
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

        private void _player_MediaOpened(object sender, EventArgs e)
        {
            sliderSeeker.Minimum = 0;
            sliderSeeker.Maximum = _player.NaturalDuration.TimeSpan.TotalSeconds;
            //sliderSeeker.SmallChange = 1;
            //sliderSeeker.LargeChange = 10;
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
                SaveRecentPlayList("recent.txt");
                _hook.KeyUp -= KeyUp_hook;
                _hook.Dispose();
            }
        }

        private void playNewSong()
        {
            //_lastIndex = listViewPlaylist.SelectedIndex;
            ButtonStop_Click(null, null);
            PlaySelectedIndex(_playingSong);
        }

        private void listBoxItemPlay_Click(object sender, RoutedEventArgs e)
        {
            _playingSong = listViewPlaylist.SelectedIndex;
            playNewSong();
        }

        private void listBoxItemPlay_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            _playingSong = listViewPlaylist.SelectedIndex;
            playNewSong();
        }

        private void ListboxOldPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listboxOldPlaylist.SelectedIndex < 0 || listboxOldPlaylist.SelectedIndex >= listOldPlaylist.Count)
                return;

            if (_fullPaths.Count == 0)
            {
                string filepath = listOldPlaylist[listboxOldPlaylist.SelectedIndex];
                LoadRecentPlayList(filepath);
            }
            else
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Remove present playlist and load new one?", "Load Playlist", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    string filepath = listOldPlaylist[listboxOldPlaylist.SelectedIndex];
                    LoadRecentPlayList(filepath);
                }
            }
        }

        /*Playlist button*/
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            var screen = new Microsoft.Win32.OpenFileDialog();
            screen.Title = "Add new music files to current playlist";
            screen.Multiselect = true;
            if (screen.ShowDialog() == true)
            {
                try
                {
                    foreach (var filename in screen.FileNames)
                    {
                        var info = TagLib.File.Create(filename);
                        _fullPaths.Add(info);
                    }
                }
                catch
                {

                }
            }
        }

        private void ButtonRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            bool isPlayingSongSelected = false;

            var selecteds = new List<object>();
            foreach (var item in listViewPlaylist.SelectedItems)
            {
                selecteds.Add(item);
                if (_playingSong > -1 && item == playedItem) 
                {
                    isPlayingSongSelected = true;
                }
            }

            if (isPlayingSongSelected == true)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Remove playing song?", "Remove Selected", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    foreach (var selected in selecteds)
                    {
                        _fullPaths.Remove(selected as TagLib.File);
                    }
                    _lastPlayedSong = -1;
                    _playingSong = -1;
                    imageAnimation.Fill = new ImageBrush(defaultSongImage);
                    Title = appName;
                    ButtonStop_Click(null, null);
                }
            }
            else
            {
                foreach (var selected in selecteds)
                {
                    _fullPaths.Remove(selected as TagLib.File);
                }
            }
            
        }

        private void ButtonRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Source != null)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Stop playing to remove all audio from list?", "Remove All", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    resetPlaylist();
                }
            }
            else
            {
                resetPlaylist();
            }
        }

        private void SaveRecentPlayList(string filepath)
        {
            string filename = filepath;
            int tmpCount = -1;
            foreach (var line in listViewPlaylist.Items)
            {
                tmpCount++;
                if (line == playedItem)
                {
                    _playingSong = tmpCount;
                    break;
                }
            }

            var writer = new StreamWriter(filename);
            writer.WriteLine(_playingSong);
            writer.WriteLine(_fullPaths.Count);

            foreach (var path in _fullPaths)
            {
                writer.WriteLine(path.Name);
            }
            writer.Close();

            filename = "recentPlaylists.txt";
            writer = new StreamWriter(filename);

            foreach (var path in listOldPlaylist)
            {
                writer.WriteLine(path);
            }
            writer.Close();
        }

        private void LoadRecentPlayList(string filepath)
        {
            resetPlaylist();

            StreamReader reader;
            //load audio of last playlist
            try
            {
                reader = new StreamReader(filepath);
                // first line is the index last played music file
                _playingSong = int.Parse(reader.ReadLine());


                // second line is the number of files in the playlist
                int count = int.Parse(reader.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    string filename = reader.ReadLine();
                    var info = TagLib.File.Create(filename);
                    _fullPaths.Add(info);
                }

                if (_playingSong >= 0)
                {
                    var filename = _fullPaths[_playingSong].Name;
                    var converter = new NameConverter();
                    var shortname = converter.Convert(filename, null, null, null);
                    Title = appName + $" : { shortname}";
                    loadAlbumCover(_playingSong);
                }

                //load all playlist has saved
                try
                {
                    string[] lines = File.ReadAllLines("recentPlaylists.txt");
                    foreach (var line in lines)
                    {
                        if (isDistinctPath(line))
                            listOldPlaylist.Add(line);
                    }

                    listboxOldPlaylist.ItemsSource = listOldPlaylist;
                }
                catch (FileNotFoundException)
                {

                }
            }
            catch (FileNotFoundException)
            {
                System.Windows.MessageBox.Show("File has no content or deleted!!!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                int tmpPos = -1;
                for (int i = 0; i < listOldPlaylist.Count; i++)
                    if (listOldPlaylist[i] == filepath)
                        tmpPos = i;
                if (tmpPos != -1)
                    listOldPlaylist.RemoveAt(tmpPos);
            }

        }

        private bool isDistinctPath(string filepath)
        {

            foreach (var path in listOldPlaylist)
                if (filepath == path)
                    return false;

            return true;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (_fullPaths.Count!=0)
            {
                var screen = new Microsoft.Win32.SaveFileDialog();
                //screen.AddExtension = true;
                if (screen.ShowDialog() == true)
                {
                    string playlist = screen.FileName;

                    if (isDistinctPath(playlist))
                    {
                        listOldPlaylist.Add(playlist);
                        listboxOldPlaylist.ItemsSource = listOldPlaylist;
                    }

                    SaveRecentPlayList(playlist);
                
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Empty playlist!!!");
            }
        }

        private void resetPlaylist()
        {
            _fullPaths.Clear();
            _lastPlayedSong = -1;
            _playingSong = -1;
            imageAnimation.Fill = new ImageBrush(defaultSongImage);
            Title = appName;
            ButtonStop_Click(null, null);
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (_fullPaths.Count != 0)
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Remove present playlist and load new one?", "Load Playlist",
                    System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    resetPlaylist();

                    var screen = new Microsoft.Win32.OpenFileDialog();
                    if (screen.ShowDialog() == true)
                    {
                        string playlist = screen.FileName;

                        //add to quick playlist
                        listOldPlaylist.Add(playlist);
                        listboxOldPlaylist.ItemsSource = listOldPlaylist;

                        var reader = new StreamReader(playlist);
                        _playingSong = int.Parse(reader.ReadLine());
                        int nSongs = int.Parse(reader.ReadLine());
                        for (int i = 0; i < nSongs; i++)
                        {
                            string line = reader.ReadLine();
                            var info = TagLib.File.Create(line);
                            if (info != null)
                            {
                                _fullPaths.Add(info);
                            }
                        }
                        //do
                        //{
                        //    string line = reader.ReadLine();
                        //    if (line == null) break;
                        //    var info = TagLib.File.Create(line);
                        //    if (info != null)
                        //    {
                        //        _fullPaths.Add(info);
                        //    }
                        //} while (true);
                    }
                }
            }
            else
            {
                var screen = new Microsoft.Win32.OpenFileDialog();
                if (screen.ShowDialog() == true)
                {
                    string playlist = screen.FileName;
                    var reader = new StreamReader(playlist);
                    _playingSong = int.Parse(reader.ReadLine());
                    int nSongs = int.Parse(reader.ReadLine());
                    for (int i = 0; i < nSongs; i++)
                    {
                        string line = reader.ReadLine();
                        var info = TagLib.File.Create(line);
                        if (info != null)
                        {
                            _fullPaths.Add(info);
                        }
                    }
                    //do
                    //{
                    //    string line = reader.ReadLine();
                    //    if (line == null) break;
                    //    var info = TagLib.File.Create(line);
                    //    if (info != null)
                    //    {
                    //        _fullPaths.Add(info);
                    //    }
                    //} while (true);
                }
            }
        }

        /*Control button*/
        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                _player.Pause();
                btnPlayIcon.Source = _playIcon;
                _isPlaying = false;
                //pauseAnimation();
                story.Pause(this);
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
                    if (_fullPaths.Count > 0)
                    {
                        if (_playingSong < 0)
                            _playingSong = 0;
                        PlaySelectedIndex(_playingSong);
                    }
                }
                resumeAnimation();
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            pauseAnimation();
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
            if (_isRandomOrder)
                getNextSong(true);
            else
                _playingSong = (_playingSong - 1 + _fullPaths.Count) % _fullPaths.Count;

            playNewSong();
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (_isRandomOrder)
                getNextSong(true);
            else
                _playingSong = (_playingSong + 1) % _fullPaths.Count;

            playNewSong();
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

        private double oldVolume = 50;
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _player.Volume = (double)sliderVolume.Value / (double)100;
            if (_speaker != null)
                if (sliderVolume.Value == 0)
                {
                    imageSpeaker.Source = _speaker[1];
                }
                else
                {
                    imageSpeaker.Source = _speaker[0];
                }
        }

        private void ButtonSpeaker_Click(object sender, RoutedEventArgs e)
        {
            if (sliderVolume.Value == 0)
            {
                sliderVolume.Value = oldVolume;
                imageSpeaker.Source = _speaker[0];
            }
            else
            {
                oldVolume = sliderVolume.Value;
                sliderVolume.Value = 0;
                imageSpeaker.Source = _speaker[1];
            }
        }

        /*Menu button*/
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