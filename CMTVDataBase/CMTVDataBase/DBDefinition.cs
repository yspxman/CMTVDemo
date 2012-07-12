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
using System.Data.Linq.Mapping;

namespace CMTVDataBase
{

    #region Service table
    [Table]
    //Service也就是频道，channel
    //Service 分片用于描述手机电视业务的频道的信息，一个Service分片可以用来提供一个电视频道（例如CCTV-5）的信息。
    //一个频道可以包含多个节目（一个Service分片可以被多个Content分片所引用）。
    public class Table_Service
    {
        //频道ID (1)
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

        // 和extention的 约束关系
        private EntitySet<Table_Service_Extention> _extensions;
        [Association(Storage = "_extensions", OtherKey = "ServiceID", ThisKey = "ID")]
        public EntitySet<Table_Service_Extention> Extentions
        {
            get { return _extensions; }
            set { this._extensions.Assign(value); }
        }

        // 和content的 约束关系
        private EntitySet<Table_Content> _contents;
        [Association(Storage = "_contents", OtherKey = "ServiceID", ThisKey = "ID")]
        public EntitySet<Table_Content> Contents
        {
            get { return _contents; }
            set { this._contents.Assign(value); }
        }

        //Service 的版本号 (1)
        [Column(CanBeNull = false)]
        public int Version { get; set; }

        // Global ID (0-1)
        [Column]
        public string GlobalServiceID { get; set; }

        //(0-1)该频道相对于其他频道展现给用户的顺序。该值提供了一种频道列表顺序的组织方法。
        [Column]
        public int Weight { get; set; }

        // Free? (0-1)
        [Column]
        public bool ForFree { get; set; }

        //servicetype   (0-n) format:Json 
        [Column]
        public string ServiceType { get; set; }

        // service name (1-n) format:Json , 注意，可能该字段附带 lang 属性
        [Column(CanBeNull = false)]
        public string ServiceName { get; set; }

        // Service _description (0-n) format:Json 

        [Column]
        public string Description { get; set; }

        // Service Genre  (0-n) format:Json 
        [Column]
        public string Genre { get; set; }

        // Service Provider  (0-1) format:Json (Lang, content)
        [Column]
        public string Provider { get; set; }

        // CADescriptor  (0-1) 
        [Column]
        public string CADescriptor { get; set; }

        [Column]
        public bool IsFavorite { get; set; }

        /// <summary>
        ///  entity set 初始化
        /// </summary>
        public Table_Service()
        {
            this._extensions = new EntitySet<Table_Service_Extention>(
                new Action<Table_Service_Extention>(this.ExtensionAdd),
                 new Action<Table_Service_Extention>(this.ExtensionRemove)

                 );

            this._contents = new EntitySet<Table_Content>(
                                 new Action<Table_Content>(this.ContentAdd),
                 new Action<Table_Content>(this.ContentRemove)
                );

        }

        private void ExtensionAdd(Table_Service_Extention a)
        {
            a.Service = this;
        }
        private void ExtensionRemove(Table_Service_Extention a)
        {
            a.Service = null;
        }

        private void ContentAdd(Table_Content a)
        {
            a.Service = this;
        }
        private void ContentRemove(Table_Content a)
        {
            a.Service = null;
        }
    }

    [Table]
    public class Table_Service_Extention
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int ID { get; set; }

        [Column]
        public string ServiceID { get; set; }

        // _services
        private EntityRef<Table_Service> _services;
        [Association(Storage = "_services", ThisKey = "ServiceID", OtherKey = "ID", IsForeignKey = true)]
        public Table_Service Service
        {
            get { return _services.Entity; }
            set { ServiceID = value.ID; }
        }

        // Service Extension  (0-1)
        [Column]
        public string ExtensionDescription { get; set; }

        // URL (1) 
        [Column]
        public string ExtensionURL { get; set; }
    }


    #endregion

    #region Table Content

    [Table]
    public class Table_Content
    {
        //Content ID (1)
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

        [Column]
        public string ServiceID { get; set; }

        // Foreign Key service ID
        private EntityRef<Table_Service> _services;
        [Association(Storage = "_services", ThisKey = "ServiceID", OtherKey = "ID", IsForeignKey = true)]
        public Table_Service Service
        {
            get { return _services.Entity; }
            set { ServiceID = (value != null) ? value.ID : null; }
        }

        //版本号 (1)
        [Column(CanBeNull = false)]
        public int Version { get; set; }

        // Global ID (0-1)
        [Column]
        public string GlobalContentID { get; set; }

        // Free? (0-1)
        [Column]
        public bool ForFree { get; set; }

        //Live? (0-1)
        [Column]
        public bool Live { get; set; }

        //Repeat? (0-1)
        [Column]
        public bool Repeat { get; set; }

        //Keyword  (0-n) Json 封装
        [Column]
        public string Keyword { get; set; }

        //ContentClass  (1-n) Json 封装
        // “text”、“image”、“audio”、“video
        [Column(CanBeNull = false)]
        public string ContentClass { get; set; }

        //  name (1-n) format:Json , 注意，可能该字段附带 lang 属性
        [Column(CanBeNull = false)]
        public string ContentName { get; set; }

        //  _description (0-n) format:Json 
        [Column]
        public string Description { get; set; }

        [Column]
        public DateTime StartTime { get; set; }

        [Column]
        public DateTime EndTime { get; set; }

        [Column]
        public string AudioLanguage { get; set; }

        [Column]
        public string TextLanguage { get; set; }

        // Content Genre  (0-n) format:Json 
        [Column]
        public string Genre { get; set; }

        // CADescriptor  (0-1) 
        [Column]
        public string CADescriptor { get; set; }

        //TODO to be continued ... PreviewDataReference PrivateExt  
    }

    #endregion

    #region Table SGDD
    [Table] //Service Guide Delivery Descriptor
    public class Table_SGDD
    {
        //SGDD ID (1)
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

        // 和_fragments的 约束关系
        private EntitySet<Table_SGDD_Fragment> _fragments;
        [Association(Storage = "_fragments", OtherKey = "SGDD_ID", ThisKey = "ID")]
        public EntitySet<Table_SGDD_Fragment> Fragments
        {
            get { return _fragments; }
            set { this._fragments.Assign(value); }
        }

        //版本号 (1)
        [Column]
        public int Version { get; set; }


        [Column]
        public string AlternativeAccessURL { get; set; }


        public Table_SGDD()
        {
            this._fragments = new EntitySet<Table_SGDD_Fragment>
                 (
                   new Action<Table_SGDD_Fragment>(this.Add),
                   new Action<Table_SGDD_Fragment>(this.Remove)
                 );
        }

        private void Add(Table_SGDD_Fragment a)
        {
            a.SGDD = this;
        }
        private void Remove(Table_SGDD_Fragment a)
        {
            a.SGDD = null;
        }
    }


    [Table] //Service Guide Delivery Descriptor
    public class Table_SGDD_Fragment
    {
        //Fragment ID (1), equal to service id or content id or other id, 
        //depends on the fragment type
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string FragmentID { get; set; }

        [Column]
        public string SGDD_ID { get; set; }

        //Association with SGDD table
        private EntityRef<Table_SGDD> _sgdd;
        [Association(Storage = "_sgdd", ThisKey = "SGDD_ID", OtherKey = "ID", IsForeignKey = true)]


        public Table_SGDD SGDD
        {
            get { return _sgdd.Entity; }
            set 
            {
                SGDD_ID = (value != null) ? value.ID : null;
            }
        }
        
        //Transport ID (0-1)
        [Column]
        public uint TransportID { get; set; }

        [Column]
        public uint Version { get; set; }

        [Column]
        public uint Encoding { get; set; }

        [Column]
        public uint Type { get; set; }
    }

    #endregion



    // PreviewData 和 各个 fragment 是多对多关系
    [Table]
    public class Table_PreviewData
    {
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

         // This fragment id maybe refer to service id, content id ...
        [Column(CanBeNull=false)]
        public string FragmentID { get; set; }

        [Column]
        public uint Version { get; set; }

        [Column]
        public String PictureURI { get; set; }

        [Column]
        public String MIMEType { get; set; }

        [Column]
        public String Text { get; set; }

        [Column]
        public uint PictureData { get; set; }
    }

    #region Puchase information

    [Table]
    public class Table_PurchaseItem
    {
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

        [Column]
        public uint Version { get; set; }
        
        [Column]
        public string Global_ID { get; set; }

        // (0-n) json format
        [Column]
        public string Service_IDs { get; set; }

        // (0-n) json format
        [Column]
        public string Content_IDs { get; set; }


        [Column]
        public String Name { get; set; }

        [Column]
        public String Description { get; set; }

        //暂时省略 extension 和 PrivateExt
    }


    // 订购渠道
    [Table]
    public class Table_PurchaseChannel
    {
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

        [Column]
        public uint Version { get; set; }

        // (0-n) json format
        [Column]
        public String PurchaseURL { get; set; }

        [Column]
        public String Name { get; set; }

        [Column]
        public String Description { get; set; }

        //暂时省略 extension 和 PrivateExt
    }
    
    //资费信息等等
    [Table]
    public class Table_PurchaseData
    {
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string ID { get; set; }

        [Column]
        public uint Version { get; set; }

        /*
        128：节目按次
        129：频道包月
        130：套餐（包含多于一个的包月频道）
         */
        [Column]
        public uint SubscriptionType { get; set; }

        [Column]
        public float Price { get; set; }

 
        [Column]
        public string PurchaseItemID { get; set; }

        // (0-n) json
        [Column]  
        public string PurchaseChannelIDs { get; set; }

        //暂时省略 extension 和 PrivateExt
    }

    #endregion  //Purchase information

}
