//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;

namespace Microsoft.Samples.Debugging.CorDebug
{

    public enum CorDebuggerVersion
    {
        RTM     = 1, //v1.0
        Everett = 2, //v1.1
        Whidbey = 3, //v2.0
    }

   

    public abstract class TokenUtils
    {
        public static int RidFromToken(int token)
        {
            return (int)( (UInt32)token & 0x00ffffff);
        }

        public static bool IsNullToken(int token)
        {
            return (RidFromToken(token)==0);
        }
    }


}
