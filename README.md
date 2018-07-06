# nstack
Simple utility to generate .NET managed process thread dump (similar to Java jstack), i.e. current stacktrace of all managed threads. 
Based on the code from Managed Stack Explorer available at https://github.com/vadimskipin/MSE.

To run it, use `nstack.exe <PID>`. It generates thread dump of all managed threads to standard output.

You can build the binary yourself or download [the latest release](https://github.com/pavel-zeman/nstack/releases).
