using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Text;
using PuppetMaster.Exceptions;
using SharedLib;
using SharedLib.Exceptions;


namespace PuppetMaster.Proxies
{
    public class DataProxy
    {
        private PuppetMasterCore _core;

        public DataProxy(PuppetMasterCore core)
        {
            _core = core;
            RemotingConfiguration.Configure("../../App.config", true);
            
        }
         

        public IDataToPuppet ConnectToDataServer(int dataserverNumber, Boolean recovery)
        {
            String hostname;
            int port;
            if (!_core.AppManager.GetProcessIPandPort(ProcessType.DataServer, dataserverNumber, recovery, out hostname, out port))
                throw new CommandException("Can not get dataserver IP and port. Dataserver: " + dataserverNumber);

            IDataToPuppet server = null;
            try
            {
                server = (IDataToPuppet)Activator.GetObject(
                    typeof(IDataToPuppet), "tcp://" + hostname + ":" + port + "/PADIConnection");
                //_core.DisplayMessage("Connected to Dataserver: " + dataserverNumber);
                return server;
            }
            catch (SocketException e)
            {
                throw new CommandException("Error connecting to Dataserver" + e.Message);
            }
            catch (RemotingException e)
            {
                throw new CommandException("Error connecting to Dataserver" + e.Message);
            }
        }


        public void NewCommand(string fullCommand)
        {
            String[] words = fullCommand.Split(' ');
            String res = words[1];
            String[] res2 = res.Split('-');
            IDataToPuppet server;
            int serverNumber;
            String toPuppet;
            try
            {
                serverNumber = Convert.ToInt32(res2[1]);
            }
            catch (Exception e)
            {
                throw new CommandException("Data Proxy: Invalid server number" + fullCommand + " " + e.Message);
            }
            server = ConnectToDataServer(serverNumber, false);
            String command = words[0];
            try
            {
                switch (command)
                {
                   case "FAIL":
                        toPuppet = server.Fail();
                       break;
                    case "RECOVER":
                        server = ConnectToDataServer(serverNumber, true);
                        toPuppet = server.Recover();
                        break;
                    case "FREEZE":
                        toPuppet = server.Freeze();
                        break;
                    case "UNFREEZE":
                        toPuppet = server.UnFreeze();
                        break;
                    case "DUMP":
                        toPuppet = server.Dump();
                        break;
                    default:
                        throw new CommandException("Data Proxy: Invalid Command: " + fullCommand);
                }
            }
            catch (SocketException e)
            {
                throw new CommandException("Data Proxy: SocketException: " + fullCommand + " " + e.Message);
            }
            catch (PadiException exN)
            {
                _core.DisplayMessage(exN.Description);
                return;
            }
            catch (Exception ex)
            {
                _core.DisplayMessage(ex.Message);
                return;
            }
            // Return the String receved from the server to be Displayed
            _core.DisplayMessage(toPuppet);

        }
    }
}







