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
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Controls;
using System.Data.Linq;

using CMTVEngine;
using CMTVDataBase;

namespace CMTVApp
{
    public partial class SettingTest : PhoneApplicationPage
    {
        private DataModel_Channel currentService = null;
        private List<DataModel_ChannelExt> serviceExts = null;
        private int curServiceExtIdx = -1;

        public SettingTest()
        {
            InitializeComponent();
            //DataContext = App.ViewModel;
            
        }
 
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // when this page is loaded.

            base.OnNavigatedTo(e);
            string id = "";
            if (NavigationContext.QueryString.TryGetValue("SelectedItem", out id))
            {
                
                // Initializing data collections
                var dataset = App.EngineInstance.GetUIDataModel.QueryServiceFromID(id);
                currentService = dataset.FirstOrDefault();

                serviceExts = App.EngineInstance.GetUIDataModel.QueryServiceExtFromID(currentService.ID);
            }

            UpdateView();
        }

        private void UpdateView()
        {
            if (currentService == null)
                return;

            this.txName.Text = this.currentService.ChannelName;
            this.txDesp.Text = this.currentService.Description;
            this.txGenre.Text = (this.currentService.Genre == null) ? "" : this.currentService.Genre;
            this.cbFree.IsChecked = currentService.Free;
            this.ApplicationTitle.Text = this.ApplicationTitle.Text + "  ID = " + currentService.ID;
            this.PageTitle.Text = this.currentService.ChannelName;

            if (serviceExts == null)
                return;

            App.EngineInstance.GetUIDataModel.ReadServiceExtToDC(serviceExts);

            LstboxExt.ItemsSource = App.EngineInstance.GetUIDataModel.ServiceExtCollection;

            //LstboxExt.ItemContainerGenerator
            //LstboxExt.ItemsSource = App.ViewModel.ServiceExtCollection;  
            //LstboxExt.da
            //var d = this.LstboxExt.Items;
            //ListBoxItem item = LstboxExt.ItemContainerGenerator.ContainerFromIndex(LbSetting.SelectedIndex) as ListBoxItem;
            //LstboxExt.DataContext = curServiceExts;
            //LstboxExt.ItemsSource = curServiceExts;
            //MessageBox.Show(  d.First<string>()）；
        }

     
     
        private void LstboxExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.LstboxExt.SelectedIndex > -1)           
            {
                this.curServiceExtIdx = LstboxExt.SelectedIndex;
            }

            if (serviceExts != null && serviceExts.Count > 0)
            {
                // update view 
                this.txExtDesp.Text = this.serviceExts[curServiceExtIdx].Description;
                this.txExtURL.Text = this.serviceExts[curServiceExtIdx].Url;
            }

            // get the selected item and its DataContext
            ListBoxItem item = LstboxExt.ItemContainerGenerator.ContainerFromIndex(LstboxExt.SelectedIndex) as ListBoxItem;
            if (item != null)
            {
                DataModel_ChannelExt c = item.DataContext as DataModel_ChannelExt;
            }
        }

        private void btUpdate_Click(object sender, RoutedEventArgs e)
        {
            //1. collect service info
            currentService.ChannelName = txName.Text;
            currentService.Description = txDesp.Text;
            currentService.Genre = txGenre.Text;
            currentService.Free = (bool)cbFree.IsChecked;
        
            //2.
            bool re = App.EngineInstance.GetUIDataModel.UpdateServiceItem(currentService);
            string msg = re? "Update complete":"Update failed!";
            MessageBox.Show(msg);

            //3. refresh current view
            UpdateView();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            // test for SGDD and it's fragment
            DBEngine _dbEngine = App.EngineInstance.GetDBEngine;

            Table_SGDD t1 = new Table_SGDD(){  ID ="1", Version = 23};

            Table_SGDD_Fragment f1 = new Table_SGDD_Fragment() { FragmentID = "f1", SGDD_ID = "1" };

            Table_SGDD_Fragment f2 = new Table_SGDD_Fragment() { FragmentID = "f2", SGDD_ID = "1" };

            try
            {
                _dbEngine.CmtvDBContext.SGDD_items.InsertOnSubmit(t1);
                _dbEngine.CmtvDBContext.SGDD_Fragment_items.InsertOnSubmit(f1);
                _dbEngine.CmtvDBContext.SGDD_Fragment_items.InsertOnSubmit(f2);


                _dbEngine.CmtvDBContext.SubmitChanges();
            }
            catch (Exception e1)
            {

            }

            var sgdd = (from c in _dbEngine.CmtvDBContext.SGDD_items select c).First();

            var v2 = from c in _dbEngine.CmtvDBContext.SGDD_Fragment_items
                     where c.SGDD.ID == sgdd.ID
                     select c;

            //foreach (var v in v2)
            {
                _dbEngine.CmtvDBContext.SGDD_Fragment_items.DeleteAllOnSubmit(v2);
            }
            

            _dbEngine.CmtvDBContext.SGDD_items.DeleteOnSubmit(sgdd);
            _dbEngine.CmtvDBContext.SubmitChanges();



            //v.FirstOrDefault()

            //SaveContentData(new Table_Content() { ServiceID = "4", ID = "C2", ContentName = "朝闻天下", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            //SaveContentData(new Table_Content() { ServiceID = "1", ID = "C3", ContentName = "生活早参考", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            //SaveContentData(new Table_Content() { ServiceID = "4", ID = "C4", ContentName = "焦点访谈:用事实说话", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            //SaveContentData(new Table_Content() { ServiceID = "1", ID = "C5", ContentName = "国际艺苑", ContentClass = "Video", StartTime = DateTime.Now.AddHours(-2), EndTime = DateTime.Now });

            //SaveContentData(new Table_Content() { ServiceID = "2", ID = "C6", ContentName = "焦点访谈:", ContentClass = "Video", StartTime = DateTime.Now.AddMinutes(33), EndTime = DateTime.Now.AddHours(5) });
            //SaveContentData(new Table_Content() { ServiceID = "2", ID = "C7", ContentName = "说话", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) });
            //SaveContentData(new Table_Content() { ServiceID = "3", ID = "C8", ContentName = "睛彩天下", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now.AddMinutes(13) });

        }



        private void button2_Click(object sender, RoutedEventArgs e)
        {
            // preview data query test

            // save some content data 

            DBEngine _dbEngine = App.EngineInstance.GetDBEngine;

            var q = from p in _dbEngine.CmtvDBContext.PreviewData_items
                    from c in _dbEngine.CmtvDBContext.Content_items
                    where p.FragmentID == c.ID
                    select p;
            string str = "";
            foreach (var v in q)
            {

                str += "||" + v.Text + " " + v.ID;
                
            }
            MessageBox.Show(str);

            
        } 

    }
}