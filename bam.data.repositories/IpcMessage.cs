/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Bam.Net.Logging;
using Bam.Net.Configuration;
using System.Configuration;

namespace Bam.Net
{
    /// <summary>
    /// A file based
    /// IPC mechanism that uses a single file
    /// with a binary formatted copy of an instance of T 
    /// in the directory RootDirectory with the name specified.
    /// </summary>
    public class IpcMessage: Loggable
    {
        internal IpcMessage(string name, Type messageType)
        {
            Name = name;           
            LockTimeout = 150;
            AcquireLockRetryInterval = 50;
            MessageType = messageType;
        }

        internal IpcMessage(string name, Type messageType, string rootDir)
            : this(name, messageType)
        {
            this.RootDirectory = rootDir ?? RuntimeSettings.ProcessDataFolder;
        }

        public static void Delete(string name, Type type, string rootDir = null)
        {
            IpcMessage toDelete = new IpcMessage(name, type, rootDir);
            if (Directory.Exists(toDelete.RootDirectory))
            {
                Directory.Delete(toDelete.RootDirectory, true);
            }
        }

        public string Name { get; set; }

        public virtual bool Write(object data)
        {
            if(AcquireLock(LockTimeout))
            {
                // if the message file doesn't exist write to it
                string writeTo = MessageFile;
                if (File.Exists(MessageFile))
                {
                    //  else write to the WriteFile
                    writeTo = WriteFile;
                }

                data.EncodeToFile(writeTo);//data.ToBinaryFile(writeTo);

                // if WriteFile exists move it on top of MessageFile
                if (File.Exists(WriteFile))
                {
                    File.Delete(MessageFile);
                    File.Move(WriteFile, MessageFile);
                }

                // copy MessageFile to ReadFile
                File.Copy(MessageFile, ReadFile, true);
                File.Move(LockFile, TempLockFile);
                File.Delete(TempLockFile);
                return true;
            }

            return false;
        }
        
        public T Read<T>()
        {            
            if (File.Exists(ReadFile))
            {
                return ReadFile.DecodeFromFile<T>();//ReadFile.FromBinaryFile<T>();
            }

            return default(T);
        }

        /// <summary>
        /// The number of milliseconds to wait to 
        /// try and acquire a lock
        /// </summary>
        public int LockTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the acquire lock retry interval, the amount of time in milliseconds to sleep
        /// between attempts to acquire a lock.
        /// </summary>
        /// <value>
        /// The acquire lock retry interval.
        /// </value>
        public int AcquireLockRetryInterval
        {
            get;
            set;
        }

        public Type MessageType
        {
            get;
            set;
        }

        string _rootDirectory;
        object _rootDirectoryLock = new object();
        protected internal string RootDirectory
        {
            get
            {
                return _rootDirectoryLock.DoubleCheckLock(ref _rootDirectory, () =>
                {
					return Path.Combine(RuntimeSettings.ProcessDataFolder, MessageType.Name);
                });
            }
            set
            {
				_rootDirectory = Path.Combine(value, MessageType.Name);
            }
        }

        //[Verbosity(VerbosityLevel.Warning, SenderMessageFormat="{Name}:Unable to acquire lock:{LastExceptionMessage}")]
        public event EventHandler AcquireLockException;
      
        protected void OnAcquireLockException(Exception ex)
        {
            if (AcquireLockException != null)
            {
                LastExceptionMessage = "PID={0}:{1}"._Format(Process.GetCurrentProcess().Id, ex.Message);
                AcquireLockException(this, new EventArgs());
            }
        }

		//[Verbosity(VerbosityLevel.Warning, SenderMessageFormat = "PID {CurrentLockerId} has lock on {Name}")]
        public event EventHandler WaitingForLock;

        protected void OnWaitingForLock()
        {
            WaitingForLock?.Invoke(this, new EventArgs());
        }
                
        public string LastExceptionMessage { get; set; }

        /// <summary>
        /// Gets the process id of the process who has 
        /// the lock
        /// </summary>
        public string CurrentLockerId { get; set; }

        public string CurrentLockerMachineName { get; set; }

        protected string LockFile => Path.Combine(RootDirectory, "{0}.lock"._Format(Name));

        protected string TempLockFile => $"{LockFile}.tmp";

        protected internal string WriteFile => Path.Combine(RootDirectory, "{0}.write"._Format(Name));

        protected internal string ReadFile => Path.Combine(RootDirectory, "{0}.read"._Format(Name));

        protected internal string MessageFile => Path.Combine(RootDirectory, Name);

        private void EnsureRoot()
        {
            if (!Directory.Exists(RootDirectory))
            {
                Directory.CreateDirectory(RootDirectory);
            }
        }

        static readonly object _lock = new object();
        private bool AcquireLock(int timeoutInMilliseconds)
        {
            try
            {
                lock (_lock)
                {
                    EnsureRoot();
                    IpcMessageLockInfo lockInfo = new IpcMessageLockInfo();
                    bool timeoutExpired = Exec.TakesTooLong(() =>
                    {
                        bool logged = false;
                        while (File.Exists(LockFile))
                        {
                            if (!logged)
                            {
                                logged = true;
                                IpcMessageLockInfo currentLockInfo = LockFile.DecodeFromFile<IpcMessageLockInfo>();// LockFile.FromBinaryFile<IpcMessageLockInfo>();
                                CurrentLockerId = currentLockInfo?.ProcessId.ToString();
                                CurrentLockerMachineName = currentLockInfo?.MachineName;
                                OnWaitingForLock();
                            }

                            Thread.Sleep(AcquireLockRetryInterval);
                        }
                        return LockFile;
                    }, (lockFile) =>
                    {
                        lockInfo.EncodeToFile(lockFile);//ToBinaryFile(lockFile);
                        return lockFile;
                    }, TimeSpan.FromMilliseconds(timeoutInMilliseconds));

                    return !timeoutExpired;
                }
            }
            catch (Exception ex)
            {
                OnAcquireLockException(ex);
                return false;
            }
        }
        
    }
}
