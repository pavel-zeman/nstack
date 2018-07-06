//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Represents a scope at which program execution can be controlled.
     */
    public class CorController : WrapperBase
    {
        internal CorController (ICorDebugController controller)
            :base(controller)
        {
            m_controller = controller;
        }

        /**
         * Cooperative stop on all threads running managed code in the process.
         */
        public virtual void Stop (int timeout)
        {
            m_controller.Stop ((uint)timeout);
        }

        /**
         * Continue processes after a call to Stop.
         *
         * outOfBand is true if continuing from an unmanaged event that
         * was sent with the outOfBand flag in the unmanaged callback;
         * false if continueing from a managed event or normal unmanaged event.
         */
        public virtual void Continue (bool outOfBand)
        {
            m_controller.Continue (outOfBand ? 1 : 0);
        }

        /**
         * Are there managed callbacks queued up for the requested thread?
         */
        public bool HasQueuedCallbacks (CorThread managedThread)
        {
            int queued = 0;
            m_controller.HasQueuedCallbacks( (managedThread==null)?null:managedThread.GetInterface(),
                                             out queued
                                             );
            return !(queued == 0);
        }

        /** Enumerate over all threads in active in the process. */
        public IEnumerable Threads
        {
            get 
            {
                ICorDebugThreadEnum ethreads;
                m_controller.EnumerateThreads (out ethreads);
                return new CorThreadEnumerator (ethreads);
            }
        }

        /** Detach the debugger from the process/appdomain. */
        public void Detach ()
        {
            m_controller.Detach ();
        }
    
       
        
        [CLSCompliant(false)]
        protected ICorDebugController GetController ()
        {
            return m_controller;
        }
        
        private ICorDebugController m_controller;
    }
}
