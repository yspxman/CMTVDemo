using System;
using System.IO;

namespace CMTVEngine
{
    /// <summary>
    /// An individual element stored in our work queue. Describes the type of work
    /// to perform.
    /// </summary>
    public class WorkQueueElement
    {
        private Stream m_cmmbStream;

        private int m_timeIdx;

        private int m_channelId;
        /// <summary>
        /// The command we are performing
        /// </summary>
        private Command m_commandToPerform;

        /// <summary>
        /// A command specific parameter
        /// </summary>
        private object m_commandParameter;

        /// <summary>
        /// Initializes a new instance of the WorkQueueElement class
        /// </summary>
        /// <param name="cmd">the command to perform</param>
        /// <param name="prm">parameter for the command</param>
        public WorkQueueElement(Command cmd, object prm)
        {
            m_commandToPerform = cmd;
            m_commandParameter = prm;
        }

        public WorkQueueElement(Command cmd, Stream s, int idx, int channelid)
        {
            m_cmmbStream = s;
            m_commandToPerform = cmd;
            m_timeIdx = idx;
            m_channelId = channelid;
        }

        /// <summary>
        /// The type of work to perform
        /// </summary>
        public enum Command
        {

            Data, 
            /// <summary>
            /// Closes the media stream source
            /// </summary>
            Close,

            /// <summary>
            /// Report diagnostics back to the media element
            /// </summary>
            Diagnostics,

            /// <summary>
            /// Open a new manifest
            /// </summary>
            Open,

            /// <summary>
            /// Parse a chunk that we have received
            /// </summary>
            ParseChunk,

            /// <summary>
            /// Pause the media stream
            /// </summary>
            Pause,

            /// <summary>
            /// Handle a new sample request
            /// </summary>
            Sample,

            /// <summary>
            /// Perform a seek
            /// </summary>
            Seek,

            /// <summary>
            /// Stop the media stream
            /// </summary>
            Stop,

            /// <summary>
            /// Switch to a different media stream
            /// </summary>
            SwitchMedia,

            /// <summary>
            /// A chunk replacement suggested
            /// </summary>
            ReplaceMedia
        }

        /// <summary>
        /// Gets the command we are performing
        /// </summary>
        public Command CommandToPerform
        {
            get
            {
                return m_commandToPerform;
            }
        }

        /// <summary>
        /// Gets a command specific parameter
        /// </summary>
        public object CommandParameter
        {
            get
            {
                return m_commandParameter;
            }
        }

        public Stream Stream
        {
            get { return m_cmmbStream; }
        }
        public int  TimeIdx
        {
            get { return m_timeIdx; }
        
        }
        public int ChannelID
        {
            get { return m_channelId; }
        }
    }
}

