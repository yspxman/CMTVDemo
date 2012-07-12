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

using CMTVEngine;
namespace CMTVApp
{
    public partial class ProgramList : PhoneApplicationPage
    {
        public ProgramList()
        {
            InitializeComponent();

            App.EngineInstance.GetUIDataModel.ReadAllChannelsToDC();
            
            CreateItems();
            this.Loaded += new RoutedEventHandler(Page_Loaded);
        }

        private void CreateItems()
        {
            foreach (DataModel_Channel channel_data in App.EngineInstance.GetUIDataModel.DC_AllChannels)
            {
                PivotItem item = new PivotItem();
                ListBox lb = new ListBox();
                lb.ItemTemplate = this.LbTemplate;

                //！！！ Listbox 绑定数据源是用itemSource 而不是 DataContext！！！！！
                lb.ItemsSource = App.EngineInstance.GetUIDataModel.DC_ProgramLst;
                item.Header = channel_data.ChannelName;
                item.Content = lb;
                item.DataContext = channel_data;   // assign channel data to the current item,

                this.PgmLstPivot.Items.Add(item);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs a)
        {
           // App.ViewModel.ReadServiceToCollection();
            // update content
        }

        private T FindFirstElementInVisualTree<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            if (count == 0)
                return null;
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var result = FindFirstElementInVisualTree<T>(child);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private void PgmLstPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
  
        }

        private void PgmLstPivot_LoadingPivotItem(object sender, PivotItemEventArgs e)
        {
           // var v = e.Item.Parent;
            // loading programs for current channel.
            PivotItem p = e.Item;
            if (p != null)
            {
                DataModel_Channel cur_channel = p.DataContext as DataModel_Channel;
                if (cur_channel != null)
                {
                    //MessageBox.Show(d.ID + " " + d.Name);
                    App.EngineInstance.GetUIDataModel.ReadProgramsToDCFromChannelID(cur_channel.ID);
                }
            }
        }
    }
}