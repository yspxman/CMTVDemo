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
    public class CmmbStreamSource : MediaStreamSource, System.IDisposable
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
        private Thread m_WorkerThread;

        // this buffer holds the decoded streams
        // remember to call dispose after used
        BufferQueue<MSF.AudioBlock> m_abuffer = new BufferQueue<MSF.AudioBlock>();
        BufferQueue<MSF.VideoBlock> m_vbuffer = new BufferQueue<MSF.VideoBlock>();

        
        MSF.AudioBlock m_curAudioBlk;
        MSF.VideoBlock m_curVideoBlk;

        CmmbDemux m_demux2 = new CmmbDemux();


        static int threadid = 0;

        public CmmbStreamSource( int channelIdx, Engine engine)
        {
            Utility.Trace(String.Format("CmmbStreamSource channel is {0}", channelIdx));

            curChannelIdx = channelIdx;
            _engine = engine;
            m_MUXprovider = _engine.GetCmmbProvider;
            m_MUXprovider.ChannelID = channelIdx;
            m_MUXprovider.StartToProvide();
            m_MUXSourceQueue = _engine.GetCmmbStreamQueue;

            // Init the worker thread      
            m_WorkerThread = new Thread(WorkerThreadRun);
            m_WorkerThread.Start();
          
            m_WorkerThread.Name = "Worker Thread " + threadid++.ToString();

            Utility.Trace("Thread started! " + m_WorkerThread.Name);
        }


        private bool m_disposed = false;
        ~CmmbStreamSource()
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
                GC.SuppressFinalize(this);
            }
            m_disposed = true;
        }

        public void StopWorkerThread()
        {
            Utility.Trace(String.Format(" StopWorkerThread !! thread state is {0}", m_WorkerThread.ThreadState) );

            // do not push 'close' msg to the 'stopped' thread
            if (m_WorkerThread.ThreadState != ThreadState.Stopped)
            {
                m_MUXSourceQueue.ClearAndEnqueue(new WorkQueueElement(WorkQueueElement.Command.Close, null, 0, curChannelIdx));
            }
            
        }

        public void WorkerThreadRun()
        {
            while (true)
            {

                if (curChannelIdx != m_MUXprovider.ChannelID)
                {                   
                    m_abuffer.Enqueue(null);
                    m_vbuffer.Enqueue(null);

                    Utility.Trace("Thread forced exit, channel ID changed! --" + m_WorkerThread.Name);
                    // the end of the queue, terminate thread
                    return;
                }

                Utility.Trace(" m_MUXSourceQueue WaitForWorkItem " + m_WorkerThread.Name);
                m_MUXSourceQueue.WaitForWorkItem();

                WorkQueueElement elem = m_MUXSourceQueue.Dequeue();
                if (elem == null)
                    continue;

                Utility.Trace(String.Format(" m_MUXSourceQueue get an item, elem.ChannelId = {0},curChannelIdx = {1}  ", elem.ChannelID, curChannelIdx) + m_WorkerThread.Name);
                
                // then try to parse the stream
                var command = elem.CommandToPerform;
                
                if ((command == WorkQueueElement.Command.Close)
                    && (curChannelIdx == elem.ChannelID))
                {
                    m_abuffer.Enqueue(null);
                    m_vbuffer.Enqueue(null);
       
                    Utility.Trace("Thread normal exit --" + m_WorkerThread.Name);
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

                    m_abuffer.Enqueue(v[curChannelIdx].msf.audioBlock);
                    m_vbuffer.Enqueue(v[curChannelIdx].msf.videoBlock);                   
                }
            }
        }

        public void SetDemux(CmmbDemux d)
        {
            //demux = d;
        }

        protected override void CloseMedia()
        {
            // some cleanup here?
            Utility.Trace("!!!!CloseMedia!!!");
            //m_MUXSourceQueue.ClearAndEnqueue(new WorkQueueElement(WorkQueueElement.Command.Close, null));
            
            
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }



        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            MediaStreamSample mediaStreamSample = null;

            //if (mediaStreamType == MediaStreamType.Audio)
            //    mediaStreamSample = this.GetAudioSample();
            //else if (mediaStreamType == MediaStreamType.Video)
            //    mediaStreamSample = this.GetVideoSample();

            if (mediaStreamType == MediaStreamType.Audio)
                mediaStreamSample = this.GetAudioSample2();
            else if (mediaStreamType == MediaStreamType.Video)
                mediaStreamSample = this.GetVideoSample2();

            if (mediaStreamSample != null)
                this.ReportGetSampleCompleted(mediaStreamSample);
        }

        bool m_AmediaStreamEnd = false;
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

                m_curAudioBlk = m_abuffer.Dequeue();
                if (m_curAudioBlk != null)
                {
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
            int len = (int)m_curAudioBlk.aFrames[aIdx].Length -1;
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
                // free current video block
                m_curVideoBlk = null;

                Utility.Trace(" GetVideoSample,m_vbuffer WaitForWorkItem ");
                m_vbuffer.WaitForWorkItem();
                Utility.Trace(" GetVideoSample,m_vbuffer Got an Item ");

                m_curVideoBlk = m_vbuffer.Dequeue();

                if (m_curVideoBlk != null)
                {
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

        private void AdjustTimeStampOf1stSecond(MSF msf )
        {            
            int ifIdx =  msf.videoBlock.FirstIFrameIdx;
            if (ifIdx == 0)
                return;

            msf.videoBlock.FirstIFrameIdx = 0;
            long ts_base = msf.videoBlock.vFrames[ifIdx].RelativeTime;

            // Remove other non-I frame
            for (int i=0; i< ifIdx; i++)
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

            long ts_base_audio =0;
            int idx_audio =0;

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

            m_curVideoBlk = m_vbuffer.Dequeue();

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

            this.AudioBufferLength = 1000;
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


        #region Old Code


        MSF abuffer = null;
        long atimestamp_base;
        long atimestamp_offset;
        private MediaStreamSample GetAudioSample()
        {
            if (aIdx > (aNum - 1))
            {
                // next second
                abuffer = null;
                aNum = 0;
                aIdx = 0;
            }

            if (abuffer == null)
            {
                int i = -1; //第几秒数据？
                abuffer = m_demux2.GetCurrentBuffer(curChannelIdx, MediaStreamType.Audio, out i);
                if (abuffer != null)
                {
                    if (i == 1) //第一秒数据
                    {
                        // 因为播放器的timestamp必须从0 开始
                        atimestamp_base = abuffer.msfHeader.PlayStartTime;
                    }
                    aNum = (int)abuffer.audioBlock.AudioFrameNum;
                    atimestamp_offset = abuffer.msfHeader.PlayStartTime - atimestamp_base;
                }
                else
                {
                    MediaStreamSample sample = new MediaStreamSample(this.audioStreamDescription, null, 0, 0, 0, emptyDict);
                    return sample;
                }
            }

            MemoryStream stream = new MemoryStream();//abuffer.audioBlock.aFrames[aIdx]._as as MemoryStream;
            //LATM 封装，第一个字节是length, 跳过第一个字节, 相应的长度应该减1
            int len = (int)abuffer.audioBlock.aFrames[aIdx].Length - 1;
            stream.Write(abuffer.audioBlock.aFrames[aIdx]._data, 1, len);

            long timestamp = atimestamp_offset + abuffer.audioBlock.aFrames[aIdx].RelativeTime;  // 以100 纳秒为单位

            MediaStreamSample mediaStreamSample = new MediaStreamSample
                (
                this.audioStreamDescription,
                stream,
                0,
                len,
                timestamp,
                emptyDict
                );
            aIdx++;

            return mediaStreamSample;
        }


        MSF msfVideoBuffer = null;
        long timestamp_base;
        long timestamp_offset;
        private MediaStreamSample GetVideoSample()
        {
            if (vIdx == -1)
            {
                // the first frame requested,the first frame must be I frame,
                int i;
                if (msfVideoBuffer == null)
                    msfVideoBuffer = this.m_demux2.GetCurrentBuffer(curChannelIdx, MediaStreamType.Video, out i);

                vIdx = msfVideoBuffer.videoBlock.FirstIFrameIdx;
                fNum = (int)msfVideoBuffer.videoBlock.VideoFrameNum;

                // 因为播放器的timestamp必须从0 开始
                timestamp_base = msfVideoBuffer.msfHeader.PlayStartTime + msfVideoBuffer.videoBlock.vFrames[vIdx].RelativeTime;
                timestamp_offset = msfVideoBuffer.msfHeader.PlayStartTime - timestamp_base;
            }
            else if (vIdx > (fNum - 1))
            {
                // next second
                msfVideoBuffer = null;
                fNum = 0;
                vIdx = 0;
            }

            if (msfVideoBuffer == null)
            {
                int i = -1; //第几秒数据？
                msfVideoBuffer = m_demux2.GetCurrentBuffer(curChannelIdx, MediaStreamType.Video, out i);
                if (msfVideoBuffer != null)
                {
                    fNum = (int)msfVideoBuffer.videoBlock.VideoFrameNum;
                    timestamp_offset = msfVideoBuffer.msfHeader.PlayStartTime - timestamp_base;
                }
                else
                {
                    MediaStreamSample sample = new MediaStreamSample(this.videoStreamDescription, null, 0, 0, 0, emptyDict);
                    return sample;
                }
            }

            MemoryStream vStream = new MemoryStream();//msfVideoBuffer.videoBlock.vFrames[vIdx]._vs as MemoryStream;
            int len = (int)msfVideoBuffer.videoBlock.vFrames[vIdx].Length;
            vStream.Write(msfVideoBuffer.videoBlock.vFrames[vIdx]._data, 0, len);
            // 以100 纳秒为单位
            long timestamp = (timestamp_offset + msfVideoBuffer.videoBlock.vFrames[vIdx].RelativeTime);
            MediaStreamSample mediaStreamSample = new MediaStreamSample
                (
                this.videoStreamDescription,
                vStream,
                0,
                len,
                timestamp,
                emptyDict
                );
            vIdx++;

            return mediaStreamSample;
        }

        #endregion
    }
}
