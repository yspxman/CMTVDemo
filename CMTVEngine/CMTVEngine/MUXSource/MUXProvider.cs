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
using System.Windows.Threading;
using System.IO;
using System.Windows.Resources;


namespace CMTVEngine
{
    public class MUXProvider : System.IDisposable
    {
        
        MemoryStream s1 = new MemoryStream();
        MemoryStream s2 = new MemoryStream();
        MemoryStream s3 = new MemoryStream();
        MemoryStream s4 = new MemoryStream();
        MemoryStream s5 = new MemoryStream();
        MemoryStream s6 = new MemoryStream();

        Timer m_timer;
        int m_sec = 0;

        
        private WorkQueue m_workQueue;

        public int ChannelID { get; set; }

        public MUXProvider(WorkQueue queue)
        {
            m_workQueue = queue;
            m_timer = new Timer(new TimerCallback(timer_Tick));

            // read resources and devide it into seconds.
            StreamResourceInfo streaminfo = Application.GetResourceStream(new Uri("Resources/cmmb.mfs", UriKind.Relative));
            var cmmb_stream = streaminfo.Stream;
            Preprocess(cmmb_stream);
            cmmb_stream.Close();
        }

        public void Dispose()
        {
            s1.Close();
            s2.Close();
            s3.Close();
            s4.Close();
            s5.Close();
            s6.Close();
            m_timer.Dispose();
        }

        public void StartToProvide()
        {
            Utility.Trace("MUXProvider.StartToProvide");

            m_sec = 0;
            //m_workQueue.Clear();
            // 1 second
            m_timer.Change(0, 700);
        }

        public void CancelProviding()
        {
            Utility.Trace("MUXProvider.CancelProviding");
            m_timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        private void Preprocess(Stream s)
        {
            int num = 492480;
            // 第一秒数据

            byte[] tmp = new byte[num];

            s.Seek(0, SeekOrigin.Begin);
            s.Read(tmp, 0, num);
            s1.Write(tmp, 0, num);

            s.Read(tmp, 0, num);
            s2.Write(tmp, 0, num);

            s.Read(tmp, 0, num);
            s3.Write(tmp, 0, num);

            s.Read(tmp, 0, num);
            s4.Write(tmp, 0, num);

            s.Read(tmp, 0, num);
            s5.Write(tmp, 0, num);

            s.Read(tmp, 0, num);
            s6.Write(tmp, 0, num);
        }

        private void timer_Tick(object sender)
        {
            Utility.Trace(String.Format("MUXProvider.timer_Tick second {0}", m_sec));

            if (m_sec > 5)
            {
                // stop the timer
                m_timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

                WorkQueueElement element = new WorkQueueElement(WorkQueueElement.Command.Close, null, 0, ChannelID);
                m_workQueue.Enqueue(element);

                Utility.Trace("MUXProvider.timer_Tick stopped!");
            }
            else 
            {
                Stream tmp = null;
                switch (m_sec)
                {
                    case 0:
                        tmp = s1;
                        break;
                    case 1:
                        tmp = s2;
                        break;
                    case 2:
                        tmp = s3;
                        break;
                    case 3:
                        tmp = s4;
                        break;
                    case 4:
                        tmp = s5;
                        break;
                    case 5:
                        tmp = s6;
                        break;
                    default:
                        break;                   
                }
                WorkQueueElement element = new WorkQueueElement(
                                                             WorkQueueElement.Command.Sample,
                                                             tmp,
                                                             m_sec,
                                                             ChannelID
                                                             );
                m_workQueue.Enqueue(element);
            }
            m_sec++;          
        }
    }
}
