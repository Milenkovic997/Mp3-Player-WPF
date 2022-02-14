using MaterialDesignThemes.Wpf;
using MediaToolkit;
using MediaToolkit.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VideoLibrary;

namespace Mp3_Player
{
    public partial class MainWindow : Window
    {
        // GLOBAL VALUES
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private DispatcherTimer _timer = new DispatcherTimer();
        private DispatcherTimer _sliderTimer = new DispatcherTimer();
        private string _selectedSongTitle;
        private int _songCounter = 0;
        private double _volumeValue;
        private bool _shuffle = false;
        private List<string> _songList = new List<string>();

        // INITIALIZATION
        public MainWindow()
        {
            InitializeComponent();

            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += timer_Tick;
            _timer.Start();

            _sliderTimer.Interval = TimeSpan.FromMilliseconds(250);
            _sliderTimer.Tick += _sliderTimer_Tick;
            _sliderTimer.Start();

            volumeSlider.Value = Properties.Settings.Default.volume;
            volumeIconCheck();

            _shuffle = Properties.Settings.Default.shuffle;
            checkShuffleState();

            SliderSongTime.ApplyTemplate();
            Thumb thumb = (SliderSongTime.Template.FindName("PART_Track", SliderSongTime) as Track).Thumb;
            thumb.MouseEnter += new MouseEventHandler(thumb_MouseEnter);
        }

        // THUMB CONTROL/ADD SONG FUNCTIONS/PLAY FUNCTIONS
        private void thumb_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                args.RoutedEvent = MouseLeftButtonDownEvent;
                (sender as Thumb).RaiseEvent(args);
                _sliderTimer.Stop();

            }
        }
        private void addedSongsClick(object sender, MouseButtonEventArgs e)
        {
            playSongStackPanel(sender as StackPanel);
        }
        private void playSongStackPanel(StackPanel spanel)
        {
            _mediaPlayer.Open(new Uri(spanel.Tag.ToString()));

            foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
            {
                sp.Background = new SolidColorBrush(Color.FromArgb(0xFF, 35, 35, 35));
            }
            spanel.Background = Brushes.Crimson;

            foreach (TextBlock tb in spanel.Children.OfType<TextBlock>())
            {
                _selectedSongTitle = tb.Text;
            }
            PlayFunction();
        }
        private void playSongFile(string filename)
        {
            _mediaPlayer.Open(new Uri(filename));

            foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
            {
                sp.Background = new SolidColorBrush(Color.FromArgb(0xFF, 45, 45, 45));
                if (sp.Tag == filename)
                {
                    sp.Background = Brushes.LightSlateGray;
                    foreach (TextBlock tb in sp.Children.OfType<TextBlock>())
                    {
                        _selectedSongTitle = tb.Text;
                    }
                }
            }
            PlayFunction();
        }

        // TIMER TICKS
        private void timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer.Source != null && _mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                lblCurrent.Content = _mediaPlayer.Position.ToString("mm\\:ss");
                lblTotal.Content = _mediaPlayer.NaturalDuration.TimeSpan.ToString("mm\\:ss");
            }
        }
        private void _sliderTimer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer.Source != null && _mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                SliderSongTime.Value = 10 * _mediaPlayer.Position.TotalSeconds / _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        // SLIDER MOVE TO SET SONG TIME
        private void SliderSongTime_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _timer.Stop();
            _sliderTimer.Stop();
        }
        private void SliderSongTime_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mediaPlayer.Source != null && _mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                _mediaPlayer.Position = TimeSpan.FromSeconds(SliderSongTime.Value / 10 * _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds);
                _timer.Start();
                _sliderTimer.Start();
            }

        }

        // PLAY/PAUSE/STOP BUTTONS
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            PlayFunction();
        }
        private void PlayFunction()
        {
            _mediaPlayer.Play();
            if (_songList.Count > 0 && _selectedSongTitle != null)
            {
                lblStatus.Content = _selectedSongTitle.Remove(0, _selectedSongTitle.IndexOf(" ") + 1);

                btnPause.Visibility = Visibility.Visible;
                btnPlay.Visibility = Visibility.Collapsed;
            }
        }
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Pause();
            lblStatus.Content = "- Paused -";

            btnPlay.Visibility = Visibility.Visible;
            btnPause.Visibility = Visibility.Collapsed;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (lblStatus.Content != "- Paused -")
                {
                    _mediaPlayer.Pause();
                    lblStatus.Content = "- Paused -";

                    btnPlay.Visibility = Visibility.Visible;
                    btnPause.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PlayFunction();
                }
            }
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_songList.Count > 0)
            {
                _mediaPlayer.Stop();
                lblStatus.Content = "- Stopped -";

                btnPlay.Visibility = Visibility.Visible;
                btnPause.Visibility = Visibility.Collapsed;
            }
        }

        // ADD BUTTON
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (String file in openFileDialog.FileNames)
                {
                    AddSong(file);
                }
            }
        }
        private void AddSong(string file)
        {
            bool isTheSongInTheList = false;
            for (int i = 0; i < _songList.Count; i++)
            {
                if (file.Equals(_songList[i]))
                {
                    isTheSongInTheList = true;
                }
            }

            if (isTheSongInTheList == false)
            {
                _songList.Add(file);
                _songCounter++;
                StackPanel sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 35, 35, 35)),
                    Tag = file,
                    Cursor = Cursors.Hand
                };
                sp.MouseLeftButtonDown += new MouseButtonEventHandler(addedSongsClick);
                addedSongsScrollViewer.Children.Add(sp);

                TextBlock tb = new TextBlock
                {
                    Text = _songCounter + ". " + Path.GetFileName(file).Replace(".mp3", ""),
                    FontFamily = new FontFamily("Bahnschrift Condensed"),
                    FontSize = 22,
                    Width = 1200,
                    Foreground = Brushes.White,
                    Margin = new Thickness(5)
                };
                sp.Children.Add(tb);

                if (_songCounter >= 12) { mainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible; }
                else { mainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled; }
            }

        }

        // SHUFFLE BUTTONS
        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            _shuffle = false;
            checkShuffleState();
        }
        private void btnNoShuffle_Click(object sender, RoutedEventArgs e)
        {
            _shuffle = true;
            checkShuffleState();
        }
        private void checkShuffleState()
        {
            if (_shuffle)
            {
                btnShuffle.Visibility = Visibility.Visible;
                btnNoShuffle.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnShuffle.Visibility = Visibility.Collapsed;
                btnNoShuffle.Visibility = Visibility.Visible;
            }

            Properties.Settings.Default.shuffle = _shuffle;
            Properties.Settings.Default.Save();
        }

        // PREVIOUS AND NEXT FUNCTIONS
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_shuffle)
            {
                _mediaPlayer.Play();
                bool onlyOnce = false;
                foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
                {
                    if (onlyOnce == false)
                    {
                        if (sp.Background == Brushes.LightSlateGray)
                        {
                            onlyOnce = true;
                            int index = _songList.IndexOf(sp.Tag.ToString());
                            if (index > 0) { playSongFile(_songList[index - 1]); }
                            else
                            {
                                playSongFile(_songList.LastOrDefault());
                            }
                        }
                    }
                }
            }
            else
            {
                _mediaPlayer.Play();
                bool onlyOnce = false;
                foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
                {
                    if (onlyOnce == false)
                    {
                        if (sp.Background == Brushes.LightSlateGray)
                        {
                            onlyOnce = true;
                            int index = _songList.IndexOf(sp.Tag.ToString());
                            if (index > 0) { playSongFile(_songList[index - 1]); }
                            else
                            {
                                playSongFile(_songList.LastOrDefault());
                            }
                        }
                    }
                }
            }
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_shuffle)
            {
                _mediaPlayer.Play();
                int index = 0;
                foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
                {

                    if (sp.Background == Brushes.LightSlateGray)
                    {
                        index = _songList.IndexOf(sp.Tag.ToString());

                    }

                }
                Random rnd = new Random();
                int newIndex = rnd.Next(0, _songList.Count);
                while (newIndex == index) newIndex = rnd.Next(0, _songList.Count);

                playSongFile(_songList[newIndex]);
            }
            else
            {
                _mediaPlayer.Play();
                bool onlyOnce = false;
                foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
                {
                    if (onlyOnce == false)
                    {
                        if (sp.Background == Brushes.LightSlateGray)
                        {
                            onlyOnce = true;
                            int index = _songList.IndexOf(sp.Tag.ToString());
                            if (index < _songList.Count - 1) { playSongFile(_songList[index + 1]); }
                            else
                            {
                                playSongFile(_songList.FirstOrDefault());
                            }
                        }
                    }
                }
            }
        }
        private void SliderSongTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderSongTime.Value >= 10)
            {
                if (_shuffle)
                {
                    _mediaPlayer.Play();
                    int index = 0;
                    foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
                    {

                        if (sp.Background == Brushes.LightSlateGray)
                        {
                            index = _songList.IndexOf(sp.Tag.ToString());

                        }

                    }
                    Random rnd = new Random();
                    int newIndex = rnd.Next(0, _songList.Count);
                    while (newIndex == index) newIndex = rnd.Next(0, _songList.Count);

                    playSongFile(_songList[newIndex]);
                }
                else
                {
                    _mediaPlayer.Play();
                    bool onlyOnce = false;
                    foreach (StackPanel sp in addedSongsScrollViewer.Children.OfType<StackPanel>())
                    {
                        if (onlyOnce == false)
                        {
                            if (sp.Background == Brushes.LightSlateGray)
                            {
                                onlyOnce = true;
                                int index = _songList.IndexOf(sp.Tag.ToString());
                                if (index < _songList.Count - 1) { playSongFile(_songList[index + 1]); }
                                else
                                {
                                    playSongFile(_songList.FirstOrDefault());
                                }
                            }
                        }
                    }
                }
            }
        }

        // VOLUME CONTROL
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = volumeSlider.Value;
            if (volumeSlider.Value > 0)
            {
                volumeIconOff.Visibility = Visibility.Collapsed;
                volumeIconOn.Visibility = Visibility.Visible;
            }
            else
            {
                volumeIconOn.Visibility = Visibility.Collapsed;
                volumeIconOff.Visibility = Visibility.Visible;
            }
            Properties.Settings.Default.volume = volumeSlider.Value;
            Properties.Settings.Default.Save();
        }
        private void volumeIconOn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _volumeValue = volumeSlider.Value;
            volumeSlider.Value = 0;
            volumeIconCheck();
        }
        private void volumeIconOff_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_volumeValue != 0) volumeSlider.Value = _volumeValue;
            else volumeSlider.Value = 1;
            volumeIconCheck();
        }
        private void volumeIconCheck()
        {
            if (volumeSlider.Value == 0)
            {
                volumeIconOn.Visibility = Visibility.Collapsed;
                volumeIconOff.Visibility = Visibility.Visible;
            }
            else
            {
                volumeIconOff.Visibility = Visibility.Collapsed;
                volumeIconOn.Visibility = Visibility.Visible;
            }
        }

        // DRAG WINDOW FUNCTION
        private void dragFrameFunction(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // EXIT BUTTON FUNCTIONS
        private void btnExit_MouseEnter(object sender, MouseEventArgs e)
        {
            btnExit.Background = Brushes.Red;
        }
        private void btnExit_MouseLeave(object sender, MouseEventArgs e)
        {
            btnExit.Background = new SolidColorBrush(Color.FromArgb(0xFF, 29, 29, 29));
        }
        private void btnExit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        // YOUTUBE DOWNLOAD OPTIONS
        private void youtubeButtonUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(textYoutubeUrl.Text))
                {
                    string searchUrl = "https://www.youtube.com/results?search_query=" + Regex.Replace(textYoutubeUrl.Text, @"[^\u0000-\u007F]+", string.Empty).Replace("()", "").Replace(",", "").Replace(" ", "%20");
                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(searchUrl);
                    myRequest.Method = "GET";
                    WebResponse myResponse = myRequest.GetResponse();
                    StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                    string result = sr.ReadToEnd();
                    sr.Close();
                    myResponse.Close();

                    listSearchResult.Children.Clear();

                    List<int> foundIndexesTitle = new List<int>();
                    for (int i = result.IndexOf("\"title\":{\"runs\":[{\"text\":\""); i > -1; i = result.IndexOf("\"title\":{\"runs\":[{\"text\":\"", i + 1)) { foundIndexesTitle.Add(i); }
                    for (int k = 0; k < 20; k++)
                    {
                        string startingText = result.Substring(foundIndexesTitle[k] + 26, 300);
                        int lastIndex = startingText.IndexOf("\"}],\"accessibility\"");
                        string finalTextTitle = startingText.Substring(0, lastIndex);

                        string code = result.Substring(foundIndexesTitle[k], 10000);
                        string codeForImg = result.Substring(foundIndexesTitle[k] - 2000, 10000);

                        string url = code.Substring(code.IndexOf("/watch?v="), 20);

                        int timefrom = code.IndexOf("\"}},\"simpleText\":\"") + 18;
                        string time = code.Substring(timefrom, code.Remove(0, timefrom).IndexOf("},\"") - 1);

                        int whofrom = code.IndexOf("longBylineText\":{\"runs\":[{\"text\":") + 34;
                        string who = code.Substring(whofrom, code.Remove(0, whofrom).IndexOf("\",\"navigationEndpoin"));

                        int imgfrom = codeForImg.IndexOf("\",\"thumbnail\":{\"thumbnails\":[{\"url\":\"") + 37;
                        string img = codeForImg.Substring(imgfrom, codeForImg.Remove(0, imgfrom).IndexOf("\",\"width\""));

                        StackPanel sp = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Background = new SolidColorBrush(Color.FromArgb(0xFF, 23, 23, 23)),
                            Margin = new Thickness(5, 5, 5, 0),
                            Tag = "https://www.youtube.com" + url,
                            Cursor = Cursors.Hand
                        };
                        sp.MouseLeftButtonDown += new MouseButtonEventHandler(songItemClick);
                        sp.MouseEnter += new MouseEventHandler(songTextEnter);
                        sp.MouseLeave += new MouseEventHandler(songTextLeave);
                        listSearchResult.Children.Add(sp);

                        Image dynamicImage = new Image
                        {
                            Width = 200,
                            Height = 100,
                            Stretch = Stretch.Fill
                        };
                        dynamicImage.Source = new BitmapImage(new Uri(img, UriKind.Absolute), new RequestCachePolicy(RequestCacheLevel.BypassCache)) { CacheOption = BitmapCacheOption.OnLoad };
                        sp.Children.Add(dynamicImage);

                        StackPanel spText = new StackPanel { Orientation = Orientation.Vertical };
                        sp.Children.Add(spText);

                        TextBlock tbTitle = new TextBlock
                        {
                            Width = 600,
                            Text = finalTextTitle,
                            FontSize = 22,
                            FontFamily = new FontFamily("Bahnschrift Condensed"),
                            Foreground = Brushes.White,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(15, 3, 10, 0)
                        };
                        spText.Children.Add(tbTitle);

                        TextBlock tbDescription = new TextBlock
                        {
                            Width = 600,
                            Text = "Duration: " + time.Replace(".", ":") + "     By: " + who,
                            FontSize = 18,
                            FontFamily = new FontFamily("Bahnschrift Condensed"),
                            Foreground = Brushes.Gray,
                            Margin = new Thickness(15, 0, 10, 3)
                        };
                        spText.Children.Add(tbDescription);
                    }
                    scrollViewer.ScrollToTop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void songTextLeave(object sender, MouseEventArgs e)
        {
            (sender as StackPanel).Background = new SolidColorBrush(Color.FromArgb(0xFF, 23, 23, 23));
        }
        private void songTextEnter(object sender, MouseEventArgs e)
        {
            (sender as StackPanel).Background = new SolidColorBrush(Color.FromArgb(0xFF, 45, 45, 45));
        }
        private void songItemClick(object sender, MouseButtonEventArgs e)
        {
            string url = (sender as StackPanel).Tag.ToString();
            string location = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (!string.IsNullOrEmpty(textLocationToDownload.Text)) location = textLocationToDownload.Text;
            SaveMP3(location, url);

            var drawer = DrawerHost.CloseDrawerCommand;
            drawer.Execute(null, null);
            scrollViewer.ScrollToTop();
        }
        private void SaveMP3(string SaveToFolder, string VideoURL)
        {
            string path = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.FullName;

            string source = SaveToFolder;
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(VideoURL);
            string videopath = Path.Combine(path, vid.FullName);
            File.WriteAllBytes(videopath, vid.GetBytes());

            var inputFile = new MediaFile { Filename = Path.Combine(path, vid.FullName) };
            var outputFile = new MediaFile { Filename = Path.Combine(source, vid.FullName.Replace(".mp4", "") + ".mp3") };
            string filenameFinal = outputFile.Filename;

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }
            File.Delete(Path.Combine(path, vid.FullName));
            AddSong(filenameFinal);
        }
        private void btnLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = @"C:\Users\Stefan\Desktop";
            dialog.Title = "Select a Directory";
            dialog.Filter = "Directory|*.this.directory"; 
            dialog.FileName = "select"; 
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");

                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                textLocationToDownload.Text = path;
            }
        }
    }
}