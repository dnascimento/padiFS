using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PuppetMaster.Exceptions;
using SharedLib.Exceptions;

namespace PuppetMaster
{
    public partial class PuppetInterface : Form
    {
        delegate void ShowMessageDelegate(String msg);

        private delegate void DisableCommandsDelegate();

        public PuppetMasterCore Puppet;

        public PuppetInterface(PuppetMasterCore puppet)
        {
            InitializeComponent();
            Puppet = puppet;
        }

        private void PuppetMaster_Load(object sender, EventArgs e)
        {
            OpenDefaultCommandFile();
        }

        private void ButtonExecute_Click(object sender, EventArgs e)
        {
            try
            {
                Puppet.ExecuteCommand(TextBoxManualCommand.Text);
            }
            catch (CommandException error)
            {
                SetLogStatus(error.Meessages);
            }
            catch (PadiException ex)
            {
                SetLogStatus(ex.Description);
            }
            catch (Exception ex)
            {
                SetLogStatus(ex.Message);
            }


        }

        public void DisableControlButtons()
        {
            if (this.InvokeRequired == false)
            {
                DisableRunButton();
                DisableNextStepButton();
            }
            else
            {
                DisableCommandsDelegate
                del
                     = new DisableCommandsDelegate(DisableControlButtons);
                del.Invoke();
            }
        }
        public void SetLogStatus(String status)
        {
            if (this.InvokeRequired == false)
            {
                TexboxLogStatus.Text = TexboxLogStatus.Text + status + "\r\n";

                // Scrolls Down
                TexboxLogStatus.SelectionStart = TexboxLogStatus.Text.Length;
                TexboxLogStatus.ScrollToCaret();
                TexboxLogStatus.Refresh();
            }
            else
            {
                ShowMessageDelegate showMessage = new ShowMessageDelegate(SetLogStatus);
                this.Invoke(showMessage, new object[] { status });
            }
        }

        private void TextBoxManualCommand_MouseClick(object sender, MouseEventArgs e)
        {
            //TextBoxManualCommand.Text = "";
        }

        public void DisableNextStepButton()
        {
            try
            {
                ButtonNextStep.Enabled = false;
            }
            catch (CommandException ex)
            {
                SetLogStatus(ex.Message);
            }
        }

        // file load, parse to the form and save the commands
        private void ButtonLoadScript_Click(object sender, EventArgs e)
        {
            Stream fileStream = null;
            StreamReader file = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = System.Environment.SpecialFolder.Desktop.ToString();
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files(*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileStream = openFileDialog1.OpenFile();
                file = new StreamReader(fileStream);
                this.Puppet.CommandList = ReadCommandFile(file);
            }
        }



         private Queue<String> ReadCommandFile( StreamReader file ){
            Queue<String> commandList = new Queue<string>();
            try
            {
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    //Insert Commands in FIFO Queue
                    commandList.Enqueue(line);
                }
                    String filename = openFileDialog1.FileName;
                    LabelOpenFile.Text = filename;
                    ButtonRun.Enabled = true;
                    ButtonNextStep.Enabled = true;
                    StringBuilder builder = new StringBuilder( );
                    foreach ( String command in commandList )
                        {
                        builder.AppendLine( command );
                        }
                    TextBoxCommandList.Text = builder.ToString( );
                }
            catch ( Exception ex )
                {
                MessageBox.Show( "Error: Could not read the file: " + ex.Message );
                }
            finally
                {
                file.Close( );
                }
            return commandList;
            }




         public void OpenDefaultCommandFile( )
             {
             String path = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Desktop ), "commands.txt" );
             bool fileExists = File.Exists( path );
             if ( File.Exists( path ) )
                 {
                 Stream stream = File.OpenRead( path );
                 StreamReader file = new StreamReader( stream );
                 this.Puppet.CommandList = ReadCommandFile( file );
                 }
             }














        private void ButtonRun_Click(object sender, EventArgs e)
        {
            try
            {
                Puppet.RunAllCommands();
            }
            catch (CommandException error)
            {
                SetLogStatus(error.Meessages);
            }
            catch (PadiException ex)
            {
                SetLogStatus(ex.Description);
            }
            catch (Exception ex)
            {
                SetLogStatus(ex.Message);
            }
        }

        private void ButtonNextStep_Click(object sender, EventArgs e)
        {
            try
            {
                ButtonNextStep.Enabled = false;
                Puppet.RunNextCommand();


            }
            catch (CommandException error)
            {
                SetLogStatus(error.Meessages);
            }
            catch (PadiException ex)
            {
                SetLogStatus(ex.Description);
            }
            catch (Exception ex)
            {
                SetLogStatus(ex.Message);
            }
            finally
            {
                // Remove a primeira linha
                string[] lines = TextBoxCommandList.Lines;
                if (lines.Length > 0)
                {
                    string[] lines2 = new string[lines.Length - 1];
                    for (int x = 1; x < lines.Length; x++)
                    {
                        lines2[x - 1] = lines[x];
                    }
                    TextBoxCommandList.Lines = lines2;
                    ButtonNextStep.Enabled = true;
                    LabelOpenFile.Text = "->" + lines[0];
                }
            }

        }

        public void DisableRunButton()
        {
            ButtonRun.Enabled = false;
        }

        private void TextBoxManualCommand_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                try
                {
                    Puppet.ExecuteCommand(TextBoxManualCommand.Text);
                }
                catch (CommandException error)
                {
                    SetLogStatus(error.Meessages);
                }
                catch (PadiException ex)
                {
                    SetLogStatus(ex.Description);
                }
                catch (Exception ex)
                {
                    SetLogStatus(ex.Message);
                }
            }
        }

        private void TexboxLogStatus_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBoxManualCommand_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
        // add meta 0
        private void buttonAddMeta_Click(object sender, EventArgs e)
        {
            Puppet.ExecuteCommand("NEW METASERVER 0");
            SetLogStatus("Added MetaDataServer 0");
            buttonAddMeta0.Enabled = false;
        }

        private void buttonAddMeta1_Click(object sender, EventArgs e)
        {
            Puppet.ExecuteCommand("NEW METASERVER 1");
            SetLogStatus("Added MetaDataServer 1");
            buttonAddMeta1.Enabled = false;
        }

        private void buttonAddMeta2_Click(object sender, EventArgs e)
        {
            Puppet.ExecuteCommand("NEW METASERVER 2");
            SetLogStatus("Added MetaDataServer 2");
            buttonAddMeta2.Enabled = false;
        }

        private void LabelOpenFile_Click(object sender, EventArgs e)
        {

        }

        private void buttonAddData1_Click(object sender, EventArgs e)
        {
            Puppet.ExecuteCommand("NEW DATASERVER 1 localhost 8030");
            SetLogStatus("Added DataServer");
        }

        private void buttonAddClient1_Click(object sender, EventArgs e)
        {
            Puppet.ExecuteCommand("NEW CLIENT 1 localhost 8090");
            SetLogStatus("Added Client");
        }

        public delegate void ExecAssyncCommand(String comd);

        public void ExecuteAssyncCommand(String command)
        {
            ExecAssyncCommand nc = new ExecAssyncCommand(Puppet.ExecuteCommand);
            nc.BeginInvoke(command, null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ExecuteAssyncCommand(TextBoxManualCommand.Text);
            }
            catch (CommandException error)
            {
                SetLogStatus(error.Meessages);
            }
            catch (PadiException ex)
            {
                SetLogStatus(ex.Description);
            }
            catch (Exception ex)
            {
                SetLogStatus(ex.Message);
            }
        }


        public delegate void ExecAssyncStepCommand();

        public void AssyncStepCommand()
        {
            ExecAssyncStepCommand nc = new ExecAssyncStepCommand(Puppet.RunNextCommand);
            nc.BeginInvoke(null, null);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                AssyncStepCommand();
            }
            catch (CommandException error)
            {
                SetLogStatus(error.Meessages);
            }
            catch (PadiException ex)
            {
                SetLogStatus(ex.Description);
            }
            catch (Exception ex)
            {
                SetLogStatus(ex.Message);
            }
            finally
            {
                // Remove a primeira linha
                string[] lines = TextBoxCommandList.Lines;
                if (lines.Length > 0)
                {
                    string[] lines2 = new string[lines.Length - 1];
                    for (int x = 1; x < lines.Length; x++)
                    {
                        lines2[x - 1] = lines[x];
                    }
                    TextBoxCommandList.Lines = lines2;
                    LabelOpenFile.Text = "->" + lines[0];
                }
            }
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void button2_Click( object sender, EventArgs e )
            {
            //Clean PADI-FS directory content
            String STORAGE_DIR = "C:/PADIFS/";
            String STORAGE_DIR_TEMP = "C:/PADIFS-OLD/";
            if (!Directory.Exists(STORAGE_DIR_TEMP))
            {
                SetLogStatus( "No old state to load" );
                return;
            }
            if (Directory.Exists(STORAGE_DIR))
            {
                Directory.Delete( STORAGE_DIR, true );   
            }
            Directory.Move( STORAGE_DIR_TEMP, STORAGE_DIR );
            SetLogStatus("Old state LOADED");

            }
    }
}
