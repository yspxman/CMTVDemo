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

namespace CMTVApp
{
    public partial class StartupPage : PhoneApplicationPage
    {
        //int i = 0;
        public StartupPage()
        {
            InitializeComponent();           
            this.Loaded += new RoutedEventHandler(StartupPage_Loaded);
            CompositionTarget.Rendering += (s, e) =>
                {
                    textBlock1.Text += ".";
                };
        }

        
        void StartupPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.EngineInstance.Initialize();
            // after initialize finished, navigate to main page
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            //Remove current page from the back stack, 
            //thus startup page will never be called after app starts up.  
            this.NavigationService.RemoveBackEntry();
            base.OnNavigatedFrom(e);
        }
    }
}