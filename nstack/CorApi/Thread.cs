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

    /** A thread in the debugged process. */
    public sealed class CorThread : WrapperBase
    {
        internal CorThread(ICorDebugThread thread)
            : base(thread)
        {
            m_th = thread;
        }

        internal ICorDebugThread GetInterface()
        {
            return m_th;
        }

        /** The process that this thread is in. */
        public CorProcess Process
        {
            get
            {
                ICorDebugProcess p;
                m_th.GetProcess(out p);
                return CorProcess.GetCorProcess(p);
            }
        }

        /** the OS id of the thread. */
        public int Id
        {
            get
            {
                uint id = 0;
                m_th.GetID(out id);
                return (int)id;
            }
        }

       
        /** The AppDomain that owns the thread. */
        public CorAppDomain AppDomain
        {
            get
            {
                ICorDebugAppDomain ad = null;
                m_th.GetAppDomain(out ad);
                return new CorAppDomain(ad);
            }
        }

        /** All stack chains in the thread. */
        public IEnumerable Chains
        {
            get
            {
                ICorDebugChainEnum ec = null;
                m_th.EnumerateChains(out ec);
                return (ec == null) ? null : new CorChainEnumerator(ec);
            }
        }

        private ICorDebugThread m_th;

    } /* class Thread */



    public enum CorFrameType
    {
        ILFrame, NativeFrame, InternalFrame,          
            RuntimeUnwindableFrame
    }


    public sealed class CorFrame : WrapperBase
    {
        internal CorFrame(ICorDebugFrame frame)
            : base(frame)
        {
            m_frame = frame;
        }

       
        public CorFrame Callee
        {
            get
            {
                ICorDebugFrame iframe;
                m_frame.GetCallee(out iframe);
                return (iframe == null ? null : new CorFrame(iframe));
            }
        }

        public CorFrame Caller
        {
            get
            {
                ICorDebugFrame iframe;
                m_frame.GetCaller(out iframe);
                return (iframe == null ? null : new CorFrame(iframe));
            }
        }

   
        public CorFunction Function
        {
            get
            {
                ICorDebugFunction ifunction;
                try
                {
                    m_frame.GetFunction(out ifunction);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    if (e.ErrorCode == (int)HResult.CORDBG_E_CODE_NOT_AVAILABLE)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }

                return (ifunction == null ? null : new CorFunction(ifunction));
            }
        }

     
        public CorFrameType FrameType
        {
            get
            {
                ICorDebugILFrame ilframe = GetILFrame();
                if (ilframe != null)
                    return CorFrameType.ILFrame;

                ICorDebugInternalFrame iframe = GetInternalFrame();
                if (iframe != null)
                    return CorFrameType.InternalFrame;

                ICorDebugRuntimeUnwindableFrame ruf = GetRuntimeUnwindableFrame();
                if (ruf != null)
                    return CorFrameType.RuntimeUnwindableFrame;
                return CorFrameType.NativeFrame;
            }
        }

        [CLSCompliant(false)]
        public CorDebugInternalFrameType InternalFrameType
        {
            get
            {
                ICorDebugInternalFrame iframe = GetInternalFrame();
                CorDebugInternalFrameType ft;

                if (iframe == null)
                    throw new CorException("Cannot get frame type on non-internal frame");

                iframe.GetFrameType(out ft);
                return ft;
            }
        }

     
      
        [CLSCompliant(false)]
        public void GetIP(out uint offset, out CorDebugMappingResult mappingResult)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if (ilframe == null)
            {
                offset = 0;
                mappingResult = CorDebugMappingResult.MAPPING_NO_INFO;
            }
            else
                ilframe.GetIP(out offset, out mappingResult);
        }
      
       
       
        private ICorDebugILFrame GetILFrame()
        {
            if (!m_ilFrameCached)
            {
                m_ilFrameCached = true;
                m_ilFrame = m_frame as ICorDebugILFrame;

            }
            return m_ilFrame;
        }

        private ICorDebugInternalFrame GetInternalFrame()
        {
            if (!m_iFrameCached)
            {
                m_iFrameCached = true;

                m_iFrame = m_frame as ICorDebugInternalFrame;
            }
            return m_iFrame;
        }

        private ICorDebugRuntimeUnwindableFrame GetRuntimeUnwindableFrame()
        {
            if(!m_ruFrameCached) 
            {
                m_ruFrameCached = true;
                
                m_ruFrame = m_frame as ICorDebugRuntimeUnwindableFrame;
            }
            return m_ruFrame;
        }
        public IEnumerable TypeParameters
        {
            get
            {
                ICorDebugTypeEnum icdte = null;
                ICorDebugILFrame ilf = GetILFrame();

                (ilf as ICorDebugILFrame2).EnumerateTypeParameters(out icdte);
                return new CorTypeEnumerator(icdte);        // icdte can be null, is handled by enumerator
            }
        }



        private ICorDebugILFrame m_ilFrame = null;
        private bool m_ilFrameCached = false;

        private ICorDebugInternalFrame m_iFrame = null;
        private bool m_iFrameCached = false;
        private ICorDebugRuntimeUnwindableFrame m_ruFrame = null;
        private bool m_ruFrameCached = false;

        internal ICorDebugFrame m_frame;
    }

    public sealed class CorChain : WrapperBase
    {
        internal CorChain(ICorDebugChain chain)
            : base(chain)
        {
            m_chain = chain;
        }

      
        public IEnumerable Frames
        {
            get
            {
                ICorDebugFrameEnum ef = null;
                m_chain.EnumerateFrames(out ef);
                return (ef == null) ? null : new CorFrameEnumerator(ef);
            }
        }

        private ICorDebugChain m_chain;
    }

    internal class CorFrameEnumerator : IEnumerable, IEnumerator, ICloneable
    {
        internal CorFrameEnumerator(ICorDebugFrameEnum frameEnumerator)
        {
            m_enum = frameEnumerator;
        }

        //
        // ICloneable interface
        //
        public Object Clone()
        {
            ICorDebugEnum clone = null;
            m_enum.Clone(out clone);
            return new CorFrameEnumerator((ICorDebugFrameEnum)clone);
        }

        //
        // IEnumerable interface
        //
        public IEnumerator GetEnumerator()
        {
            return this;
        }

        //
        // IEnumerator interface
        //
        public bool MoveNext()
        {
            ICorDebugFrame[] a = new ICorDebugFrame[1];
            uint c = 0;
            int r = m_enum.Next((uint)a.Length, a, out c);
            if (r == 0 && c == 1) // S_OK && we got 1 new element
                m_frame = new CorFrame(a[0]);
            else
                m_frame = null;
            return m_frame != null;
        }

        public void Reset()
        {
            m_enum.Reset();
            m_frame = null;
        }

        public Object Current
        {
            get
            {
                return m_frame;
            }
        }

        private ICorDebugFrameEnum m_enum;
        private CorFrame m_frame;
    }


    public sealed class CorFunction : WrapperBase
    {
        internal CorFunction(ICorDebugFunction managedFunction)
            : base(managedFunction)
        {
            m_function = managedFunction;
        }

        public CorClass Class
        {
            get
            {
                ICorDebugClass iclass;
                m_function.GetClass(out iclass);
                return (iclass == null ? null : new CorClass(iclass));
            }
        }

        public CorModule Module
        {
            get
            {
                ICorDebugModule imodule;
                m_function.GetModule(out imodule);
                return (imodule == null ? null : new CorModule(imodule));
            }
        }

        public int Token
        {
            get
            {
                UInt32 pMethodDef;
                m_function.GetToken(out pMethodDef);
                return (int)pMethodDef;
            }
        }

        public int Version
        {
            get
            {
                UInt32 pVersion;
                (m_function as ICorDebugFunction2).GetVersionNumber(out pVersion);
                return (int)pVersion;
            }
        }

        internal ICorDebugFunction m_function;
    }

    [Serializable]
    public class CorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CorException with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CorException(string message)
            : base(message)
        {
        }
    }

} /* namespace */
