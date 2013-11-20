using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using PuppetMaster.Exceptions;
using SharedLib;
using SharedLib.Exceptions;


namespace PuppetMaster.Proxies
{
    public class MetadataProxy
    {
        private PuppetMasterCore _core;

        public MetadataProxy(PuppetMasterCore core)
        {
            _core = core;
            RemotingConfiguration.Configure("../../App.config", true);
 
        }

        public IRecovery ConnectToRecovery(int metaServerNumber)
        {
             String hostname;
            int port;
            if ( !_core.AppManager.GetProcessIPandPort( ProcessType.MetaServer, metaServerNumber, true, out hostname, out port ) )
            throw new CommandException( "Can not get metaServer IP and port. Metaserver: " + metaServerNumber );

            IRecovery server =
                (IRecovery) Activator.GetObject( typeof( IRecovery ), "tcp://" + hostname + ":" + port + "/PADIConnection" );
            return server;
        }

        public IMetaToPuppet ConnectToMetaDataServer(int metaServerNumber,Boolean recovery = false)
        {
            String hostname;
            int port;
            if (!_core.AppManager.GetProcessIPandPort(ProcessType.MetaServer, metaServerNumber,false, out hostname, out port))
                throw new CommandException("Can not get metaServer IP and port. Metaserver: " + metaServerNumber);

            IMetaToPuppet server = null;
            try
            {
                server = (IMetaToPuppet)Activator.GetObject(
                    typeof(IMetaToPuppet), "tcp://" + hostname + ":" + port + "/PADIConnection");
                return server;
            }
            catch (Exception e)
            {
                throw new CommandException("Error connecting to metaserver" + e.Message);
            }
        }


        public void NewCommand(string fullCommand)
        {
            String[] words = fullCommand.Split(' ');
            String res = words[1];
            String[] res2 = res.Split('-');
            IMetaToPuppet server;
            IRecovery recover;
            int serverNumber;
            String toPuppet;
            try
            {
                serverNumber = Convert.ToInt32(res2[1]);
                server = ConnectToMetaDataServer(serverNumber);
                recover = ConnectToRecovery(serverNumber);
            }
            catch (CommandException ex)
            {
                throw  ex;
            }
            catch (Exception e)
            {
                throw new CommandException("MetaData Proxy: Invalid server number" + fullCommand + " " + e.Message);
            }


            String command = words[0];
            try
            {
                switch (command.ToUpper())
                {
                    case "DUMP":
                        toPuppet = server.Dump();
                        break;
                    case "FAIL":
                        toPuppet = server.Fail();
                        break;
                    case "RECOVER":
                        toPuppet = recover.Recover();
                        break;

                    default:
                        throw new CommandException("MetaData Proxy: Invalid Command: " + fullCommand);
                }
            }
            catch (SocketException e)
            {
                throw new CommandException("MetaData Proxy: SocketException: " + fullCommand + " " + e.Message);
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
