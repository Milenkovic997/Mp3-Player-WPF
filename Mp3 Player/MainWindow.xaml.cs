using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

            volumeSlider.Value = 1;

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
                sp.Background = new SolidColorBrush(Color.FromArgb(0xFF, 45, 45, 45));
            }
            spanel.Background = Brushes.LightSlateGray;

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
                if(sp.Tag == filename)
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
            lblStatus.Content = "Paused...";

            btnPlay.Visibility = Visibility.Visible;
            btnPause.Visibility = Visibility.Collapsed;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (lblStatus.Content != "Paused...")
                {
                    _mediaPlayer.Pause();
                    lblStatus.Content = "Paused...";

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
                lblStatus.Content = "Stopped...";

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
                            Width = 1150,
                            Margin = new Thickness(2),
                            Background = new SolidColorBrush(Color.FromArgb(0xFF, 45, 45, 45)),
                            Tag = file,
                            Cursor = Cursors.Hand
                        };
                        sp.MouseLeftButtonDown += new MouseButtonEventHandler(addedSongsClick);
                        addedSongsScrollViewer.Children.Add(sp);

                        TextBlock tb = new TextBlock
                        {
                            Text = _songCounter + ". " + Path.GetFileName(file).Replace(".mp3", ""),
                            FontFamily = new FontFamily("Courier New"),
                            Tag = file,
                            FontWeight = FontWeights.Bold,
                            FontSize = 16,
                            Width = 1010,
                            Foreground = Brushes.White,
                            Margin = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        sp.Children.Add(tb);
                    }
                }
            }
        }

        // SHUFFLE BUTTONS
        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            _shuffle = false;
            btnShuffle.Visibility = Visibility.Collapsed;
            btnNoShuffle.Visibility = Visibility.Visible;
        }
        private void btnNoShuffle_Click(object sender, RoutedEventArgs e)
        {
            _shuffle = true;
            btnShuffle.Visibility = Visibility.Visible;
            btnNoShuffle.Visibility = Visibility.Collapsed;
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
            if(volumeSlider.Value > 0)
            {
                volumeIconOff.Visibility = Visibility.Collapsed;
                volumeIconOn.Visibility = Visibility.Visible;
            }
            else
            {
                volumeIconOn.Visibility = Visibility.Collapsed;
                volumeIconOff.Visibility = Visibility.Visible;
            }
        }
        private void volumeIconOn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _volumeValue = volumeSlider.Value;
            volumeSlider.Value = 0;
            volumeIconOn.Visibility = Visibility.Collapsed;
            volumeIconOff.Visibility = Visibility.Visible;
        }
        private void volumeIconOff_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            volumeSlider.Value = _volumeValue;
            volumeIconOff.Visibility = Visibility.Collapsed;
            volumeIconOn.Visibility = Visibility.Visible;
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
            lblExit.Foreground = Brushes.White;
        }
        private void btnExit_MouseLeave(object sender, MouseEventArgs e)
        {
            btnExit.Background = Brushes.LightGray;
            lblExit.Foreground = Brushes.Black;
        }
        private void btnExit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

    }
}