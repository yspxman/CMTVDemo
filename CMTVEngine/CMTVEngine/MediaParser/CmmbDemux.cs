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
using System.IO;
using System.Collections.Generic;

namespace CMTVEngine
{
    public class MultiplexFrameHeader
    {
        //该header 相对于stram 的offset
        public ulong startAddr;
        // 1 byte , 不含CRC_32 (32 bits)
        // header 的总长度应该是headerLength + 4
        public uint headerLength;
        public uint MD_ID;
        public uint subFrameNum;
        public int[] subFrameLength;
    }

    //持续业务复用配置表
    public class ContinualServiceConfigTable
    {
        public uint mpFameNum;
        public List<MUXConfigTable> mfConfigTables;
    }

    // 复用帧配置表， 每个复用帧的信息
    public class MUXConfigTable
    {
        // 以下变量的含义查看广播信道物理层规范
        public uint RSCodec;
        public uint LDPC;
        public uint mode;
        public uint TS_num;
        public uint Length;
    }

    // 数据复用帧
    public class MUX
    {
        public MultiplexFrameHeader muxHeader;
        public MSF msf;
        public Stream stream;
    }

    // 复用子帧，从复用帧1开始有复用子帧
    public class MSF  //MultiplexSubFrame
    {
        public class VideoBlock
        {
            public class VideoFrame
            {
                public uint Length;
                public long RelativeTime;
                //0是I帧，1是P帧
                public uint FrameMode;
                // 相对该复用帧的地址
                public uint RelativeStartAddr; 
                //public Stream _vs;
                public byte[] _data;
            }
            public class NAL_SPS_PPS_SEI
            {
                public byte[] sps;
                public byte[] pps;
                public byte[] sei;
            }

            //默认-1， 表明该复用子帧中没有I帧
            public int FirstIFrameIdx = -1;
            public NAL_SPS_PPS_SEI FirstIFrameInfo = new NAL_SPS_PPS_SEI();

            public uint VideoBlockHeaderLength;
            public uint VideoFrameNum;
            public List<VideoFrame> vFrames = new List<VideoFrame>();
        }

        public class AudioBlock
        {
            public class AudioFrame
            {
                public uint Length;
                public long RelativeTime;
                
                public uint RelativeStartAddr;  
                //public Stream _as;
                public byte[] _data;
            }
            public uint AudioBlockHeaderLength;
            public uint AudioFrameNum;
            public List<AudioFrame> aFrames = new List<AudioFrame>();
        }

        public MSFHeader msfHeader; 
        public VideoBlock videoBlock;
        public AudioBlock audioBlock; 
    }

    // 复用子帧头
    public class MSFHeader //MultiplexSubFrameHeader
    {
        public uint Length;
        public uint VideoBlockIndicator;
        public uint AudioBlockIndicator;
        public uint DataBlockIndicator;
        public uint ExtBlockIndicator;

        public long PlayStartTime;

        public uint VideoBlockLenth;
        public uint VideoBlockNum;

        public uint AudioBlockLenth;
        public uint AudioBlockNum;
    }
    
    public class CmmbDemux
    {
        MemoryStream s1;
        MemoryStream s2;
        MemoryStream s3;
        MemoryStream s4;
        MemoryStream s5;
        MemoryStream s6;

        //复用帧头的起始码， 4字节
        static readonly byte[] frameStartCode = new byte[] { 0x00, 0x00, 0x00, 0x01 };
        static readonly ushort CRC_Size = 4; // 4 bytes

        //每个unit 是 1/22500 秒, 
        //media element 接受的timestamp是 100 ns = (1/10^7)s
        //把这里的unit单位设置成100 ns，那么 timeUint = (1/22500) * 10^7 = 444
        static readonly uint timeUint = 444; 

       
        ContinualServiceConfigTable _msConfigTable;
        public List<MUX> _muxs1;
        public List<MUX> _muxs2;
        public List<MUX> _muxs3;
        public List<MUX> _muxs4;
        public List<MUX> _muxs5;
        public List<MUX> _muxs6;
       

        int idx = 0;
        int aidx = 0;
        int vidx = 0;
        
        public void ResetBufferIdx()
        {
             idx = 0;
             aidx = 0;
             vidx = 0;
        }

        public MSF GetCurrentBuffer(int channel,MediaStreamType mediatype, out int f)
        {
            MSF msf;
            f = 0;

            if (mediatype == MediaStreamType.Audio)            
                idx = aidx;
            else if (mediatype == MediaStreamType.Video)            
                idx = vidx;
            
            switch (idx)
            {
                case 0:
                    msf = _muxs1[channel].msf;
                    f = 1;
                    break;
                case 1:
                    msf = _muxs2[channel].msf;          
                    break;
                case 2:
                    msf = _muxs3[channel].msf;               
                    break;
                case 3:
                    msf = _muxs4[channel].msf;                 
                    break;
                case 4:
                    msf = _muxs5[channel].msf;        
                    break;
                case 5:
                    msf = _muxs6[channel].msf;                    
                    break;
                default:
                    msf = null;                  
                    break;
            }
            idx++;

            if (mediatype == MediaStreamType.Audio)
                 aidx = idx;
            else if (mediatype == MediaStreamType.Video)             
                 vidx = idx;
            
            return msf;
        }

        public MSF GetCurrentBuffer2(int channel, out bool fistSecond, out Stream s)
        {
            MSF msf;
            fistSecond = false;
            switch (idx)
            {
                case 0:
                    msf = _muxs1[channel].msf;
                    s = _muxs1[channel].stream;
                    fistSecond = true;
                    break;
                case 1:
                    msf = _muxs2[channel].msf;
                    s = _muxs2[channel].stream;
                    break;
                case 2:
                    msf = _muxs3[channel].msf;
                    s = _muxs3[channel].stream;
                    break;
                case 3:
                    msf = _muxs4[channel].msf;
                    s = _muxs4[channel].stream;
                    break;
                case 4:
                    msf = _muxs5[channel].msf;
                    s = _muxs5[channel].stream;
                    break;
                case 5:
                    msf = _muxs6[channel].msf;
                    s = _muxs6[channel].stream;
                    break;
                default:
                    msf = null;
                    s = null;
                    break;
            }
            idx++;
            return msf;
        }

        public CmmbDemux()
        { 
        }

        public CmmbDemux(Stream s)
        {
            s1 = new MemoryStream();
            s2 = new MemoryStream();
            s3 = new MemoryStream();
            s4 = new MemoryStream();
            s5 = new MemoryStream();
            s6 = new MemoryStream();

            Preprocess(s);


            Stream currentStream = s1;
            _muxs1 = ParseEntireMUX(currentStream);
            currentStream.Close();


             currentStream = s2;
            _muxs2 = ParseEntireMUX(currentStream);
            currentStream.Close();

             currentStream = s3;
            _muxs3 = ParseEntireMUX(currentStream);
            currentStream.Close();

             currentStream = s4;
            _muxs4 = ParseEntireMUX(currentStream);
            currentStream.Close();

             currentStream = s5;
            _muxs5 = ParseEntireMUX(currentStream);
            currentStream.Close();

            currentStream = s6;
            _muxs6 = ParseEntireMUX(currentStream);
            currentStream.Close();
        }

        public void Preprocess(Stream s)
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

        public List<MUX> ParseEntireMUX(Stream currentStream)
        {
            List<MUX> muxs = new List<MUX>();
            MultiplexFrameHeader _mf1ConfigHeader = ParseMultiplexFrameHeader(0, currentStream);

            // 解析 控制帧
            int offset = SearchServiceMultiplexConfigTable(currentStream, _mf1ConfigHeader);
            if (offset != -1)
                ParseServiceMultiplexConfigTable(offset, currentStream);

            // fill the MF length
            foreach (var v in _msConfigTable.mfConfigTables)
            {
                uint len = MFLengthLookupTable(v);
                v.Length = len;
            }

            // 初始化结束，开始解视频帧
            uint offset2 = _msConfigTable.mfConfigTables[0].Length + _msConfigTable.mfConfigTables[1].Length;
            for (int i = 2; i < _msConfigTable.mpFameNum; i++)
            {
                uint offset3 = offset2;
                MUX mux = new MUX();
                mux.stream = currentStream;
                // 从第二个复用帧开始
                mux.muxHeader = ParseMultiplexFrameHeader((int)offset3, currentStream);
                offset3 += mux.muxHeader.headerLength + CRC_Size;

                mux.msf = new MSF();
                mux.msf.msfHeader = ParseMSFHeader((int)offset3, currentStream);

                // 解析视频段’
                offset3 += mux.msf.msfHeader.Length + CRC_Size;
                mux.msf.videoBlock = ParseVideoBlock((int)offset3, currentStream);
                //TODO 解析 音频段
                offset3 += mux.msf.msfHeader.VideoBlockLenth;
                mux.msf.audioBlock = ParseAudioBlock((int)offset3, currentStream);
     
                // the next
                muxs.Add(mux);
                offset2 += _msConfigTable.mfConfigTables[i].Length;
            }
            return muxs;
        }

        public MSF.AudioBlock ParseAudioBlock(int offset, Stream _stream)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            byte[] tmp = new byte[4];
            MSF.AudioBlock ab = new MSF.AudioBlock();
            _stream.Read(tmp, 0, 1);
            ab.AudioFrameNum = (uint)BitTools.MaskBits(tmp, 0, 8);
            ab.AudioBlockHeaderLength = ab.AudioFrameNum * 5 +1;

            uint addr = (uint)offset + ab.AudioBlockHeaderLength + CRC_Size;
            for (int i = 0; i < ab.AudioFrameNum; i++)
            {
                //读音频参数集
                MSF.AudioBlock.AudioFrame f = new MSF.AudioBlock.AudioFrame();
                _stream.Read(tmp, 0, 2);
                f.RelativeStartAddr = addr;
                f.Length = (uint)BitTools.MaskBits(tmp, 0, 16);
                // skip 音频流编号 和 保留
                _stream.Seek(1, SeekOrigin.Current);

                // 读相对时间
                _stream.Read(tmp, 0, 2);
                f.RelativeTime = BitTools.MaskBits(tmp, 0, 16) * timeUint;

                // copy 音频流
                //f._as = BitTools.CopyToNewStream(_stream, (long)f.RelativeStartAddr, (int)f.Length);
                BitTools.CopyFromStreamToBytesArray(_stream, (long)f.RelativeStartAddr, (int)f.Length, ref f._data);

                ab.aFrames.Add(f);
                //下一段音频地址
                addr += f.Length;              
            }
            return ab;
        }

  
        public MSF.VideoBlock ParseVideoBlock(int offset, Stream _stream)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            byte[] tmp = new byte[4];
            MSF.VideoBlock vb = new MSF.VideoBlock();

            _stream.Read(tmp, 0, 2);
            vb.VideoBlockHeaderLength = (uint)BitTools.MaskBits(tmp, 0, 12);

            //计算有多少个视频帧, 每个视频参数集 5byte
            vb.VideoFrameNum = vb.VideoBlockHeaderLength / 5;

            uint addr = (uint)offset + vb.VideoBlockHeaderLength + CRC_Size;
            for (int i = 0; i < vb.VideoFrameNum; i++)
            {
                MSF.VideoBlock.VideoFrame f = new MSF.VideoBlock.VideoFrame();
                //本段视频的开始地址
                f.RelativeStartAddr = addr;
                
                // 视频段长度
                _stream.Read(tmp, 0, 2);
                f.Length = (uint)BitTools.MaskBits(tmp, 0, 16);
    
                // 帧类型，I帧 or P帧
                _stream.Read(tmp, 0, 1);
                f.FrameMode = (uint)BitTools.MaskBits(tmp, 0, 3);
                if (vb.FirstIFrameIdx == -1 && f.FrameMode ==0)
                {
                    vb.FirstIFrameIdx = i;
                }
                // 相对播放时间
                if (BitTools.MaskBits(tmp, 7, 1) == 1)  //相对时间提示
                { 
                    _stream.Read(tmp, 0, 2);
                    f.RelativeTime = BitTools.MaskBits(tmp, 0, 16) * timeUint;
                }

                // 本段视频流                
                //f._vs = BitTools.CopyToNewStream(_stream, (long)f.RelativeStartAddr, (int)f.Length);
                BitTools.CopyFromStreamToBytesArray(_stream, (long)f.RelativeStartAddr, (int)f.Length, ref f._data);

                vb.vFrames.Add(f);
                //下一段视频的开始地址
                addr += f.Length;
            }

            ParseNALHeader(vb);

            return vb;       
        }

        public bool ParseNALHeader(MSF.VideoBlock vb)
        {
            // Need to get the SPS and PPS of the stream, the first frame must be I frame, other frames cannot be played.
            if (vb.FirstIFrameIdx == -1)
                return false;

            //有I帧
            byte[] tmp = new byte[32];
            byte[] NALStartCode = new byte[] { 0x00, 0x00, 0x01 };
            List<byte[]> nals = new List<byte[]>();


            Stream s = new MemoryStream(); //vb.vFrames[vb.FirstIFrameIdx]._vs;
            s.Write(vb.vFrames[vb.FirstIFrameIdx]._data, 0, 
                     (int)vb.vFrames[vb.FirstIFrameIdx].Length);

            s.Seek(0, SeekOrigin.Begin);
            s.Read(tmp, 0, 3);

            if (BitTools.FindBytePattern(tmp, NALStartCode) != 0)
                return false;

            while (true)
            {
                long pos = s.Position;
                s.Read(tmp, 0, 32);
                int nextNALidx = BitTools.FindBytePattern(tmp, NALStartCode);

                if (nextNALidx == -1)
                    break;
                byte[] nal = new byte[nextNALidx];
                BitTools.CopyToBytesArray(tmp, nal, nextNALidx);
                nals.Add(nal);

                // rollback pos
                pos += nextNALidx + 3; //跳过下一个startcode
                s.Seek(pos, SeekOrigin.Begin);
            }
            s.Close();

            vb.FirstIFrameInfo = new MSF.VideoBlock.NAL_SPS_PPS_SEI();

            foreach (var v in nals)
            {
                switch (v[0])
                {
                    case 0x67:  //SPS http://www.eefocus.com/czzheng/blog/11-09/230262_65ff0.html
                        vb.FirstIFrameInfo.sps = new byte[v.Length - 1];
                        BitTools.CopyToBytesArray(v, 1, vb.FirstIFrameInfo.sps, v.Length - 1);
                        break;
                    case 0x68:  //PPS
                        vb.FirstIFrameInfo.pps = new byte[v.Length - 1];
                        BitTools.CopyToBytesArray(v, 1, vb.FirstIFrameInfo.pps, v.Length - 1);
                        break;
                    case 0x06:  // SEI
                        vb.FirstIFrameInfo.sei = new byte[v.Length - 1];
                        BitTools.CopyToBytesArray(v, 1, vb.FirstIFrameInfo.sei, v.Length - 1);
                        break;
                    default:
                        break;
                }
            }
            return true;
        }
       

        public MSFHeader ParseMSFHeader(int offset, Stream _stream)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            byte[] tmp = new byte[4];
            MSFHeader msfHeader = new MSFHeader();

            _stream.Read(tmp, 0, 1);
            msfHeader.Length = tmp[0];

            _stream.Read(tmp, 0, 1);
            msfHeader.VideoBlockIndicator = (uint)BitTools.MaskBits(tmp, 1, 1);
            msfHeader.AudioBlockIndicator = (uint)BitTools.MaskBits(tmp, 2, 1);
            msfHeader.DataBlockIndicator = (uint)BitTools.MaskBits(tmp, 3, 1);
            msfHeader.ExtBlockIndicator = (uint)BitTools.MaskBits(tmp, 4, 1);

            // 起始播放时间
            if (BitTools.MaskBits(tmp, 0, 1) == 1)
            {
                _stream.Read(tmp, 0, 4);
                msfHeader.PlayStartTime = BitTools.MaskBits(tmp, 0, 32) * timeUint ;
            }
            // 视频段信息
            if (msfHeader.VideoBlockIndicator == 1)
            {
                _stream.Read(tmp, 0, 3);
                msfHeader.VideoBlockLenth = (uint)BitTools.MaskBits(tmp, 0, 21);
                msfHeader.VideoBlockNum = (uint)BitTools.MaskBits(tmp, 21, 3);
            }

            //音频段信息
            if (msfHeader.AudioBlockIndicator == 1)
            {
                _stream.Read(tmp, 0, 3);
                msfHeader.AudioBlockLenth = (uint)BitTools.MaskBits(tmp, 0, 21);
                msfHeader.AudioBlockNum = (uint)BitTools.MaskBits(tmp, 21, 3);
            }

            if (msfHeader.DataBlockIndicator == 1)
                _stream.Seek(3, SeekOrigin.Current);

            if (msfHeader.ExtBlockIndicator == 1)
                _stream.Seek(3, SeekOrigin.Current);

            // 跳过视频和音频参数集
            return msfHeader;
        }


        public MultiplexFrameHeader ParseMultiplexFrameHeader(int offset, Stream _stream)
        {
            _stream.Seek(offset, SeekOrigin.Begin);

            byte[] tmp = new byte[4];
            int r = _stream.Read(tmp, 0, 4); // read the start code,
            if (r !=4)
                return null;  

            if (BitTools.FindBytePattern(tmp, frameStartCode) != 0)
                return null; // start code not match, return

            MultiplexFrameHeader mfHeader = new MultiplexFrameHeader();
            mfHeader.startAddr = 0x0;
            // parse frame header length 1 byte.
            _stream.Read(tmp, 0, 1);
            mfHeader.headerLength = tmp[0];

            // 再读两个字节，协议版本号5 bit, 最低版本号5 bit, 复用帧标示 6 bit
            _stream.Read(tmp, 0, 2);
            mfHeader.MD_ID = (uint)BitTools.MaskBits(tmp, 10, 6);

            // 跳过4个字节， 包括从紧急广播提示-ESG更新序列号
            _stream.Seek(4, SeekOrigin.Current);

            // 读一个字节，取出 subFrame number (4 bit)
            _stream.Read(tmp, 0, 1);
            mfHeader.subFrameNum = (uint)BitTools.MaskBits(tmp, 4, 4);

            //读取子帧长度 （3 byte）
            mfHeader.subFrameLength = new int[mfHeader.subFrameNum];

            for (int i = 0; i < mfHeader.subFrameNum; i++)
            {
                _stream.Read(tmp, 0, 3);
                mfHeader.subFrameLength[i] = BitTools.MaskBits(tmp, 0, 24);
            }
            return mfHeader;
        }

        public int SearchServiceMultiplexConfigTable(Stream s, MultiplexFrameHeader _mf1ConfigHeader)
        {
            //定位持续业务复用配置表
            int MFHeaderOffset = 0;
            byte[] tmp = new byte[4];

            //非业务复用帧
            if (_mf1ConfigHeader.MD_ID != 0)
                return -1;

            //复用帧头长度
            MFHeaderOffset = (int)_mf1ConfigHeader.headerLength + CRC_Size;
            s.Seek(MFHeaderOffset, SeekOrigin.Begin);

            int offset = 0;
            bool re = false;
            //读取每个控制表的标示号（1byte），寻找 持续业务复用配置表
            for (int i = 0; i < _mf1ConfigHeader.subFrameNum; i++)
            {
                s.Read(tmp, 0, 1);
                s.Seek(-1, SeekOrigin.Current);

                if (tmp[0] == 0x02)
                {                  
                    re = true;
                    break;
                }
                else
                {
                    offset += _mf1ConfigHeader.subFrameLength[i];
                    s.Seek(offset, SeekOrigin.Current);
                }
            }
            if (re)
                return offset + MFHeaderOffset;
            else
                return -1;
        }


        public uint MFLengthLookupTable(MUXConfigTable c)
        {         
            if (c.LDPC == 0
                && c.RSCodec == 1
                && c.mode == 1)
            {
                return 129024 * c.TS_num / 8;
            }

            if (c.LDPC == 0
                && c.RSCodec == 0
                && c.mode == 0)
            {
                // MF 长度
                return 69120 * c.TS_num / 8;
            }

            return 0;
        }

        public void ParseServiceMultiplexConfigTable(int offset, Stream _stream)
        {
            // 开始解析复用配置表
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Seek(3, SeekOrigin.Current);

            // 读取Multiplex frame number, (6 bit)
            byte[] tmp = new byte[4];
            _stream.Read(tmp, 0, 1);
            _msConfigTable = new ContinualServiceConfigTable();
            _msConfigTable.mpFameNum = (uint)BitTools.MaskBits(tmp, 2, 6);
            _msConfigTable.mfConfigTables = new List<MUXConfigTable>();

            for (int i = 0; i < _msConfigTable.mpFameNum; i++)
            {
                //读取每个MF的信息配置表，RS, LDPC, mode
                MUXConfigTable mftmp = new MUXConfigTable();

                _stream.Read(tmp, 0, 1);
                mftmp.RSCodec = (uint)BitTools.MaskBits(tmp, 6, 2);
                _stream.Read(tmp, 0, 2);

                mftmp.LDPC = (uint)BitTools.MaskBits(tmp, 2, 2);
                mftmp.mode = (uint)BitTools.MaskBits(tmp, 4, 2);
                mftmp.TS_num = (uint)BitTools.MaskBits(tmp, 10, 6);

                _msConfigTable.mfConfigTables.Add(mftmp);

                for (int j = 0; j < mftmp.TS_num; j++)
                {
                    // skip 时隙号 和 保留，1 byte
                    _stream.Seek(1, SeekOrigin.Current);
                }
                
                //读复用子帧数
                _stream.Read(tmp, 0, 1);
                int subnumber = BitTools.MaskBits(tmp, 4, 4);

                for (int m = 0; m < subnumber; m++)
                {
                    // skip 子帧号，。。。3 bytes
                    _stream.Seek(3, SeekOrigin.Current);
                }
            }
            // skip CRC_32
            _stream.Seek(CRC_Size, SeekOrigin.Current);
        }
    }
}
