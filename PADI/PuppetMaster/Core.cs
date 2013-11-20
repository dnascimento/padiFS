using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using PuppetMaster.Exceptions;
using PuppetMaster.Proxies;

namespace PuppetMaster
{
    internal static class PuppetMasterMain
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            PuppetMasterCore core = new PuppetMasterCore();
            PuppetInterface interf = new PuppetInterface(core);
            core.Interf = interf;

            //Usar para lancar as aplicacoes todas
            core.AppManager.LaunchServers();

            String  STORAGE_DIR = "C:/PADIFS/";
            String  STORAGE_DIR_TEMP = "C:/PADIFS-OLD/";
            if (Directory.Exists(STORAGE_DIR_TEMP))
            {
                Directory.Delete( STORAGE_DIR_TEMP, true );
            }
            if (!Directory.Exists(STORAGE_DIR))
            {
                Directory.CreateDirectory( STORAGE_DIR );
            }
            Directory.Move(STORAGE_DIR, STORAGE_DIR_TEMP);
            Directory.CreateDirectory( STORAGE_DIR );


            //Manter comentado para lancar pelos comandos
            //Usar em caso de debug de 1 instancia cada
            //core.AppManager.LoadLocalServers();
            Application.Run(interf);
        }
    }


    public class PuppetMasterCore
    {
        public PuppetInterface Interf;
        public Queue<String> CommandList;
        private MetadataProxy _metaProxy;
        private ClientProxy _clientProxy;
        private DataProxy _dataProxy;
        public ApplicationManager AppManager;

        public PuppetMasterCore()
        {
            _metaProxy = new MetadataProxy(this);
            _clientProxy = new ClientProxy(this);
            _dataProxy = new DataProxy(this);
            AppManager = new  ApplicationManager();


            TcpChannel channel = new TcpChannel( );
            try
            {
                ChannelServices.RegisterChannel(channel, false);
            }
            catch (Exception)
            {

            }
        }

        public void DisplayMessage(String msg)
        {
            Interf.SetLogStatus(msg);
        }

        public void RunAllCommands()
        {
            //This cicle is broken by exception     
            while (true)
            {
                RunNextCommand();
            }
        }

        public void RunNextCommand()
        {
            try
            {
               if (CommandList == null)
                   throw new CommandException("No Command list loaded");

                ExecuteCommand(CommandList.Dequeue());
            }
            catch (InvalidOperationException)
            {
                Interf.DisableControlButtons();
                throw new CommandException("All commands done");
            }
        }

        /// <summary>
        /// COMMAND: NEW CLIENT (ID) (IP) (PORT)
        /// COMMAND: NEW METASERVER (ID)
        /// COMMAND: NEW DATASERVER (ID) (IP) (PORT)
        /// </summary>
        /// <param name="commandElements"></param>
        private void CreateProcessCommand(String[] commandElements)
        {
        try
            {
            int id = Convert.ToInt32( commandElements[2] );
            switch ( commandElements[1] )
                {
                case "CLIENT":
                    String ip = commandElements[3];
                    AppManager.AddNewClient(ip);
                    break;
                case "METASERVER":
                    AppManager.AddNewMetaserver( id );
                    break;
                case "DATASERVER":
                    String hostname = commandElements[3];
                    AppManager.AddNewDataserver(hostname);
                    break;
                default:
                    throw new CommandException( "Invalid NEW Command" );
                }
            }
        catch ( FormatException )
            {
            throw new CommandException( "Invalid NEW Command" );
            }
        catch ( IndexOutOfRangeException )
            {
            throw new CommandException( "Invalid NEW Command" );
            }
        }



        public void ExecuteCommand(String command)
        {
                
        
            if (command.Length < 1){
                Interf.SetLogStatus("Empty Command");
                 return;
              }
            //Igonore comment lines
            if (command.Substring(0, 1).Equals("#"))
            {
                Interf.SetLogStatus(command);
                return;
            }
            String[] words = command.Split(' ', ',');
            String process = words[1];
            String processType = words[1].Split('-')[0];
            try
            {
                if (words[0].Equals("NEW"))
                {
                    CreateProcessCommand(words);
                    return;
                }
                switch (processType)
                {
                        // metadataserver
                    case "m":
                        //Interf.SetLogStatus("Command " + words[0] + "  sent to " + words[1] + ".");
                        _metaProxy.NewCommand(command);
                        break;
                        //Dataservers
                    case "d":
                        _dataProxy.NewCommand(command);
                        break;
                        //clients
                    case "c":
                        _clientProxy.NewCommand(command);
                        break;
                    default:
                        throw new CommandException("Invalid Command Type");
                }
            }
            catch (CommandException e)
            {
                this.DisplayMessage(e.Meessages);
            }
            catch (Exception ex)
            {
                this.DisplayMessage( ex.Message );

            }
        }

        /////////////////////////////////////////// Ler ficheiros /////////////////////////////////////////////
        public Queue<String> ReadFileName(String filename)
        {
            //String path = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Desktop ), filename );
            String path = Environment.CurrentDirectory + "\\" + filename;
            bool fileExists = File.Exists(path);
            if (File.Exists(path))
            {
                Stream stream = File.OpenRead(path);
                StreamReader file = new StreamReader(stream);
                 Queue<String> commandList = new Queue<string>();
              try
            {
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    //Insert Commands in FIFO Queue
                    commandList.Enqueue(line);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read the file: " + ex.Message);
            }
            finally
            {
                file.Close();
            }
            return commandList;
            }
             throw  new CommandException("File doesnt exists: "+path);
        }

  




    }
}