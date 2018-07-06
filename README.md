# nstack
Simple utility to generate .NET managed process thread dump (similar to Java jstack). 
Based on the code from Managed Stack Explorer available at https://github.com/vadimskipin/MSE.

To run it, use `nstack.exe <PID>`. It then generates thread dump of all managed threads to standard output.
