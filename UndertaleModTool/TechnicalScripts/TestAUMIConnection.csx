IpcMessage_t ipMessage;
IpcReply_t ipReply;

ipMessage.FuncID = 0x01;
ipMessage.Buffer = null;

SendAUMIMessage(ipMessage, ref ipReply);

if (ipReply.Buffer != null)
	ScriptMessage(System.Text.Encoding.ASCII.GetString(ipReply.Buffer));
else
	ScriptMessage("Didn't get a reply. Is AUMI not running?");