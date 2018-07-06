//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System.Text;

using Microsoft.Samples.Debugging.CorPublish.NativeApi;

namespace Microsoft.Samples.Debugging.CorPublish
{
    public sealed class CorPublish
    {
        public CorPublish()
        {
            m_publish = new CorpubPublishClass();
        }

        public CorPublishProcess GetProcess(int pid)
        {
            ICorPublishProcess proc;
            m_publish.GetProcess((uint)pid,out proc);
            return (proc==null)?null:new CorPublishProcess(proc);
        }
        

        private ICorPublish m_publish;
    }

    public sealed class CorPublishProcess
    {
        internal CorPublishProcess(ICorPublishProcess iprocess)
        {
            m_process = iprocess;
        }

        public string DisplayName
        {
            get
            {
                uint size;
                m_process.GetDisplayName(0, out size, null);
                StringBuilder szName = new StringBuilder((int)size);
                m_process.GetDisplayName((uint)szName.Capacity, out size, szName);
                return szName.ToString();
            }
        }
        

        private ICorPublishProcess m_process;
    }
    
}
