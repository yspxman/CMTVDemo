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
using System.Threading;
using System.Windows.Resources;
using System.Collections.Generic;
using CMTVDataBase;

namespace CMTVEngine
{

    public class Engine : System.IDisposable
    {
        private DBEngine _dbEngine;
        private UIDataModel _uiDataModel;
        
        private MUXProvider _provider;
        private WorkQueue _cmmbStreamQueue;




        public DBEngine GetDBEngine
        {
            get { return _dbEngine; }
            //private set;
        }
        public UIDataModel GetUIDataModel
        {
            get { return _uiDataModel; }
            //private set;
        }


        public MUXProvider GetCmmbProvider
        {
            get { return _provider; }
            //private set;
        }

        public WorkQueue GetCmmbStreamQueue
        {
            get { return _cmmbStreamQueue; }
        }

        public Engine()
        {
        }

        ~Engine()
        {
            _cmmbStreamQueue.Dispose();
        }

        private bool m_disposed = false;
        
        public void Dispose()
        {
            if (!m_disposed)
            {
                if (_provider != null)
                {
                    _provider.CancelProviding();
                    _provider.Dispose();
                }
                if (_cmmbStreamQueue != null)
                {
                    _cmmbStreamQueue.Clear();
                    _cmmbStreamQueue.Dispose();
                }
            }
            m_disposed = true;
            GC.SuppressFinalize(this);
        }

        public void Initialize()
        {
            //1. initialize the database, 
            _dbEngine = new DBEngine();

            //2. init the MBBMS
            // MBBMSEngine.init();

            //3. init the main view data model
            _uiDataModel = new UIDataModel(this);

            // 4.init the cmmb demux
            //StreamResourceInfo streaminfo = Application.GetResourceStream(new Uri("Resources/cmmb.mfs", UriKind.Relative));
            //var cmmb_stream = streaminfo.Stream;
            //_demux = new CmmbDemux(streaminfo.Stream);
            //cmmb_stream.Close();

            //5. init cmmb stream provider
            _cmmbStreamQueue = new WorkQueue();
            _provider = new MUXProvider(_cmmbStreamQueue);
        }

        private DataModel_Channel m_curPlayingChannel;

        public void PrepareToPlayChannel(DataModel_Channel dc)
        {
            m_curPlayingChannel = dc;

            _provider.CancelProviding();
           // _provider.StartToProvide();
            //_provider.
        }

        public string GetCurPlayingChannelName 
        {
            get
            {
                if (m_curPlayingChannel != null)
                    return m_curPlayingChannel.ChannelName;
                else
                    return "";
            }
        }
        public string GetCurPlayingChannelID
        {
            get
            {
                if (m_curPlayingChannel != null)
                    return m_curPlayingChannel.ID;
                else
                    return "";
            }
        }

        public void PrepareToPlayNextChannel()
        {

            IEnumerator<DataModel_Channel> enumerator = _uiDataModel.DC_AllChannels.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.ID == m_curPlayingChannel.ID)
                {
                    if (enumerator.MoveNext())
                        m_curPlayingChannel = enumerator.Current;
                    else
                    {
                        // reach the end, back to the first
                        enumerator.Reset();
                        while (enumerator.MoveNext())
                        {
                            m_curPlayingChannel = enumerator.Current;
                            break;
                        }
                    }
                    break;
                }
            }

            _provider.CancelProviding();            
        }
                    









        // end of the file

    }
}
