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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using System.Threading;

namespace CMTVEngine
{
    public class CmmbStreamSource2 : MediaStreamSource, System.IDisposable
    {

        private MediaStreamDescription videoStreamDescription;
        private MediaStreamDescription audioStreamDescription;
        Dictionary<MediaSampleAttributeKeys, string> emptyDict = new Dictionary<MediaSampleAttributeKeys, string>();

        public double PlayerPosition { get; set; }

        private int curChannelIdx;

        // owned by engine
        private WorkQueue m_MUXSourceQueue;
        // owned by engine
        private MUXProvider m_MUXprovider;
        private Engine _engine;

        //this thread is used for demux cmmb frame
        private Thread m_ParserThread;

        private Thread m_WorkerThread;
        // this buffer holds the decoded streams
        // remember to call dispose after used
        BufferQueue<WorkQueueElement> m_abuffer = new BufferQueue<WorkQueueElement>();
        BufferQueue<WorkQueueElement> m_vbuffer = new BufferQueue<WorkQueueElement>();
        BufferQueue<WorkQueueElement> m_commands = new BufferQueue<WorkQueueElement>();
        ManualResetEvent m_aBufferFullEvent = new ManualResetEvent(false);
        ManualResetEvent m_vBufferFullEvent = new ManualResetEvent(false);
        int m_bufferDepth = 3;

        MSF.AudioBlock m_curAudioBlk;
        MSF.VideoBlock m_curVideoBlk;

        CmmbDemux m_demux2 = new CmmbDemux();


        static int threadid = 0;

        public CmmbStreamSource2(int channelIdx, Engine engine)
        {
            Utility.Trace(String.Format("CmmbStreamSource channel is {0}", channelIdx));

            curChannelIdx = channelIdx;
            _engine = engine;
            m_MUXprovider = _engine.GetCmmbProvider;
            m_MUXprovider.ChannelID = channelIdx;
            m_MUXprovider.StartToProvide();
            m_MUXSourceQueue = _engine.GetCmmbStreamQueue;

            // Init the worker thread      
            m_ParserThread = new Thread(ParserThreadRun);
            m_ParserThread.Start();

            m_ParserThread.Name = "Parser_Thread " + threadid++.ToString();

            Utility.Trace("Parser_Thread started! " + m_ParserThread.Name);
        }


        private bool m_disposed = false;
        ~CmmbStreamSource2()
        {
            Dispose();
        }

        // this dispose cannot be called outside, 
        public void Dispose()
        {
            if (!m_disposed)
            {
                Utility.Trace("CmmbStreamSource Dispose!!!!!! ");
                m_abuffer.Dispose();
                m_vbuffer.Dispose();
                
                m_aBufferFullEvent.Dispose();
                m_vBufferFullEvent.Dispose();
                m_commands.Dispose();

                GC.SuppressFinalize(this);
            }
            m_disposed = true;
        }

        public void StopParserAndWorkerThreads()
        {
            Utility.Trace(String.Format(" StopParserThread !! thread state is {0}", m_ParserThread.ThreadState));

            // do not push 'close' msg to the 'stopped' thread
            if (m_ParserThread.ThreadState != ThreadState.Stopped)
            {
                m_MUXSourceQueue.ClearAndEnqueue(new WorkQueueElement(WorkQueueElement.Command.Close, null, 0, curChannelIdx));
            }

            // do not push 'close' msg to the 'stopped' thread
            if (m_WorkerThread.ThreadState != ThreadState.Stopped)
                this.m_commands.ClearAndEnqueue(new WorkQueueElement(WorkQueueElement.Command.Close, null));

        }

        public void ParserThreadRun()
        {
            while (true)
            {
                if (curChannelIdx != m_MUXprovider.ChannelID)
                {
                    m_abuffer.Enqueue(null);
                    m_vbuffer.Enqueue(null);

                    Utility.Trace("Thread forced exit, channel ID changed! --" + m_ParserThread.Name);
                    // the end of the queue, terminate thread
                    return;
                }

                Utility.Trace(" m_MUXSourceQueue WaitForWorkItem " + m_ParserThread.Name);
                m_MUXSourceQueue.WaitForWorkItem();

                WorkQueueElement elem = m_MUXSourceQueue.Dequeue();
                if (elem == null)
                    continue;

                Utility.Trace(String.Format(" m_MUXSourceQueue get an item, elem.ChannelId = {0},curChannelIdx = {1}  ", elem.ChannelID, curChannelIdx) + m_ParserThread.Name);

                // then try to parse the stream
                var command = elem.CommandToPerform;

                if ((command == WorkQueueElement.Command.Close)
                    && (curChannelIdx == elem.ChannelID))
                {
                    m_abuffer.Enqueue(new WorkQueueElement(WorkQueueElement.Command.Data, null));
                    m_vbuffer.Enqueue(new WorkQueueElement(WorkQueueElement.Command.Data, null));

                    m_vBufferFullEvent.Set();
                    m_aBufferFullEvent.Set();
                    Utility.Trace("Thread normal exit --" + m_ParserThread.Name);
                    // the end of the queue, terminate thread
                    return;
                }

                if ((command == WorkQueueElement.Command.Sample)
                    && (curChannelIdx == elem.ChannelID))
                {
                    var v = m_demux2.ParseEntireMUX(elem.Stream);
                    if (elem.TimeIdx == 0)
                    {
                        // the first second
                        AdjustTimeStampOf1stSecond(v[curChannelIdx].msf);
                    }
                    AdjustTimeStampBase(v[curChannelIdx].msf, elem.TimeIdx);

                    m_abuffer.Enqueue(new WorkQueueElement(WorkQueueElement.Command.Data, v[curChannelIdx].msf.audioBlock));
                    m_vbuffer.Enqueue(new WorkQueueElement(WorkQueueElement.Command.Data, v[curChannelIdx].msf.videoBlock));

                    if (m_abuffer.Count() >= m_bufferDepth)
                        m_aBufferFullEvent.Set();

                    if (m_vbuffer.Count() >= m_bufferDepth)
                        m_vBufferFullEvent.Set();
                }
            }
        }


        public void WorkerThreadRun()
        {
            while (true)
            {
                //if ( m_AmediaStreamEnd)
                //{
                //    ReportGetSampleCompleted(new MediaStreamSample(this.audioStreamDescription, null, 0, 0, 0, emptyDict));
                //    Utility.Trace("Worker thread sent audio media end again ");
                //    continue;
                //}
                //if ( m_VmediaStreamEnd)
                //{
                //    ReportGetSampleCompleted(new MediaStreamSample(this.videoStreamDescription, null, 0, 0, 0, emptyDict));
                //    Utility.Trace("Worker thread sent video media end again ");
                //    continue;
                //}
                //if (m_AmediaStreamEnd && m_VmediaStreamEnd)
                //{
                //    ReportGetSampleCompleted(new MediaStreamSample(this.audioStreamDescription, null, 0, 0, 0, emptyDict));
                //    ReportGetSampleCompleted(new MediaStreamSample(this.videoStreamDescription, null, 0, 0, 0, emptyDict));
                //    Utility.Trace("Worker thread exit with both media end ");
                //    m_commands.Clear();
                //    return;
                //}


                Utility.Trace("Worker thread wait for command ");
                m_commands.WaitForWorkItem();
                Utility.Trace("Worker thread got an command ");

                WorkQueueElement elem = m_commands.Dequeue();
                
                if (elem.CommandToPerform == WorkQueueElement.Command.Close)
                {
                    Utility.Trace("Worker thread exit with close event ");
                    m_commands.Clear();
                    return;
                }

                MediaStreamType mediaStreamType = (MediaStreamType)elem.CommandParameter;

                //if (m_AmediaStreamEnd && m_VmediaStreamEnd)
                //{
                //    Utility.Trace("Worker thread exit with both media end ");
                //    m_aBufferFullEvent.Dispose();
                //    m_vBufferFullEvent.Dispose();
                //    m_commands.Clear();
                //    m_commands.Dispose();
                //    return;
                //}

                //if ((mediaStreamType == MediaStreamType.Audio) && m_AmediaStreamEnd)
                //{  
                //        ReportGetSampleCompleted(new MediaStreamSample(this.audioStreamDescription, null, 0, 0, 0, emptyDict));
                //        Utility.Trace("Worker thread sent audio media end again ");
                //        continue;
                //}
                //if ((mediaStreamType == MediaStreamType.Video) && m_VmediaStreamEnd)
                //{
                //    ReportGetSampleCompleted(new MediaStreamSample(this.videoStreamDescription, null, 0, 0, 0, emptyDict));
                //    Utility.Trace("Worker thread sent video media end again ");
                //    continue;
                //}


                if (elem.CommandToPerform == WorkQueueElement.Command.Sample)
                {
                    // to check the buffer depth
                    if (mediaStreamType == MediaStreamType.Audio)
                    {
                        if (m_abuffer.Count() == 0 )
                        {
                            if (m_ParserThread.ThreadState != ThreadState.Stopped)
                            {
                                ReportGetSampleProgress(0.5f);
                                Utility.Trace("Worker thread ReportGetSampleProgress sent for Audio, and wait for buffer full");                                
                                m_aBufferFullEvent.WaitOne();
                                Utility.Trace("Worker thread audio buffer full");
                            }
                            else
                                Utility.Trace("m_ParserThread has been stopped!, so keep going");
                            ReportGetSampleCompleted(GetAudioSample2());
                        }
                        else
                        {
                           ReportGetSampleCompleted( GetAudioSample2());
                        }
                    }
                    else if (mediaStreamType == MediaStreamType.Video)
                    {
                        if (m_vbuffer.Count() == 0)
                        {
                            if (m_ParserThread.ThreadState != ThreadState.Stopped)
                            {
                                ReportGetSampleProgress(0.5f);
                                Utility.Trace("Worker thread ReportGetSampleProgress sent for Video ");
                                m_vBufferFullEvent.WaitOne();
                                Utility.Trace("Worker thread video buffer full");
                            }
                            else
                                Utility.Trace("m_ParserThread has been stopped!, so keep going");
                            
                            ReportGetSampleCompleted(GetVideoSample2());
                        }
                        else
                        {
                            ReportGetSampleCompleted(GetVideoSample2());
                        }
                    }
                }
            }
        }

        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            //start the worker thread
            if (m_WorkerThread == null)
            {
                m_WorkerThread = new Thread(WorkerThreadRun);
                m_WorkerThread.Start();
            }

            m_commands.Enqueue(new WorkQueueElement(WorkQueueElement.Command.Sample, mediaStreamType));
        }

#if DEBUG
        bool m_AmediaStreamEnd = true;
#else
        bool m_AmediaStreamEnd = false;
#endif
       
        int aNum = 0;
        int aIdx = 0;
        private MediaStreamSample GetAudioSample2()
        {
            MediaStreamSample sample = new MediaStreamSample(this.audioStreamDescription, null, 0, 0, 0, emptyDict);

            if (m_AmediaStreamEnd)
                return sample;

            if (aIdx > (aNum - 1))
            {
                // free current video block
                m_curAudioBlk = null;
                m_abuffer.WaitForWorkItem();

                m_curAudioBlk = (MSF.AudioBlock)m_abuffer.Dequeue().CommandParameter ;
                if (m_curAudioBlk != null)
                {
                    if (m_abuffer.Count() == 0)
                        m_aBufferFullEvent.Reset();

                    aIdx = 0;
                    aNum = (int)m_curAudioBlk.AudioFrameNum;
                }
                else
                {
                    m_AmediaStreamEnd = true;
                    return sample;
                }
            }

            MemoryStream aStream = new MemoryStream();
            //LATM 封装，第一个字节是length, 跳过第一个字节, 相应的长度应该减1
            int len = (int)m_curAudioBlk.aFrames[aIdx].Length - 1;
            aStream.Write(m_curAudioBlk.aFrames[aIdx]._data, 1, len);

            MediaStreamSample mediaStreamSample = new MediaStreamSample
                (
                this.audioStreamDescription,
                aStream,
                0,
                len,
                m_curAudioBlk.aFrames[aIdx].RelativeTime,   //// 以100 纳秒为单位
                emptyDict
                );
            aIdx++;  

            return mediaStreamSample;
        }

        bool m_VmediaStreamEnd = false;
        int fNum = 0;
        int vIdx = 0;
        private MediaStreamSample GetVideoSample2()
        {
            //Utility.Trace(" GetVideoSample2 ");

            MediaStreamSample sample = new MediaStreamSample(this.videoStreamDescription, null, 0, 0, 0, emptyDict);
            if (m_VmediaStreamEnd)
            {
                Utility.Trace(" GetVideoSample2 NULL stream has been sent!");
                return sample;
            }
            if (vIdx > (fNum - 1))
            {
                // free current video block and request for next video block
                m_curVideoBlk = null;

                Utility.Trace(" GetVideoSample,m_vbuffer WaitForWorkItem ");
                m_vbuffer.WaitForWorkItem();
                Utility.Trace(" GetVideoSample,m_vbuffer Got an Item ");

                m_curVideoBlk = (MSF.VideoBlock)m_vbuffer.Dequeue().CommandParameter;

                if (m_curVideoBlk != null)
                {
                    Utility.Trace(String.Format(" GetVideoSample,m_vbuffer.count = {0} ", m_vbuffer.Count()));

                    if (m_vbuffer.Count() == 0)
                        m_vBufferFullEvent.Reset();

                    vIdx = 0;
                    fNum = (int)m_curVideoBlk.VideoFrameNum;
                }
                else
                {
                    m_VmediaStreamEnd = true;
                    Utility.Trace(" GetVideoSample2 NULL stream has been sent!");
                    return sample;
                }

            }


            MemoryStream vStream = new MemoryStream();
            int len = (int)m_curVideoBlk.vFrames[vIdx].Length;
            vStream.Write(m_curVideoBlk.vFrames[vIdx]._data, 0, len);

            MediaStreamSample mediaStreamSample = new MediaStreamSample
                (
                this.videoStreamDescription,
                vStream,
                0,
                len,
                m_curVideoBlk.vFrames[vIdx].RelativeTime,   //// 以100 纳秒为单位
                emptyDict
                );
            vIdx++;

            return mediaStreamSample;
        }


        long FistTSBase = 0;

        private void AdjustTimeStampBase(MSF msf, int idx)
        {
            if (idx > 0)
            {
                msf.msfHeader.PlayStartTime = msf.msfHeader.PlayStartTime - FistTSBase;
                foreach (var v in msf.audioBlock.aFrames)
                {
                    v.RelativeTime += msf.msfHeader.PlayStartTime;
                }
                foreach (var v in msf.videoBlock.vFrames)
                {
                    v.RelativeTime += msf.msfHeader.PlayStartTime;
                }
            }
            else if (idx == 0)
            {

                long offset = msf.videoBlock.vFrames[1].RelativeTime - msf.videoBlock.vFrames[0].RelativeTime;
                FistTSBase += msf.videoBlock.vFrames[(int)msf.videoBlock.VideoFrameNum - 1].RelativeTime + offset;
                FistTSBase += msf.msfHeader.PlayStartTime;
                msf.msfHeader.PlayStartTime = 0;
            }
        }

        private void AdjustTimeStampOf1stSecond(MSF msf)
        {
            int ifIdx = msf.videoBlock.FirstIFrameIdx;
            if (ifIdx == 0)
                return;

            msf.videoBlock.FirstIFrameIdx = 0;
            long ts_base = msf.videoBlock.vFrames[ifIdx].RelativeTime;

            // Remove other non-I frame
            for (int i = 0; i < ifIdx; i++)
            {
                msf.videoBlock.vFrames.RemoveAt(0);
            }
            msf.videoBlock.VideoFrameNum = msf.videoBlock.VideoFrameNum - (uint)ifIdx;

            foreach (var v in msf.videoBlock.vFrames)
            {
                v.RelativeTime -= ts_base;
            }

            // 处理audio， 使其同步
            // 首先查找和Video I 帧大概同步的audio 帧
            long offset = msf.audioBlock.aFrames[1].RelativeTime - msf.audioBlock.aFrames[0].RelativeTime;

            long ts_base_audio = 0;
            int idx_audio = 0;

            for (int m = 0; m < msf.audioBlock.AudioFrameNum; m++)
            {
                if (Math.Abs(msf.audioBlock.aFrames[m].RelativeTime - ts_base) < offset)
                {
                    ts_base_audio = msf.audioBlock.aFrames[m].RelativeTime;
                    idx_audio = m;
                    break;
                }
            }

            // remove other frames;
            for (int i = 0; i < idx_audio; i++)
            {
                msf.audioBlock.aFrames.RemoveAt(0);
            }
            msf.audioBlock.AudioFrameNum -= (uint)idx_audio;

            foreach (var v in msf.audioBlock.aFrames)
            {
                v.RelativeTime -= ts_base_audio;
            }
        }

        protected override void OpenMediaAsync()
        {
            //WaveFormatEx
            HeAacWaveFormat aacf = new HeAacWaveFormat();
            WaveFormatExtensible wfx = new WaveFormatExtensible();
            aacf.WaveFormatExtensible = wfx;

            aacf.WaveFormatExtensible.FormatTag = 0x1610; //0xFF;//0x1610;
            aacf.WaveFormatExtensible.Channels = 2; //
            aacf.WaveFormatExtensible.BlockAlign = 1;
            aacf.WaveFormatExtensible.BitsPerSample = 0;//16; //unkonw set to 0
            aacf.WaveFormatExtensible.SamplesPerSec = 24000; //  from 8000 to 96000 Hz
            aacf.WaveFormatExtensible.AverageBytesPerSecond = 0;//wfx.SamplesPerSec * wfx.Channels * wfx.BitsPerSample / wfx.BlockAlign;
            aacf.WaveFormatExtensible.Size = 12;

            // Extra 3 words in WAVEFORMATEX
            // refer to http://msdn.microsoft.com/en-us/library/windows/desktop/dd757806(v=vs.85).aspx
            aacf.wPayloadType = 0x0; //Audio Data Transport Stream (ADTS). The stream contains an adts_sequence, as defined by MPEG-2.
            aacf.wAudioProfileLevelIndication = 0xFE;
            aacf.wStructType = 0;

            string codecPrivateData = aacf.ToHexString();

            Dictionary<MediaStreamAttributeKeys, string> audioStreamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            audioStreamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = codecPrivateData;
            audioStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, audioStreamAttributes);


            m_vbuffer.WaitForWorkItem();

            m_curVideoBlk = m_vbuffer.Dequeue().CommandParameter as MSF.VideoBlock;

            if (m_curVideoBlk == null)
                return;
            vIdx = 0;
            fNum = (int)m_curVideoBlk.VideoFrameNum;


            H264NalFormat h264f = new H264NalFormat();
            h264f.sps = m_curVideoBlk.FirstIFrameInfo.sps;
            h264f.pps = m_curVideoBlk.FirstIFrameInfo.pps;
            string s = h264f.ToHexString();

            //Video
            Dictionary<MediaStreamAttributeKeys, string> videoStreamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            videoStreamAttributes[MediaStreamAttributeKeys.VideoFourCC] = "H264";
            videoStreamAttributes[MediaStreamAttributeKeys.Height] = "240";
            videoStreamAttributes[MediaStreamAttributeKeys.Width] = "320";
            videoStreamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = s;//"0000016742E00D96520283F40500000168CE388000";
            videoStreamDescription = new MediaStreamDescription(MediaStreamType.Video, videoStreamAttributes);

            //Media
            Dictionary<MediaSourceAttributesKeys, string> mediaSourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            mediaSourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.FromSeconds(6).Ticks.ToString(CultureInfo.InvariantCulture);
            mediaSourceAttributes[MediaSourceAttributesKeys.CanSeek] = "0";

            List<MediaStreamDescription> mediaStreamDescriptions = new List<MediaStreamDescription>();

#if !DEBUG
            // Emulator does not support HE-AAC
           mediaStreamDescriptions.Add(audioStreamDescription);
#endif

            mediaStreamDescriptions.Add(videoStreamDescription);

            this.AudioBufferLength = 500;
            this.ReportOpenMediaCompleted(mediaSourceAttributes, mediaStreamDescriptions);
        }

        protected override void SeekAsync(long seekToTime)
        {
            this.ReportSeekCompleted(seekToTime);
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            throw new NotImplementedException();
        }
        protected override void CloseMedia()
        {
            // some cleanup here?
            Utility.Trace("!!!!CloseMedia!!!");
            // do not push 'close' msg to the 'stopped' thread
            if (m_WorkerThread.ThreadState != ThreadState.Stopped)
                this.m_commands.ClearAndEnqueue(new WorkQueueElement(WorkQueueElement.Command.Close, null));
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }
    }
}
