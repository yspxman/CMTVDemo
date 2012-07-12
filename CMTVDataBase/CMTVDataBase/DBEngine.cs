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
using System.Data.Linq.Mapping;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;



namespace CMTVDataBase
{
    public class CmtvDBDataContex : DataContext
    {
        public const string ConnnectionStr = "Data Source=isostore:/cmtvDB.sdf";
        public Table<Table_Service> Service_items
        {
            get { return this.GetTable<Table_Service>(); }
        }
        public Table<Table_Service_Extention> Servicext_items
        {
            get { return this.GetTable<Table_Service_Extention>(); }
        }

        public Table<Table_Content> Content_items
        {
            get { return this.GetTable<Table_Content>(); }
        }

        public Table<Table_SGDD> SGDD_items
        {
            get { return this.GetTable<Table_SGDD>(); }
        }

        public Table<Table_SGDD_Fragment> SGDD_Fragment_items
        {
            get { return this.GetTable<Table_SGDD_Fragment>(); }
        }


        public Table<Table_PreviewData> PreviewData_items
        {
            get { return this.GetTable<Table_PreviewData>(); }
        }

        public Table<Table_PurchaseChannel> PurchaseChannel_items
        {
            get { return this.GetTable<Table_PurchaseChannel>(); }
        }
        public Table<Table_PurchaseData> PurchaseDatal_items
        {
            get { return this.GetTable<Table_PurchaseData>(); }
        }
        public Table<Table_PurchaseItem> PurchaseItem_items
        {
            get { return this.GetTable<Table_PurchaseItem>(); }
        }

        public CmtvDBDataContex()
            : base(ConnnectionStr)
        {
        }
    }

    public class DBEngine
    {
        //数据库定义
        private CmtvDBDataContex _dbContext;
        public CmtvDBDataContex CmtvDBContext
        {
            get { return _dbContext; }
        }

        public DBEngine()
        {
            _dbContext = new CmtvDBDataContex();
            if (!_dbContext.DatabaseExists())
            {
                _dbContext.CreateDatabase();
                //SaveTestDataToDB();
            }
        }
    
    }
}
