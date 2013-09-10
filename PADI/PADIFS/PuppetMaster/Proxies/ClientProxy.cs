using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using PuppetMaster.Exceptions;
using SharedLib;
using SharedLib.Exceptions;
using SharedLib.MetadataObjects;
using SharedLib.DataserverObjects;

namespace PuppetMaster.Proxies
{



    public class ClientProxy
    {
        private PuppetMasterCore _core;

        public ClientProxy(PuppetMasterCore core)
        {
            _core = core;
            RemotingConfiguration.Configure("../../App.config", true);
        }


        public IClientToPuppet ConnectToClient(int clientNumber)
        {
            String hostname;
            int port;

            if (!_core.AppManager.GetProcessIPandPort(ProcessType.Client, clientNumber, false, out hostname, out port))
                throw new CommandException("Can not get client IP and port. Client: " + clientNumber);

            IClientToPuppet server = null;
            try
            {

                server = (IClientToPuppet)Activator.GetObject(
                    typeof(IClientToPuppet), "tcp://" + hostname + ":" + port + "/PADIConnection");

                //_core.DisplayMessage("Connected to Client: " + clientNumber);
                return server;
            }
            catch (SocketException e)
            {
                throw new CommandException("Error connecting to Client" + e.Message);
            }
            catch (RemotingException e)
            {
                throw new CommandException("Error connecting to Client" + e.Message);
            }
        }


        public void NewCommand(string fullCommand)
        {
            String[] words = fullCommand.Split(' ', ',');
            String clientN = words[1].Split('-')[1];
            IClientToPuppet server;
            ASCIIEncoding ascii = new ASCIIEncoding();
            String toPuppet;
            int serverNumber;

            try
            {
                serverNumber = Convert.ToInt32(clientN);
            }
            catch (Exception e)
            {
                throw new CommandException("ClientProxy: Invalid server number" + fullCommand + " " + e.Message);
            }
            server = ConnectToClient(serverNumber);

            String command = words[0];

            try
            {
                switch (command)
                {
                    case "CREATE":
                        // Create(server,words,fullCommand);
                        toPuppet = server.Create(words[3], Convert.ToInt32(words[5]), Convert.ToInt32(words[7]), Convert.ToInt32(words[9]));
                        break;
                    case "OPEN":
                        // Open(server, words, fullCommand);
                        toPuppet = server.Open(words[3]);
                        break;
                    case "CLOSE":
                        //    Close(server, words, fullCommand);
                        toPuppet = server.Close(words[3]);
                        break;
                    case "DELETE":
                        //    Delete(server, words, fullCommand);
                        toPuppet = server.Delete(words[3]);
                        break;
                    case "READ":
                        //   Read(server,words,fullCommand);
                        SemanticType type = (words[5].Equals("monotonic")) ? SemanticType.Monotonic : SemanticType.Default;
                        toPuppet = server.Read(Convert.ToInt32(words[3]), type, Convert.ToInt32(words[7]));
                        break;
                    case "WRITE":
                        // Write(server,words,fullCommand);
                        if (fullCommand.Split('"').Length > 1)
                        {
                            toPuppet = server.Write(Convert.ToInt32(words[3]), ascii.GetBytes(fullCommand.Split('"')[1]));
                        }
                        else
                        {
                            toPuppet = server.Write(Convert.ToInt32(words[3]), Convert.ToInt32(words[5]));
                        }
                        break;
                    case "COPY":
                        //    Copy(server, words, fullCommand);
                        SemanticType type2 = (words[5].Equals("monotonic")) ? SemanticType.Monotonic : SemanticType.Default;
                        toPuppet = server.Copy(Convert.ToInt32(words[3]), type2, Convert.ToInt32(words[7]), ascii.GetBytes(fullCommand.Split('"')[1]));
                        break;
                    case "DUMP":
                        //    dump(server);
                        toPuppet = server.Dump();
                        break;
                    case "EXESCRIPT":
                        toPuppet = ExeScript( server, words, fullCommand );
                        break;
                    default:
                        throw new CommandException("Client Proxy: Invalid Command: " + fullCommand);
                }
            }
            catch ( SocketException e )
                {
                throw new CommandException( "Client Proxy: SocketException: " + fullCommand + " " + e.Message );
                }
            catch ( PadiException exN )
                {
                _core.DisplayMessage( exN.Description );
                return;
                }
            catch ( Exception ex )
                {
                _core.DisplayMessage( ex.Message );
                return;
                }
            // Return the String receved from the server to be Displayed
            _core.DisplayMessage(toPuppet);
        }


        ///////////////////////////////////// EXESCRIPT ////////////////////////////////////////////
        public delegate String DelExeScript(Queue<String> commands );

        public void CBExeScript(IAsyncResult ar)
        {
            DelExeScript del = (DelExeScript)((AsyncResult)ar).AsyncDelegate;
            String result = del.EndInvoke(ar);
            _core.DisplayMessage(result);
        }

        private String ExeScript(IClientToPuppet server, String[] words, String fullCommand)
        {
            if (words.Length < 3)
                throw new CommandException("ClientProxy: ExeScript: Invalid number of arguments " + fullCommand);
            try
            {
                String filename = words[2];
                //Read file
                Queue<String> commands = _core.ReadFileName(words[2]);
                DelExeScript remoteDelExeScript = new DelExeScript(server.ExeScript);
                AsyncCallback remoteCallBack = new AsyncCallback(CBExeScript);
                remoteDelExeScript.BeginInvoke( commands, remoteCallBack, null );
            }
            catch (InvalidCastException ex)
            {
                throw new CommandException("ClientProxy: ExeScript: Invalid arguments " + fullCommand + "Error: " + ex.Message);
            }
            return "Running execScript on Client";
        }


        //'     ____   _      _                                             _____            _       
        //'    / __ \ | |    | |     /\                                    / ____|          | |      
        //'   | |  | || |  __| |    /  \    ___  ___  _   _  _ __    ___  | |      ___    __| |  ___ 
        //'   | |  | || | / _` |   / /\ \  / __|/ __|| | | || '_ \  / __| | |     / _ \  / _` | / _ \
        //'   | |__| || || (_| |  / ____ \ \__ \\__ \| |_| || | | || (__  | |____| (_) || (_| ||  __/
        //'    \____/ |_| \__,_| /_/    \_\|___/|___/ \__, ||_| |_| \___|  \_____|\___/  \__,_| \___|
        //'                                            __/ |                                         
        //'                                           |___/                                          


        ///////////////////////////////////// CREATE ////////////////////////////////////////////
        //public delegate MetadataEntry DelCreate(String filename, int nbDataServers, int readQuorum, int writeQuorum);
        //public void CBCreate(IAsyncResult ar)
        //{
        //    try{
        //    DelCreate del = (DelCreate)((AsyncResult)ar).AsyncDelegate;
        //    MetadataEntry result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result.ToString());
        //    }
        //    catch (PadiException e)
        //    {
        //        _core.DisplayMessage(e.Description);
        //    }

        //    catch (Exception ex)
        //    {
        //        _core.DisplayMessage(ex.Message);
        //    }
        //}
        //private void Create(IClientToPuppet server, String[] words,String fullCommand)
        //{
        //    if (words.Length < 6)
        //    {
        //        throw new CommandException("ClientProxy: Create file: Invalid number of arguments " + fullCommand);
        //    }
        //    try
        //    {
        //        String fileName = words[2];
        //        int nbDataServers = Convert.ToInt32(words[3]);
        //        int readQuorum = Convert.ToInt32(words[4]);
        //        int writeQuorum = Convert.ToInt32(words[5]);
        //        DelCreate remoteDelCreate = new DelCreate(server.Create);
        //        AsyncCallback remoteCallBack = new AsyncCallback(CBCreate);
        //        remoteDelCreate.BeginInvoke(fileName, nbDataServers, readQuorum, writeQuorum, remoteCallBack, null);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Create file: Invalid rguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}


        ///////////////////////////////////// OPEN ////////////////////////////////////////////
        //public delegate MetadataEntry DelOpen(String filename);
        //public void CBOpen(IAsyncResult ar)
        //{
        //    DelOpen del = (DelOpen)((AsyncResult)ar).AsyncDelegate;
        //    MetadataEntry result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result.ToString());
        //}
        //private void Open(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 3)
        //    {
        //        throw new CommandException("ClientProxy: Open file: Invalid number of arguments " + fullCommand);
        //    }
        //    try
        //    {
        //        String fileName = words[2];
        //        DelOpen remoteDelOpen = new DelOpen(server.Open);
        //        AsyncCallback remoteCallBack = new AsyncCallback(CBOpen);
        //        remoteDelOpen.BeginInvoke(fileName,remoteCallBack, null);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Open file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}

        ///////////////////////////////////// CLOSE ////////////////////////////////////////////
        //public delegate Boolean DelClose(String filename);
        //public void CBClose(IAsyncResult ar)
        //{
        //    DelClose del = (DelClose)((AsyncResult)ar).AsyncDelegate;
        //    Boolean result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result.ToString());
        //}
        //private void Close(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 2)
        //    {
        //        throw new CommandException("ClientProxy: Close file: Invalid number of arguments " + fullCommand);
        //    }
        //    try
        //    {
        //        String fileName = words[2];
        //        DelClose remoteDelClose = new DelClose(server.Close);
        //        AsyncCallback remoteCallBack = new AsyncCallback(CBClose);
        //        remoteDelClose.BeginInvoke(fileName, remoteCallBack, null);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Close file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}

        ///////////////////////////////////// DELETE ////////////////////////////////////////////
        //public delegate Boolean DelDelete(String filename);
        //public void CBDelete(IAsyncResult ar)
        //{
        //    DelDelete del = (DelDelete)((AsyncResult)ar).AsyncDelegate;
        //    Boolean result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result.ToString());
        //}
        //private void Delete(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 2)
        //    {
        //        throw new CommandException("ClientProxy: Delete file: Invalid number of arguments " + fullCommand);
        //    }
        //    try
        //    {
        //        String fileName = words[2];
        //        DelDelete remoteDelDelete = new DelDelete(server.Delete);
        //        AsyncCallback remoteCallBack = new AsyncCallback(CBDelete);
        //        remoteDelDelete.BeginInvoke(fileName, remoteCallBack, null);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Delete file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}




        ///////////////////////////////////// READ ////////////////////////////////////////////
        //public delegate TFile DelRead(int fileRegister, SemanticType semantic, int stringRegister);

        //public void CBRead(IAsyncResult ar)
        //{
        //    DelRead del = (DelRead)((AsyncResult)ar).AsyncDelegate;
        //    TFile result = del.EndInvoke(ar);
        //    String txt = Encoding.ASCII.GetString(result.Data);
        //    _core.DisplayMessage(txt);
        //}

        //private void Read(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 5)
        //        throw new CommandException("ClientProxy: Read file: Invalid number of arguments " + fullCommand);
        //    try
        //    {
        //        int fileRegister = Convert.ToInt32(words[2]);
        //        SemanticType semantic = SemanticType.Default;
        //        switch (words[3])
        //        {
        //            case "default":
        //            case "DEFAULT":
        //                semantic = SemanticType.Default;
        //                break;
        //            case "monotonic":
        //            case "MONOTONIC":
        //                semantic = SemanticType.Monotonic;
        //                break;
        //            default:
        //                throw new CommandException("ClientProxy: Read File: Invalid semantic argument");
        //        }
        //        int stringRegister = Convert.ToInt32(words[4]);
        //        DelRead remoteDelRead = new DelRead(server.Read);
        //        AsyncCallback remoteCallBack = new AsyncCallback(CBRead);
        //        remoteDelRead.BeginInvoke(fileRegister, semantic, stringRegister, remoteCallBack, null);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Read file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}



        ///////////////////////////////////// WRITE ////////////////////////////////////////////
        //public delegate String DelWrite(int fileRegister, byte[] data);
        //public delegate String DelWriteRegister(int fileRegister, int byteArrayRegister);

        //public void CBWrite(IAsyncResult ar)
        //{
        //    DelWrite del = (DelWrite)((AsyncResult)ar).AsyncDelegate;
        //    String result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result);
        //}

        //public void CBWriteRegister(IAsyncResult ar)
        //{
        //    DelWriteRegister del = (DelWriteRegister)((AsyncResult)ar).AsyncDelegate;
        //    String result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result);
        //}


        //private void Write(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 4)
        //        throw new CommandException("ClientProxy: Write file: Invalid number of arguments " + fullCommand);
        //    int byteArrayRegister;
        //    int fileRegister;
        //    try
        //    {
        //         fileRegister = Convert.ToInt32(words[2]);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Read file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }

        //    if (int.TryParse(words[3].ToString(), out byteArrayRegister))
        //    {
        //        DelWriteRegister remoteDelWriteRegister = new DelWriteRegister(server.Write);
        //        AsyncCallback RemoteCallBack = new AsyncCallback(CBWriteRegister);
        //        remoteDelWriteRegister.BeginInvoke(fileRegister, byteArrayRegister, RemoteCallBack, null);
        //    }
        //    else
        //    {
        //        ASCIIEncoding encoding = new ASCIIEncoding();
        //        byte[] data = encoding.GetBytes(words[3]);
        //        DelWrite remoteDelWrite = new DelWrite(server.Write);
        //        AsyncCallback RemoteCallBack = new AsyncCallback(CBWrite);
        //       remoteDelWrite.BeginInvoke(fileRegister, data, RemoteCallBack, null);
        //    }
        //}

        // /////////////////////////////////// COPY ////////////////////////////////////////////

        //// hypothesis 01 - copy just use read and write callbacks making a "parsing" of arguments in the expected format
        //private void Copya(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 6)
        //        throw new CommandException("ClientProxy: Copy file: Invalid number of arguments " + fullCommand);
        //    try
        //    {
        //        // temp is a temporary string to store reads inside of copy functions and (pass to write?)
        //        String temp = "";
        //        // words to pass to the read function with the expected format
        //        String[] wordsRead = new string[5] {words[0],words[1],words[2],words[3],temp};
        //        // words to pass to the write function to make the write of salt in file1
        //        String[] wordsSalt = new string[4] {words[0],words[1],words[2],words[5]};
        //        // words to pass to the write function to write content of file1 in file2
        //        String[] wordsWrite = new string[4] {words[0], words[1], words[4], temp};
        //        Read(server,wordsRead,fullCommand);
        //        // write salt in the first file
        //        Write(server,wordsSalt,fullCommand);
        //        // write content of file1 in file2
        //        Write(server,wordsWrite,fullCommand);                
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Copy file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}


        //// hypotesis two - Copy do itself a remote invocation
        //public delegate String DelCopy(int fileRegisterRead, SemanticType semantic, int fileRegisterWrite, byte[] stringSalt);

        //public void CBCopy(IAsyncResult ar)
        //{
        //    DelCopy del = (DelCopy)((AsyncResult)ar).AsyncDelegate;
        //    String result = del.EndInvoke(ar);
        //    _core.DisplayMessage(result);
        //}

        //private void Copy(IClientToPuppet server, String[] words, String fullCommand)
        //{
        //    if (words.Length < 6)
        //        throw new CommandException("ClientProxy: Copy file: Invalid number of arguments " + fullCommand);
        //    try
        //    {
        //        int fileRegisterRead = Convert.ToInt32(words[2]);
        //        int fileRegisterWrite = Convert.ToInt32(words[4]);
        //        SemanticType semantic = SemanticType.Default;
        //        switch (words[3])
        //        {
        //            case "default":
        //            case "DEFAULT":
        //                semantic = SemanticType.Default;
        //                break;
        //            case "monotonic":
        //            case "MONOTONIC":
        //                semantic = SemanticType.Monotonic;
        //                break;
        //            default:
        //                throw new CommandException("ClientProxy: Copy File: Invalid semantic argument");
        //        }
        //        ASCIIEncoding encoding = new ASCIIEncoding();
        //        byte[] stringSalt = encoding.GetBytes(words[3]);
        //        DelCopy remoteDelCopy = new DelCopy(server.Copy);
        //        AsyncCallback remoteCallBack = new AsyncCallback(CBCopy);
        //        remoteDelCopy.BeginInvoke(fileRegisterRead, semantic, fileRegisterWrite, stringSalt, remoteCallBack, null);
        //    }
        //    catch (InvalidCastException ex)
        //    {
        //        throw new CommandException("ClientProxy: Copy file: Invalid arguments " + fullCommand + "Error: " + ex.Message);
        //    }
        //}




        ///////////////////////////////////// DUMP ////////////////////////////////////////////
        //public delegate String DelDump();
        ////RemoteAsyncDelegate which returns boolean 
        //public void CBDump(IAsyncResult ar)
        //{
        //    try
        //    {
        //        DelDump del = (DelDump) ((AsyncResult) ar).AsyncDelegate;
        //        String result = del.EndInvoke(ar);
        //        _core.DisplayMessage(result);
        //    }
        //    catch (PadiException e)
        //    {
        //        _core.DisplayMessage(e.Description);
        //    }

        //    catch (Exception ex)
        //    {
        //        _core.DisplayMessage(ex.Message);
        //    }

        //}

        //public delegate Boolean RemoteAsyncDelegateBoolean();


        //private void dump(IClientToPuppet server)
        //{
        //        DelDump remoteDump = new DelDump(server.Dump);
        //        AsyncCallback RemoteDumpCallBack = new AsyncCallback(CBDump);
        //        remoteDump.BeginInvoke(RemoteDumpCallBack, null);
        //}




    }



}
