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

            // Create the pipe
            using var pPipeServer = new NamedPipeServerStream("AUMI-IPC", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            // Wait 1/8th of a second for AUMI to connect.
            // If it doesn't connect in time (which it should), just return false to avoid a deadlock.
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
            catch (Exception e)
            {
                // Catch any errors that might arise if the connection is broken
                ScriptError("Could not write data to the pipe!\nError: " + e.Message);
                return false;
            }

            // Read the reply, the length of which is always a pre-set amount of bytes.
            byte[] bBuffer = new byte[ReplySize];
            pPipeServer.Read(bBuffer, 0, ReplySize);

            outReply = IpcReply_t.FromBytes(bBuffer);
            return true;
        }
        public byte[] CreateGMLBytecode(string sCode, bool UseAUMI = false)
        {
            // By Archie
            EnsureDataLoaded();

            var Context = UndertaleModLib.Compiler.Compiler.CompileGMLText(sCode, Data, null); // We don't need UndertaleCode

            using (var smStream = new MemoryStream())
            {
                using UndertaleWriter writer = new UndertaleWriter(smStream);

                foreach (var Instruction in Context.ResultAssembly)
                {
                    Instruction.Serialize(writer);

                    if (Instruction.Destination != null)
                    {
                        uint Override = (uint)(Instruction.Destination.Target.VarID + 100000);
                        writer.Position -= 4;
                        writer.WriteUInt24(Override);
                        writer.Position++;
                    }

                    else if (Instruction.Value is UndertaleInstruction.Reference<UndertaleVariable> && Instruction.Value != null)
                    {
                        uint Override = (uint)(((UndertaleInstruction.Reference<UndertaleVariable>)Instruction.Value).Target.VarID + 100000);
                        writer.Position -= 4;
                        writer.WriteUInt24(Override);
                        writer.Position++;
                    }

                    //From AUMI, if not injected, this will fail!
                    if (UseAUMI)
                    {
                        if (Instruction.Function != null)
                        {
                            if (Instruction.Function.Target == null)
                                continue;

                            if (Instruction.Function.Target.Name == null)
                                continue;

                            if (Instruction.Function.Target.Name.Content == null)
                                continue;

                            string Name = Instruction.Function.Target.Name.Content;
                            byte[] NameBytes = System.Text.Encoding.ASCII.GetBytes(Name);

                            IpcMessage_t ipMessage; IpcReply_t ipReply = new IpcReply_t();

                            ipMessage.FuncID = 3; // IPCID_GetFunctionByName
                            ipMessage.Buffer = new byte[512];

                            Buffer.BlockCopy(NameBytes, 0, ipMessage.Buffer, 0, NameBytes.Length);

                            SendAUMIMessage(ipMessage, ref ipReply);

                            if (ipReply.AUMIResult != 0) // AUMI_OK
                            {
                                ScriptError("AUMI - Failed to find function " + Name + " - result: " + ipReply.AUMIResult.ToString());
                            }
                            else
                            {
                                byte[] IndexBytes = new byte[4];
                                Buffer.BlockCopy(ipReply.Buffer, 0, IndexBytes, 0, 4); // Incompatible with old AUMI versions! Use commit ebd58007 or newer!
                                uint Index = BitConverter.ToUInt32(IndexBytes);
                                writer.Position -= 4;
                                writer.WriteUInt24(Index);
                                writer.Position++;
                            }
                        }
                    }
                }

                writer.Flush();

                byte[] Code = smStream.ToArray();
                return Code;
            }
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
        public void InitializeScriptDialog()
        {
            if (scriptDialog == null)
            {
                scriptDialog = new LoaderDialog("Script in progress...", "Please wait...");
                scriptDialog.Owner = this;
                scriptDialog.PreventClose = true;
            }
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
                // Using the 100 MS timer it can time out before successfully running, compilation errors are fast enough to get through.
                ScriptExecutionSuccess = true;
                ScriptErrorMessage = "";
                ScriptErrorType = "";
                return true;
            }
            return true;
        }
    }
}
