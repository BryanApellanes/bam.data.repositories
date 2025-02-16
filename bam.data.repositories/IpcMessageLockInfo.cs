/*
	Copyright © Bryan Apellanes 2015  
*/

using System.Diagnostics;

namespace Bam
{
	[Serializable]
    public class IpcMessageLockInfo
    {
        public IpcMessageLockInfo()
        {
            this.ProcessId = Process.GetCurrentProcess().Id;
            this.MachineName = Environment.MachineName;
        }

        public int ProcessId { get; set; }

        public string MachineName { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is IpcMessageLockInfo lockInfo)
            {
                return lockInfo.ProcessId == ProcessId && MachineName.Equals(MachineName);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return $"{MachineName}:{ProcessId}".GetHashCode();
        }
    }
}
