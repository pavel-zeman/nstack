//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;
namespace Microsoft.Samples.Debugging.CorDebug
{
    /** A process running some managed code. */
    public sealed class CorProcess : CorController, IDisposable      
    {
        [CLSCompliant(false)]
        public static CorProcess GetCorProcess(ICorDebugProcess process)
        {
            Debug.Assert(process != null);
            lock (m_instances)
            {
                if (!m_instances.Contains(process))
                {
                    CorProcess p = new CorProcess(process);
                    m_instances.Add(process, p);
                    return p;
                }
                return (CorProcess)m_instances[process];
            }
        }

        public void Dispose()
        {
            // Release event handlers. The event handlers are strong references and may keep
            // other high-level objects (such as things in the MdbgEngine layer) alive.
            m_callbacksArray = null;

            if (m_callbackAttachedEvent != null)
            {
                m_callbackAttachedEvent.Close();
            }

            // Remove ourselves from instances hash.
            lock (m_instances)
            {
                m_instances.Remove(_p());
            }
        }

        private CorProcess(ICorDebugProcess process)
            : base(process)
        {
        }

        private static Hashtable m_instances = new Hashtable();

        private ICorDebugProcess _p()
        {
            return (ICorDebugProcess)GetController();
        }


        #region ICorDebug Wrappers

       
        public override void Stop(int timeout)
        {
            _p().Stop((uint)timeout);
        }

        public override void Continue(bool outOfBand)
        {
            if (!outOfBand &&                               // OOB event can arrive anytime (we just ignore them).
                (m_callbackAttachedEvent != null))
            {
                // first special call to "Continue" -- this fake continue will start delivering
                // callbacks.
                Debug.Assert(!outOfBand);
                ManualResetEvent ev = m_callbackAttachedEvent;
                // we set the m_callbackAttachedEvent to null first to prevent races.
                m_callbackAttachedEvent = null;
                ev.Set();
            }
            else
                base.Continue(outOfBand);
        }
        #endregion ICorDebug Wrappers

        // when process is first created wait till callbacks are enabled.
        private ManualResetEvent m_callbackAttachedEvent = new ManualResetEvent(false);

        private Delegate[] m_callbacksArray = new Delegate[(int)ManagedCallbackTypeCount.Last + 1];

     
        

        internal void DispatchEvent(ManagedCallbackType callback, CorEventArgs e)
        {
            try
            {
                // CorProcess.Continue has an extra abstraction layer. 
                // - The fist call just sets m_callbackAttachedEvent
                // - future calls go to ICorDebugProcess::Continue.
                // This ensures that we don't dispatch any callbacks until
                // after CorProcess.Continue() is called. 
                if (m_callbackAttachedEvent != null)
                {
                    m_callbackAttachedEvent.WaitOne(); // waits till callbacks are enabled
                }

                Debug.Assert((int)callback >= 0 && (int)callback < m_callbacksArray.Length);
                Delegate d = m_callbacksArray[(int)callback];
                if (d != null)
                {
                    d.DynamicInvoke(new Object[] { this, e });
                }
            }
            catch (Exception ex)
            {
                CorExceptionInCallbackEventArgs e2 = new CorExceptionInCallbackEventArgs(e.Controller, ex);
                Debug.Assert(false, "Exception in callback: " + ex.ToString());
                try
                {
                    // we need to dispatch the exception in callback error, but we cannot
                    // use DispatchEvent since throwing exception in ExceptionInCallback
                    // would lead to infinite recursion.
                    Debug.Assert(m_callbackAttachedEvent == null);
                    Delegate d = m_callbacksArray[(int)ManagedCallbackType.OnExceptionInCallback];
                    if (d != null)
                        d.DynamicInvoke(new Object[] { this, e2 });
                }
                catch (Exception ex2)
                {
                    Debug.Assert(false, "Exception in Exception notification callback: " + ex2.ToString());
                    // ignore it -- there is nothing we can do.
                }
                e.Continue = e2.Continue;
            }
        }

        #region Event handlers
       
        public event CorProcessEventHandler OnCreateProcess
        {
            add
            {
                int i = (int)ManagedCallbackType.OnCreateProcess;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnCreateProcess;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] - value;
            }
        }

        public event CorProcessEventHandler OnProcessExit
        {
            add
            {
                int i = (int)ManagedCallbackType.OnProcessExit;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnProcessExit;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] - value;
            }
        }

        public event CorThreadEventHandler OnCreateThread
        {
            add
            {
                int i = (int)ManagedCallbackType.OnCreateThread;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnCreateThread;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] - value;
            }
        }

        public event CorThreadEventHandler OnThreadExit
        {
            add
            {
                int i = (int)ManagedCallbackType.OnThreadExit;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnThreadExit;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] - value;
            }
        }

        public event CorModuleEventHandler OnModuleLoad
        {
            add
            {
                int i = (int)ManagedCallbackType.OnModuleLoad;
                m_callbacksArray[i] = (CorModuleEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnModuleLoad;
                m_callbacksArray[i] = (CorModuleEventHandler)m_callbacksArray[i] - value;
            }
        }

        public event CorAppDomainEventHandler OnCreateAppDomain
        {
            add
            {
                int i = (int)ManagedCallbackType.OnCreateAppDomain;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnCreateAppDomain;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] - value;
            }
        }

        public event CorAppDomainEventHandler OnAppDomainExit
        {
            add
            {
                int i = (int)ManagedCallbackType.OnAppDomainExit;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] + value;
            }
            remove
            {
                int i = (int)ManagedCallbackType.OnAppDomainExit;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] - value;
            }
        }

        #endregion Event handlers
    } /* class Process */
} /* namespace */
