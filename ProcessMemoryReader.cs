using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;  
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PandyProductions
{
    class ProcessMemoryReaderApi
    {
        // constants information can be found in <winnt.h>
        [Flags]
        public enum ProcessAccessType
        {
            PROCESS_TERMINATE = (0x0001),
            PROCESS_CREATE_THREAD = (0x0002),
            PROCESS_SET_SESSIONID = (0x0004),
            PROCESS_VM_OPERATION = (0x0008),
            PROCESS_VM_READ = (0x0010),
            PROCESS_VM_WRITE = (0x0020),
            PROCESS_DUP_HANDLE = (0x0040),
            PROCESS_CREATE_PROCESS = (0x0080),
            PROCESS_SET_QUOTA = (0x0100),
            PROCESS_SET_INFORMATION = (0x0200),
            PROCESS_QUERY_INFORMATION = (0x0400)
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, int size, out int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        public static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        public static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);
    }

    public class ProcessMemoryReader : IDisposable
    {
        private const int MAX_MODULE_NAME32 = 255;
        private const int MAX_PATH = 260;

        private UInt32 m_pid;
        private IntPtr m_hpid;
        private IntPtr m_hbase;

        public ProcessMemoryReader(int PID) : this((uint)PID) { }
        public ProcessMemoryReader(uint PID)
        {
            m_pid = PID;
            OpenProcess();
        }

        [Flags]
        private enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct MODULEENTRY32
        {
            public UInt32 dwSize;
            public UInt32 th32ModuleID;
            public UInt32 th32ProcessID;
            public UInt32 GlblcntUsage;
            public UInt32 ProccntUsage;
            public IntPtr modBaseAddr;
            public UInt32 modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_MODULE_NAME32 + 1)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szExePath;
        };

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);
        [DllImport("kernel32.dll")]
        private static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);
        [DllImport("kernel32.dll")]
        private static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        private static IntPtr GetModuleBase(uint PID, string ModuleName)
        {
            //Take a snapshot of the module list
            IntPtr hModuleList = CreateToolhelp32Snapshot(SnapshotFlags.Module | SnapshotFlags.Module32, PID);
            if (hModuleList == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                MODULEENTRY32 me32 = new MODULEENTRY32();
                me32.dwSize = (uint)Marshal.SizeOf(typeof(MODULEENTRY32));

                //Set cursor at the start and grab the info
                if (!Module32First(hModuleList, ref me32))
                    return IntPtr.Zero;

                //If the module matches our target, use its baseaddress. Otherwise, grab the next module in the list
                do
                {
                    if (me32.szModule == ModuleName)
                        return me32.modBaseAddr;
                } while (Module32Next(hModuleList, ref me32));
            }
            finally
            {
                //gets fired even if return is used
                ProcessMemoryReaderApi.CloseHandle(hModuleList);
            }
            return IntPtr.Zero;
        }

        private bool GetModuleInfo(uint PID, string ModuleName, out MODULEENTRY32 ModuleInfo)
        {
            ModuleInfo = default(MODULEENTRY32);

            //Take a snapshot of the module list
            IntPtr hModuleList = CreateToolhelp32Snapshot(SnapshotFlags.Module | SnapshotFlags.Module32, PID);
            if (hModuleList == IntPtr.Zero)
                return false;

            try
            {
                MODULEENTRY32 me32 = new MODULEENTRY32();
                me32.dwSize = (uint)Marshal.SizeOf(typeof(MODULEENTRY32));

                //Set cursor at the start and grab the info
                if (!Module32First(hModuleList, ref me32))
                    return false;

                ProcessModuleCollection progMods = System.Diagnostics.Process.GetProcessById((int)m_pid).Modules;
                foreach (ProcessModule module in progMods)
                {
                    if (module.ModuleName == ReadProcess.MainModule.ModuleName)
                    {
                        ModuleInfo = me32;
                        return true;
                    }
                    Module32Next(hModuleList, ref me32);
                }
            }
            finally
            {
                //gets fired even if return is used
                ProcessMemoryReaderApi.CloseHandle(hModuleList);
            }
            return false;
        }

        /// <summary>Gets or sets the BaseAddress used to calculate memory offsets</summary>
        public IntPtr BaseAddress
        {
            get { return m_hbase; }
            set { m_hbase = value; }
        }

        /// <summary>Gets whether the targer process is still running or not</summary>
        public bool HasExited
        {
            get
            {
                if (m_hpid == IntPtr.Zero)
                    return true;
                uint code;
                if (ProcessMemoryReaderApi.GetExitCodeProcess(m_hpid, out code))
                    return (code != 259);
                return true;
            }
        }

        /// <summary>Close access to the process and release the handle</summary>
        public void Close()
        {
            if (m_hpid == IntPtr.Zero)
                return;
            ProcessMemoryReaderApi.CloseHandle(m_hpid);
            m_hpid = IntPtr.Zero;
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>	
        /// Process from which to read		
        /// </summary>
        public Process ReadProcess
        {
            get
            { return m_ReadProcess; }
            set
            { m_ReadProcess = value; }
        }

        private Process m_ReadProcess = null;

        private IntPtr m_hProcess = IntPtr.Zero;

        internal bool IsThisProcessOpen()
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.Id == m_pid)
                {
                    return true;
                }
            }
            return false;
        }

        public void OpenProcess()
        {
            ReadProcess = Process.GetProcessById((int)m_pid);
            //			m_hProcess = ProcessMemoryReaderApi.OpenProcess(ProcessMemoryReaderApi.PROCESS_VM_READ, 1, (uint)m_ReadProcess.Id);
            ProcessMemoryReaderApi.ProcessAccessType access;
            access = ProcessMemoryReaderApi.ProcessAccessType.PROCESS_VM_READ
                | ProcessMemoryReaderApi.ProcessAccessType.PROCESS_VM_WRITE
                | ProcessMemoryReaderApi.ProcessAccessType.PROCESS_VM_OPERATION;
            m_hProcess = ProcessMemoryReaderApi.OpenProcess((uint)access, 1, (uint)m_ReadProcess.Id);
            m_hpid = m_hProcess;
            m_pid = (uint)m_ReadProcess.Id;
            ProcessModuleCollection progMods = m_ReadProcess.Modules;
            foreach (ProcessModule module in progMods)
            {
                if (module.ModuleName == "FFXiMain.dll")//ReadProcess.MainModule.ModuleName)
                {
                    m_hbase = module.BaseAddress;
                    break;
                }
            }
        }

        public byte[] ReadProcessMemory(IntPtr MemoryAddress, uint bytesToRead, out int bytesRead)
        {
            byte[] buffer = new byte[bytesToRead];
            IntPtr ptrBytesRead;
            ProcessMemoryReaderApi.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, bytesToRead, out ptrBytesRead);
            bytesRead = ptrBytesRead.ToInt32();
            return buffer;
        }
        public int ReadProcessMemory(IntPtr Address, int Length, out byte[] data)
        {
            byte[] buffer = new byte[Length];
            int iReadCount;
            ProcessMemoryReaderApi.ReadProcessMemory(m_hpid, Address, buffer, Length, out iReadCount);
            data = buffer;
            return iReadCount;
        }

        public void WriteProcessMemory(IntPtr MemoryAddress, byte[] bytesToWrite, out int bytesWritten)
        {
            IntPtr ptrBytesWritten;
            ProcessMemoryReaderApi.WriteProcessMemory(m_hProcess, MemoryAddress, bytesToWrite, (uint)bytesToWrite.Length, out ptrBytesWritten);
            bytesWritten = ptrBytesWritten.ToInt32();
        }

        public int ReadOffset(IntPtr Offset, int Length, out byte[] data)
        {
            int baseoffset = (int)this.m_hbase + (int)Offset;
            return ReadProcessMemory((IntPtr)baseoffset, Length, out data);
        }

        public T ReadStruct<T>(IntPtr Address)
        {
            byte[] buffer;
            int cnt = ReadProcessMemory(Address, Marshal.SizeOf(typeof(T)), out buffer);
            if (cnt > 0)
            {
                GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    return (T)Marshal.PtrToStructure(pinned.AddrOfPinnedObject(), typeof(T));
#if DEBUG
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ReadStruct: Unable to coerce foreign data into <" + typeof(T).ToString() + ">: " + ex.Message);
#else
                }
                catch
                {
#endif
                }
                finally
                {
                    pinned.Free();
                }
            }
            return default(T);
        }


        public T[] ReadStructArray<T>(IntPtr Address, int Count)
        {
            if (Count <= 0)
                return default(T[]);

            //Read in the number of bytes required for each array index
            byte[] buffer;
            int cnt = ReadProcessMemory(Address, Marshal.SizeOf(typeof(T)) * Count, out buffer);
            if (cnt > 0)
            {
                GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    //Read in the structure at each position and coerce it into its slot
                    T[] output = new T[Count];
                    IntPtr current = pinned.AddrOfPinnedObject();
                    for (int i = 0; i < Count; i++)
                    {
                        output[i] = (T)Marshal.PtrToStructure(current, typeof(T));
                        current = (IntPtr)((int)current + Marshal.SizeOf(output[i])); //advance to the next index
                    }
                    return output;
#if DEBUG
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ReadStruct: Unable to coerce foreign data into <" + typeof(T).ToString() + ">: " + ex.Message);
#else
                }
                catch
                {
#endif
                }
                finally
                {
                    pinned.Free();
                }
            }
            return default(T[]);
        }

        public string ReadString(IntPtr Address, int MaxLen)
        {
            byte[] buffer;
            int cnt = ReadProcessMemory(Address, MaxLen, out buffer);
            if (cnt > 0)
            {
                GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStringAnsi(pinned.AddrOfPinnedObject());
#if DEBUG
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ReadString: Unable to coerce foreign data into string: " + ex.Message);
#else
                }
                catch
                {
#endif
                }
                finally
                {
                    pinned.Free();
                }
            }
            return "";
        }

        public T ReadOffsetStruct<T>(IntPtr Offset)
        {
            byte[] buffer;
            int cnt = ReadOffset(Offset, Marshal.SizeOf(typeof(T)), out buffer);
            if (cnt > 0)
            {
                GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    return (T)Marshal.PtrToStructure(pinned.AddrOfPinnedObject(), typeof(T));
#if DEBUG
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ReadOffsetStruct: Unable to coerce foreign data into <" + typeof(T).ToString() + ">: " + ex.Message);
#else
                }
                catch
                {
#endif
                }
                finally
                {
                    pinned.Free();
                }
            }
            return default(T);
        }

        public IntPtr FindSignature(string Signature)
        {
            return FindSignature(Signature, (int)m_hbase, false);
        }
        public IntPtr FindSignature(string Signature, bool readLoc)
        {
            return FindSignature(Signature, (int)m_hbase, readLoc);
        }
        public IntPtr FindSignature(byte[] Signature, bool readLoc)
        {
            return FindSignature(Signature, (int)m_hbase, readLoc);
        }
        public IntPtr FindSignature(string Signature, int baseAddr)
        {
            return FindSignature(Signature, baseAddr, false);
        }

        //private BinSearch bSearch = new BinSearch();

        public IntPtr FindSignature(string Signature, int baseAddr, bool readLoc)
        {
            //if (Signature.Length == 0 || Signature.Length % 2 != 0)
            //  throw new MemoryReaderException("Invalid signature");

            //Narrow the search breadth to only the requested module's address space
            MODULEENTRY32 info;
            if (GetModuleInfo(m_pid, "ffximain.dll", out info))
            {
                ProcessModule mod = null;
                for (int i = 0; i < ReadProcess.Modules.Count; i++)
                    if (ReadProcess.Modules[i].ModuleName.ToLower() == "ffximain.dll")
                    {
                        mod = ReadProcess.Modules[i];

                        //Take a memory snapshot of the entire module and then locate the pointer
                        byte[] buffer;
                        //                int cnt = ReadProcessMemory((IntPtr)info.modBaseAddr, (int)info.modBaseSize, out buffer);
                        int cnt = ReadProcessMemory(mod.BaseAddress, mod.ModuleMemorySize, out buffer);
                        if (cnt > 0)
                        {
                            return BinSearch.FindSignature(buffer, Signature, baseAddr, readLoc);
                        }
                        break;
                    }
            }
            return IntPtr.Zero;
        }

        public IntPtr FindSignature(byte[] Signature, int baseAddr, bool readLoc)
        {
            //if (Signature.Length == 0 || Signature.Length % 2 != 0)
            //  throw new MemoryReaderException("Invalid signature");

            //Narrow the search breadth to only the requested module's address space
            MODULEENTRY32 info;
            if (GetModuleInfo(m_pid, ReadProcess.MainModule.ModuleName, out info))
            {
                //Take a memory snapshot of the entire module and then locate the pointer
                IntPtr value = IntPtr.Zero;
                int iter = 0;
                int nextRead;
                while (value == IntPtr.Zero)
                {
                    nextRead = baseAddr + (iter * 0x1000);
                    if (nextRead > 0x60000000)
                    {
                        break;
                    }

                    byte[] buffer;
                    int cnt = ReadProcessMemory((IntPtr)nextRead, 0x1000, out buffer);
                    if (cnt > 0)
                    {
                        for (int i = 0; i < buffer.Length - Signature.Length; i++)
                        {
                            for (int j = 0; j < Signature.Length; j++)
                            {
                                if (buffer[i + j] != Signature[j])
                                    break;
                                else if (j == Signature.Length - 1)
                                {
                                    return (IntPtr)(i + nextRead);
                                }
                            }
                        }
                    }
                    iter++;
                }
            }
            return IntPtr.Zero;
        }
/*
        public IntPtr FindStructure()
        {
            //if (Signature.Length == 0 || Signature.Length % 2 != 0)
            //  throw new MemoryReaderException("Invalid signature");

            //Narrow the search breadth to only the requested module's address space
            const int SIZE = 0x1C;
            
            MODULEENTRY32 info;
            if (GetModuleInfo(m_pid, ReadProcess.MainModule.ModuleName, out info))
            {
                //Take a memory snapshot of the entire module and then locate the pointer
                IntPtr value = IntPtr.Zero;
                int iter = 0;
                int nextRead;
                while (value == IntPtr.Zero)
                {
                    nextRead = (int)info.modBaseAddr + (iter * 0x1000);
                    if (nextRead > 0x60000000)
                    {
                        int ih = 0;
                        ih = value.ToInt32();
                        break;
                    }

                    byte[] buffer;
                    int cnt = ReadProcessMemory((IntPtr)nextRead, 0x1000 + SIZE, out buffer);
                    if (cnt > 0)
                    {
                        for (int i = 0; i < buffer.Length - SIZE; i++)
                        {
                            if (BitConverter.ToInt32(buffer, i + 0x18) - BitConverter.ToInt32(buffer, i + 0x10) == 0x1E00 && BitConverter.ToInt32(buffer, i + 0x8) - BitConverter.ToInt32(buffer, i + 0) == 0x4C)
                                return (IntPtr)(i - 0x34 + nextRead);
                        }
                    }
                    iter++;
                }
            }
            return IntPtr.Zero;
        }
*/
        public MemoryBuffer createSearchBuffer(int location, int size)
        {
            MemoryBuffer sbuffer = new MemoryBuffer(m_pid);
            sbuffer.loadBuffer(location, size);
            return sbuffer;
        }
    }
    /// <summary>Custom exception class for catch filtering.</summary>
    public sealed class MemoryReaderException : Exception
    {
        public MemoryReaderException(string message) : base(message) { }
        public MemoryReaderException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class MemoryBuffer
    {
        private byte[] buffer;
        private int offset;
        private uint PID;

        public MemoryBuffer(uint PID)
        {
            this.PID = PID;
        }

        public int loadBuffer(int startLocation, int size)
        {
            offset = startLocation;
            ProcessMemoryReader preader = new ProcessMemoryReader(PID);
            int value = preader.ReadProcessMemory((IntPtr)startLocation, (int)size, out buffer);
            preader.Dispose();
            return value;
        }

        public IntPtr ReadPointer(IntPtr location)
        {
            return ReadPointer((int)location);
        }
        public IntPtr ReadPointer(int location)
        {
            if (buffer == null || (int)location + 4 > buffer.Length + offset)
                return IntPtr.Zero;
            return (IntPtr)BitConverter.ToInt32(buffer, location - offset);
        }

        public IntPtr Read2Bytes(IntPtr location)
        {
            return Read2Bytes((int)location);
        }
        public IntPtr Read2Bytes(int location)
        {
            if (buffer == null || (int)location + 4 > buffer.Length + offset)
                return IntPtr.Zero;
            return (IntPtr)BitConverter.ToInt16(buffer, location - offset);
        }

        public byte[] ReadBytes(IntPtr location, int length)
        {
            return ReadBytes((int)location, length);
        }
        public byte[] ReadBytes(int location, int length)
        {
            if (buffer == null || (int)location + 4 > buffer.Length + offset)
                return null;
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = buffer[location-offset+i];
            }
            return result;
        }

    }
}