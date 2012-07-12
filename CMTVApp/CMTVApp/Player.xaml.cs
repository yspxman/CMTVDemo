
#define STREAM_SOURCE2


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Resources;

using System.IO.IsolatedStorage;
using CMTVEngine;
       


namespace CMTVApp
{
    

    public partial class player : PhoneApplicationPage
    {

#if STREAM_SOURCE2
         CmmbStreamSource2 source = null;
#else
         CmmbStreamSource source = null;
#endif


        public player()
        {
            InitializeComponent();
            //CompositionTarget.Rendering += new EventHandler(OnCompositionTarget_Rendering);

            MediaPlayer.Position = System.TimeSpan.FromSeconds(0);

            this.Loaded += new RoutedEventHandler(player_Loaded);
            CompositionTarget.Rendering += (s, e) =>
            {     


                TimeSpan duration = MediaPlayer.NaturalDuration.TimeSpan;                
                if (duration.TotalSeconds != 0)
                {
                    if (source != null)
                    {
                        source.PlayerPosition = MediaPlayer.Position.TotalSeconds;
                    }
                    double percentComplete = MediaPlayer.Position.TotalSeconds / duration.TotalSeconds;
                    //SliderBar.Value = percentComplete;
                    TimeSpan mediaTime = MediaPlayer.Position;
                    string text = string.Format("{0:00}:{1:00}", (mediaTime.Hours * 60) + mediaTime.Minutes,
                                                                  mediaTime.Seconds);
                    if (tb_timeline.Text != text)
                        tb_timeline.Text = text;
                }
            };
        }

        void player_Loaded(object sender, RoutedEventArgs e)
        {
            // loaded event only occurs after instance initiated.
            Utility.Trace("player_Loaded");
        }

        /*
        void OnCompositionTarget_Rendering(object sender, EventArgs a)
        {
            //rotate.Angle = (rotate.Angle + 2) % 360;
        }
        */

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // since media source will be closed automatically when page navigated away,
            // so we should re-create media source whenever page loaded

            Utility.Trace(String.Format("OnNavigatedTo mode = {0}", e.NavigationMode));

            if (App.State == App.AppActivationState.TombStone)
            {
                // need to do sth to restore the saved data
            }

            ReleaseStreamSource();
            // when this page is loaded.
            textBlock1.Text = App.EngineInstance.GetCurPlayingChannelName;
            CreateNewMediaSourceAndPlay();

        }

        private void CreateNewMediaSourceAndPlay()
        {
            int id = GetStreamIdxFromChannelId(App.EngineInstance.GetCurPlayingChannelID);
            #if STREAM_SOURCE2
            source = new CmmbStreamSource2(id, App.EngineInstance);
            #else
            source = new CmmbStreamSource(id, App.EngineInstance);
            #endif
            MediaPlayer.Volume = 100;
            this.MediaPlayer.SetSource(source);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
        }
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // need to save some data when navigating away.

            Utility.Trace(String.Format("OnNavigatedFrom mode = {0}", e.NavigationMode));
            App.EngineInstance.GetCmmbProvider.CancelProviding();
            MediaPlayer.Stop();
            ReleaseStreamSource();
            base.OnNavigatedFrom(e);
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            // next channel
            MediaPlayer.Stop();
            ReleaseStreamSource();

            App.EngineInstance.PrepareToPlayNextChannel();

            // update UI
            textBlock1.Text = App.EngineInstance.GetCurPlayingChannelName;

            CreateNewMediaSourceAndPlay();
        }

        private void ReleaseStreamSource()
        {
            if (source != null)
            {
                source.StopParserAndWorkerThreads();
                source = null;
            }
        }

        private void MediaPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            //App.EngineInstance.GeDemux.ResetBufferIdx();
        }

        private void MediaPlayer_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("MediaPlayer_BufferingProgressChanged");
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("MediaPlayer_MediaEnded");
        }

        private void MediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("MediaPlayer_MediaFailed" + e.ErrorException);
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
           // MessageBox.Show("MediaPlayer_MediaOpened");           
        }

        private int GetStreamIdxFromChannelId(string id)
        {
            int idx = 0;

            switch (id)
            {
                case "S1":
                    idx = 0;
                    break;
                case "S2":
                    idx = 1;
                    break;
                case "S3":
                    idx = 2;
                    break;
                case "S4":
                    idx = 3;
                    break;
                case "S5":
                    idx = 4;
                    break;
                case "S6":
                    idx = 5;
                    break;
                case "S7":
                    idx = 6;
                    break;
                default:
                    break;
            }

            return idx;

        }

        private void MediaPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void MediaPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            tb_Status.Text = String.Format("{0}", MediaPlayer.CurrentState);
        }
    }
}