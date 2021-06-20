using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO.Pipes;

namespace UndertaleModTool
{
    // Adding misc. scripting functions here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply)
        {
            // By Archie
            const int ReplySize = 132;

            //Create the pipe
            using var pPipeServer = new NamedPipeServerStream("AUMI-IPC", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            //Wait 1/8th of a second for AUMI to connect.
            //If it doesn't connect in time (which it should), just return false to avoid a deadlock.
            if (!pPipeServer.IsConnected)
            {
                pPipeServer.WaitForConnectionAsync();
                Thread.Sleep(125);
                if (!pPipeServer.IsConnected)
                {
                    pPipeServer.DisposeAsync();
                    return false;
                }
            }

            try
            {
                //Send the message
                pPipeServer.Write(ipMessage.RawBytes());
                pPipeServer.Flush();
            }
            catch (IOException)
            {
                //Catch any errors that might arise if the connection is broken
                ScriptError("Could not write data to the pipe!");
                return false;
            }

            //Read the reply, the length of which is always a pre-set amount of bytes.
            byte[] bBuffer = new byte[ReplySize];
            pPipeServer.Read(bBuffer, 0, ReplySize);

            outReply = IpcReply_t.FromBytes(bBuffer);
            return true;
        }

        public bool RunUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            RunScript(path);
            if (!ScriptExecutionSuccess)
                ScriptError("An error of type \"" + ScriptErrorType + "\" occurred. The error is:\n\n" + ScriptErrorMessage, ScriptErrorType);
            return ScriptExecutionSuccess;
        }
        public bool LintUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(100);
                CancellationToken token = source.Token;
                object test = CSharpScript.EvaluateAsync(File.ReadAllText(path), scriptOptions, this, typeof(IScriptInterface), token);
            }
            catch (CompilationErrorException exc)
            {
                ScriptError(exc.Message, "Script compile error");
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "CompilationErrorException";
                return false;
            }
            catch (Exception)
            {
                //Using the 100 MS timer it can time out before successfully running, compilation errors are fast enough to get through.
                ScriptExecutionSuccess = true;
                ScriptErrorMessage = "";
                ScriptErrorType = "";
                return true;
            }
            return true;
        }
    }
}
