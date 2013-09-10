using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using SharedLib;
using SharedLib.DataserverObjects;
using SharedLib.Exceptions;

namespace DataServer
{
    class StorageManager
    {
        private String STORAGE_DIR = null;

        public StorageManager(int id) 
        {
            STORAGE_DIR = "C:/PADIFS/ServerId-" + id + "/";
        }

        public void DeleteFile(String filename)
        {
            String path = STORAGE_DIR + filename;

            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (IOException)
                {
                    throw new PadiException(PadiExceptiontType.DeleteFile,
                                            "StorageManager: Can't delete file at: " + path);
                }
            }
            else
            {
                throw new PadiException(PadiExceptiontType.DeleteFile, "StorageManager: Delete : File doesnt exist " + path);
            }
        }


        public TFile ReadFile(String filename)
        {
            String path = STORAGE_DIR + filename;
            TFile outFile = null;

            //If read a file that
            if (!System.IO.File.Exists(path))
            {
                throw new Exception("Storage: Read: File doesnt exist");
            }

            FileStream fs = new FileStream(path, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();

            try
            {
                outFile = (TFile)formatter.Deserialize(fs);
                String txt = Encoding.ASCII.GetString(outFile.Data);
                //Console.WriteLine("Storage: File readed: " + filename+" content: " + txt);
            }
            catch (SerializationException)
            {
                Console.WriteLine("Storage: Failed to read the file: " + path);
            }
            finally
            {
                fs.Close();
            }

            return outFile;
        }

        public void WriteFile(String filename, TFile data)
        {
            System.IO.Directory.CreateDirectory(STORAGE_DIR);
            filename = STORAGE_DIR + filename;
            BinaryFormatter formatter = new BinaryFormatter();

            FileStream fs = new FileStream(filename, FileMode.Create);
            
            try
            {
                lock (this)
                {
                    formatter.Serialize(fs, data);
                }
                String txt = Encoding.ASCII.GetString(data.Data);
                //Console.WriteLine("Storage: File Write: " + filename + " content: " + txt);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Storage: Failed to write File " + e.Message);
            }
            finally
            {
                fs.Close();
            }
        }

    }


  


}
