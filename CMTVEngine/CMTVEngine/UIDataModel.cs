using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Data.Linq;
using System.Linq;
using System.Windows.Data;
using System.Data.Linq.Mapping;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;


using CMTVDataBase;
namespace CMTVEngine
{
    // Definitions of data collections for UI view
    public class DataModel_Channel
    {
        public string ChannelName { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public bool Free { get; set; }
        public string Genre { get; set; }
        public bool IsFavorite { get; set; }

        // current playing content
        public string CurrentContentName { get; set; }
        public DateTime CurrentContentStartTime { get; set; }
        public DateTime CurrentContentEndTime { get; set; }
        public string CurrentContentTimeDuration
        {
            get
            {
                return CurrentContentStartTime.ToShortTimeString() + " - " + CurrentContentEndTime.ToShortTimeString();
            }
        }        
    }


    public class DataModel_ChannelExt
    {
        public string Url { get; set; }
        public string Description { get; set; }
        public int ID { get; set; }
    }

    public class DataModel_Program
    {
        public string ID { get; set; }
        public string ProgramName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Free { get; set; }
        public string TimeDuration
        {
            get
            {
                return StartTime.ToShortTimeString() + " - " + EndTime.ToShortTimeString();
            }
            //   private set;
        }
    }


    public class UIDataModel
    {
        private Engine _engine;
        private DBEngine _dbEngine;



        public ObservableCollection<DataModel_Channel> DC_AllChannels
        { get; private set; }

        public ObservableCollection<DataModel_Channel> DC_Favorite
        { get; private set; }

        public ObservableCollection<DataModel_Program> DC_ProgramLst
        { get; private set; }


        // used for ext setting test
        public ObservableCollection<DataModel_ChannelExt> ServiceExtCollection
        { get; private set; }

        public UIDataModel(Engine e)
        {
            _engine = e;
            _dbEngine = e.GetDBEngine;

            this.DC_AllChannels = new ObservableCollection<DataModel_Channel>();
            this.ServiceExtCollection = new ObservableCollection<DataModel_ChannelExt>();
            this.DC_ProgramLst = new ObservableCollection<DataModel_Program>();
            this.DC_Favorite = new ObservableCollection<DataModel_Channel>();

            SaveTestDataToDB();
        }


        public void SaveTestDataToDB()
        {
            // 1. add service items
            SaveServiceData(new Table_Service() { ID = "S1", ServiceName = "CCTV-1", IsFavorite = true, Description = "test1" });
            SaveServiceData(new Table_Service() { ID = "S2", ServiceName = "CCTV-2", IsFavorite = false, Description = "一个频道可包括若干个节目" });
            SaveServiceData(new Table_Service() { ID = "S3", ServiceName = "CCTV-3", IsFavorite = true, Description = "该分片的版本号。新版本的分片可以从接收到的时候开始" });
            SaveServiceData(new Table_Service() { ID = "S4", ServiceName = "CCTV-5", IsFavorite = false, Description = "节目唯一对应于一个频道（一个content分片只" });
            SaveServiceData(new Table_Service() { ID = "S5", ServiceName = "CCTV-6", IsFavorite = true, Description = "节目唯一对应于一个频道（一个content分片只" });
            SaveServiceData(new Table_Service() { ID = "S6", ServiceName = "CCTV-Child", IsFavorite = false, Description = "节目唯一对应于一个频道（一个content分片只" });
            SaveServiceData(new Table_Service() { ID = "S7", ServiceName = "CCTV-7", IsFavorite = true, Description = "节目唯一对应于一个频道（一个content分片只" });

            // 2. add service extention items
            SaveServiceExtData(new Table_Service_Extention() { ServiceID = "S1", ExtensionURL = "url:xxx.com", ExtensionDescription = "service id =1" });
            SaveServiceExtData(new Table_Service_Extention() { ServiceID = "S1", ExtensionURL = "url:4364564.com", ExtensionDescription = "service id =1" });
            SaveServiceExtData(new Table_Service_Extention() { ServiceID = "S2", ExtensionURL = "url:2.com", ExtensionDescription = "service id =2" });

            //3. add content      
            SaveContentData(new Table_Content() { ServiceID = "S1", ID = "C1", ContentName = "天下音乐：睛彩天下", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            SaveContentData(new Table_Content() { ServiceID = "S4", ID = "C2", ContentName = "朝闻天下", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            SaveContentData(new Table_Content() { ServiceID = "S1", ID = "C3", ContentName = "生活早参考", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            SaveContentData(new Table_Content() { ServiceID = "S4", ID = "C4", ContentName = "焦点访谈:用事实说话", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now });
            SaveContentData(new Table_Content() { ServiceID = "S1", ID = "C5", ContentName = "国际艺苑", ContentClass = "Video", StartTime = DateTime.Now.AddHours(-2), EndTime = DateTime.Now });

            SaveContentData(new Table_Content() { ServiceID = "S2", ID = "C6", ContentName = "焦点访谈:", ContentClass = "Video", StartTime = DateTime.Now.AddMinutes(33), EndTime = DateTime.Now.AddHours(5) });
            SaveContentData(new Table_Content() { ServiceID = "S2", ID = "C7", ContentName = "说话", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) });
            SaveContentData(new Table_Content() { ServiceID = "S3", ID = "C8", ContentName = "睛彩天下", ContentClass = "Video", StartTime = DateTime.Now, EndTime = DateTime.Now.AddMinutes(13) });


            // 4. save some previewdata
            Table_PreviewData p1 = new Table_PreviewData() { ID = "P1", FragmentID = "C1", Text = "previewdata1" };
            Table_PreviewData p2 = new Table_PreviewData() { ID = "P2", FragmentID = "C1", Text = "previewdata2" };
            Table_PreviewData p3 = new Table_PreviewData() { ID = "P3", FragmentID = "S2", Text = "previewdata3" };
            Table_PreviewData p4 = new Table_PreviewData() { ID = "P4", FragmentID = "S2", Text = "previewdata4" };

            try
            {
                _dbEngine.CmtvDBContext.PreviewData_items.InsertOnSubmit(p1);
                _dbEngine.CmtvDBContext.PreviewData_items.InsertOnSubmit(p2);
                _dbEngine.CmtvDBContext.PreviewData_items.InsertOnSubmit(p3);
                _dbEngine.CmtvDBContext.PreviewData_items.InsertOnSubmit(p4);
                _dbEngine.CmtvDBContext.SubmitChanges();
            }
            catch { }


            // purchase info

            Table_PurchaseItem pi1 = new Table_PurchaseItem() { ID = "Pi1", Service_IDs = "S1,S2", Name = "手机电视全网套餐1", Description = "这是手机电视全网套餐" };
            Table_PurchaseItem pi2 = new Table_PurchaseItem() { ID = "Pi2", Service_IDs = "S2,S3,S4",  Name = "手机电视全网套餐2", Description = "这是手机电视全网套餐" };

            _dbEngine.CmtvDBContext.PurchaseItem_items.InsertOnSubmit(pi1);
            _dbEngine.CmtvDBContext.PurchaseItem_items.InsertOnSubmit(pi2);
            _dbEngine.CmtvDBContext.PurchaseChannel_items.InsertOnSubmit(new Table_PurchaseChannel() { ID = "PC1", Name = "purchasechannel", PurchaseURL = "http://naf.mbbms.chinamobile.com:18000" });
            _dbEngine.CmtvDBContext.PurchaseDatal_items.InsertOnSubmit(new Table_PurchaseData() {  ID = "PD1", PurchaseItemID = "Pi1", Price= (float)5.0, SubscriptionType = 129, PurchaseChannelIDs = "PC1"});
            _dbEngine.CmtvDBContext.PurchaseDatal_items.InsertOnSubmit(new Table_PurchaseData() { ID = "PD2", PurchaseItemID = "Pi2", Price = (float)4.0, SubscriptionType = 130, PurchaseChannelIDs = "PC1" });

            try
            {
                _dbEngine.CmtvDBContext.SubmitChanges();
            }
            catch (Exception e){}
        }

        public bool SaveContentData(Table_Content item)
        {
            try
            {
                _dbEngine.CmtvDBContext.Content_items.InsertOnSubmit(item);
                _dbEngine.CmtvDBContext.SubmitChanges();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool SaveServiceExtData(Table_Service_Extention item)
        {
            try
            {
                _dbEngine.CmtvDBContext.Servicext_items.InsertOnSubmit(item);
                _dbEngine.CmtvDBContext.SubmitChanges();
            }
            catch (Exception) 
            {
                return false;
            }
            return true;
        }


        public void QueryTest()
        {
            //EnumerableQuery
            //EnumerableRowCollection

            //1. 试着做一个 查询
            var dataset = from c in _dbEngine.CmtvDBContext.Service_items where c.ID == "1" select c;
            List<Table_Service> serviceList = dataset.ToList();

            //2. 查询ext中所有的items
            var dataset2 = from c in _dbEngine.CmtvDBContext.Servicext_items select c;

            //3. 从两张表中查1个表的结果，查询service name 是CCTV-1的所有service 的ext， 返回的结果集是一个匿名的新对象的集合
            var dataset3 = from c in _dbEngine.CmtvDBContext.Servicext_items
                           where c.Service.ServiceName == "CCTV-1"  // 这里用到了EntityRef
                           select new               // Create a  new anonymous type
                           {
                               c.ServiceID,
                               c.Service.ServiceName,   // 可以通过、entityRef来取 另一张表的内容
                               c.ExtensionURL
                           };

            //4. 取出第一个数据，在update 和 delete中有用
            var dataset4 = _dbEngine.CmtvDBContext.Service_items.First(c => c.ServiceName == "CCTV-2");

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("services:");
            foreach (/*Service_ExtentionTable service in list*/ var v in dataset3)
            {
                messageBuilder.AppendLine(v.ServiceID + "  " + v.ServiceName + "  " + v.ExtensionURL);
            }
            MessageBox.Show(messageBuilder.ToString());
        }

        public List<DataModel_Channel> QueryServiceFromID(string id)
        {
            //1. 试着做一个 查询

            IQueryable<DataModel_Channel> dataset = from c in _dbEngine.CmtvDBContext.Service_items
                                                           where c.ID == id
                                                           select new DataModel_Channel()
                                                           {
                                                               ChannelName = c.ServiceName,
                                                               Description = c.Description,
                                                               Free = c.ForFree,
                                                               Genre = c.Genre,
                                                               ID = c.ID
                                                           };

            // 直到调用 ToList ，dataset 才会被实例化，查询才真正执行
            return dataset.ToList<DataModel_Channel>();
        }



        public List<DataModel_ChannelExt> QueryServiceExtFromID(string id)
        {
            var dataset = from c in _dbEngine.CmtvDBContext.Servicext_items
                          where c.Service.ID == id  // 这里用到了EntityRef
                          select new DataModel_ChannelExt             // Create a  new anonymous type
                          {
                              Description = c.ExtensionDescription,
                              Url = c.ExtensionURL,
                              ID = c.ID
                          };

            return dataset.ToList<DataModel_ChannelExt>();
        }



        public List<DataModel_Program> QueryContentFromServiceID(string id)
        {
            var dataset = from c in _dbEngine.CmtvDBContext.Content_items
                          where c.Service.ID == id  // 这里用到了EntityRef
                          select new DataModel_Program             // Create a  new anonymous type
                          {
                              ID = c.ID,
                              ProgramName = c.ContentName,
                              StartTime = c.StartTime,
                              EndTime = c.EndTime,
                              Free = c.ForFree
                          };

            return dataset.ToList<DataModel_Program>();
        }


        public ObservableCollection<DataModel_Program> ReadProgramsToDCFromChannelID(string id)
        {
            // clear the legacy programs.
            DC_ProgramLst.Clear();

            var re = QueryContentFromServiceID(id);
            foreach (var v in re)
            {
                this.DC_ProgramLst.Add(v);
            }
            return DC_ProgramLst;
        }


        public bool UpdateServiceItem(DataModel_Channel data)
        {
            // 1. find the item in DB to update
            var item = from c in _dbEngine.CmtvDBContext.Service_items where c.ID == data.ID select c;

            Table_Service t = item.FirstOrDefault();

            // 2. assign values
            t.ServiceName = data.ChannelName;
            t.Description = data.Description;
            t.ForFree = data.Free;
            t.Genre = data.Genre;

            // 3. submit changes
            try
            {
                _dbEngine.CmtvDBContext.SubmitChanges();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool SaveServiceData(Table_Service item)
        {
            try
            {
                //1. 把数据加入到本地Collection中， 这样绑定DataContext的控件就可以直接得到数据了 
                //this.ServiceCollection.Add(item);
                // 2. 把数据加到数据库中                
                _dbEngine.CmtvDBContext.Service_items.InsertOnSubmit(item);
                _dbEngine.CmtvDBContext.SubmitChanges();
            }

            catch (Exception)
            {
                return false;
            }
            return true;
        }


       // private bool isAddedToCollection = false;

        public void ReadFavoriteChannelsToDC()
        {
            this.DC_Favorite.Clear();
            //IEnumerator<DataModel_Channel> enumerator = DC_AllChannels.GetEnumerator();
            List<DataModel_Channel> list = DC_AllChannels.ToList();

            foreach (var v in list)
            {
                if (v.IsFavorite)
                {
                    DC_Favorite.Add(v);
                }
            }
 
        }

        public void ReadAllChannelsToDC()
        {
            //if (!isAddedToCollection)

            // refresh the view everytime
            this.DC_AllChannels.Clear();
            IEnumerator<Table_Service> enumerator = _dbEngine.CmtvDBContext.Service_items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                DataModel_Channel d = new DataModel_Channel
                {
                    ChannelName = enumerator.Current.ServiceName,
                    Description = enumerator.Current.Description,
                    ID = enumerator.Current.ID,
                    Genre = enumerator.Current.Genre,
                    Free = enumerator.Current.ForFree,
                    IsFavorite = enumerator.Current.IsFavorite
                };
                // get the content from service ID
                var contents = QueryContentFromServiceID(enumerator.Current.ID);

                // only pickup the first item
                if (contents.Count > 0)
                {
                    d.CurrentContentName = contents.FirstOrDefault().ProgramName;
                    d.CurrentContentStartTime = contents.FirstOrDefault().StartTime;
                    d.CurrentContentEndTime = contents.FirstOrDefault().EndTime;
                }

                this.DC_AllChannels.Add(d);
            }
        }

        public void ReadServiceExtToDC(List<DataModel_ChannelExt> d)
        {
            this.ServiceExtCollection.Clear();
            foreach (var v in d)
            {
                this.ServiceExtCollection.Add(v);
            }
        }

    }
}
