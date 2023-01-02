using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.DirectoryServices.ActiveDirectory;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Math;
using System.Globalization;
using System.Security.Policy;
using System.Reflection;
using Org.BouncyCastle.Cms;

namespace AliSign
{
    public partial class Form1 : Form
    {
        public const string REG_VALUE_APP_VERSION = "AppVersion";
        public const string APP_REGISTRY_VERSION = "0.1"; // change this will clear all registry of the project.

        public const string REG_VALUE_WORKING_PATH_LIST = "WorkingPathList";
        public const string REG_VALUE_WORKING_PATH_SELECTEDINDEX = "WorkingPathSelected";
        public const string REG_VALUE_SIGN_TAB_INDEX = "SignTabIndex";

        public const string REG_VALUE_IMAGE_BIOS_PATH = "ImageBiosPath";
        public const string REG_VALUE_IMAGE_OUTPUT_BIOS_PATH = "ImageOutputBiosPath";
        public const string REG_VALUE_PRIVATE_KEY_PATH = "DsaPrivateKeyPath";
        public const string REG_VALUE_UBIOS_VERSION = "UbiosVersion";
        public const string REG_VALUE_UBIOS_PUBLIC_KEY_PATH = "UbiosPublickeyPath";
        public const string REG_VALUE_UBC_PUBLIC_KEY_PATH = "UbcPublicKeyPath";
        public const string REG_VALUE_BOOT_LOADER_PUBLIC_KEY_PATH = "BootLoaderPublicKeyPath";
        public const string REG_VALUE_HASH_PATH_LIST = "HashPathList";

        public const string REG_VALUE_IMAGE_DISK_PATH = "ImageDiskPath";
        public const string REG_VALUE_IMAGE_OUTPUT_DISK_PATH = "ImageOutputDiskPath";

        public const string REG_VALUE_IMAGE_UBC_PATH = "ImageUbcPath";
        public const string REG_VALUE_IMAGE_OUTPUT_UBC_PATH = "ImageOutputUbcPath";

        public const int MAX_WORKING_FOLDER_SAVED = 5;

        public const int HASH_SIZE = 20;
        public const int SIGNATURE_SIZE = 40;
        // TODO: confirm the size of the DSA private key
        public const int PRIVATE_KEY_SIZE = 684;
        public const int PRIVATE_KEY_SIZE_2 = 672;
        public const int PUBLIC_KEY_SIZE = 404;
        public const int ROM_FILE_SIZE = 0x800000;
        public const string DEFAULT_VERSION_STRING = "UBIOS Version: 8.00.0";

        public const int OFFSET_HASH_LIST_START = 0x37c;
        public const int OFFSET_HASH_LIST_END_PLUS1 = 0x10000;
        public const int OFFSET_UBC_PUBLIC_KEY = 0x03c;
        public const int OFFSET_BOOT_LOADER_PUBLIC_KEY = 0x01d0;
        public const int OFFSET_UBIOS_VERSION = 0x0364;
        public const int OFFSET_UBIOS_PUBLIC_KEY = 0x7f8020;

        public const int MAX_HASH_PATH_COUNT = (OFFSET_HASH_LIST_END_PLUS1 - OFFSET_HASH_LIST_START) / HASH_SIZE;

        public string registryAppKey;
        public string registryAppSubKey;
        public string registryCompanyName;

        // Common encripto variables
        IDigest hashFunction;
        IDsa signer;
        // for BIOS sign
        public byte[] BytesImageBios;
        public int identificationAlignment = 16;
        public bool is1stTime = true;
        // for Disk sign
        public byte[] BytesImageDisk;
        // for UBC sign
        public byte[] BytesImageUbc;

        private void SaveComboSettings(RegistryKey appKey, string keyName, System.Windows.Forms.ComboBox comboBox, int maxCount)
        {
            RegistryKey subKey = appKey.OpenSubKey(keyName, true);
            if (subKey != null)
            {
                appKey.DeleteSubKeyTree(keyName);
            }
            subKey = appKey.CreateSubKey(keyName);
            var i = 0;
            foreach (var item in comboBox.Items)
            {
                subKey.SetValue(i++.ToString(), item);
            }
        }

        private void SaveListSettings(RegistryKey appKey, string keyName, System.Windows.Forms.ListBox listBox, int maxCount)
        {
            RegistryKey subKey = appKey.OpenSubKey(keyName, true);
            if (subKey != null)
            {
                appKey.DeleteSubKeyTree(keyName);
            }
            subKey = appKey.CreateSubKey(keyName);

            var i = 0;
            foreach (var hashfilepath in listBox.Items)
            {
                subKey.SetValue(i++.ToString(), hashfilepath);
            }

        }

        private void SaveSettings()
        {
            RegistryKey appKey = Registry.CurrentUser.CreateSubKey(registryAppSubKey);

            appKey.SetValue(REG_VALUE_WORKING_PATH_SELECTEDINDEX, comboBoxWorkingFolder.SelectedIndex);
            appKey.SetValue(REG_VALUE_SIGN_TAB_INDEX, tabControlSign.SelectedIndex);
            appKey.SetValue(REG_VALUE_IMAGE_BIOS_PATH, textBoxImageBios.Text);
            appKey.SetValue(REG_VALUE_IMAGE_OUTPUT_BIOS_PATH, textBoxOutputImageBios.Text);
            appKey.SetValue(REG_VALUE_PRIVATE_KEY_PATH, textBoxDsaPrivateKey.Text);
            appKey.SetValue(REG_VALUE_UBIOS_VERSION, textBoxUbiosVersion.Text);
            appKey.SetValue(REG_VALUE_UBIOS_PUBLIC_KEY_PATH, textBoxUbiosPublicKey.Text);
            appKey.SetValue(REG_VALUE_UBC_PUBLIC_KEY_PATH, textBoxUbcPublicKey.Text);
            appKey.SetValue(REG_VALUE_BOOT_LOADER_PUBLIC_KEY_PATH, textBoxBootLoaderPublicKey.Text);
            //
            // Save comboBox Lists
            // 
            SaveComboSettings(appKey, REG_VALUE_WORKING_PATH_LIST, comboBoxWorkingFolder, MAX_WORKING_FOLDER_SAVED);
            SaveListSettings(appKey, REG_VALUE_HASH_PATH_LIST, listBoxHash, MAX_HASH_PATH_COUNT);

            appKey.SetValue(REG_VALUE_IMAGE_DISK_PATH, textBoxImageDisk.Text);
            appKey.SetValue(REG_VALUE_IMAGE_OUTPUT_DISK_PATH, textBoxOutputImageDisk.Text);

            appKey.SetValue(REG_VALUE_IMAGE_UBC_PATH, textBoxImageUbc.Text);
            appKey.SetValue(REG_VALUE_IMAGE_OUTPUT_UBC_PATH, textBoxOutputImageUbc.Text);
        }

        private void RestoreComboSettings(RegistryKey appKey, string keyName, System.Windows.Forms.ComboBox comboBox, int maxCount)
        {
            RegistryKey subKey = appKey.OpenSubKey(keyName);
            if (subKey != null)
            {
                for (int i = 0; i < maxCount; i++)
                {
                    string PathName = (string)subKey.GetValue(i.ToString());
                    if (String.IsNullOrEmpty(PathName)) break;
                    comboBox.Items.Add(PathName);
                }
            }
        }

        private void RestoreListSettings(RegistryKey appKey, string keyName, System.Windows.Forms.ListBox listBox, int maxCount)
        {
            RegistryKey subKey = appKey.OpenSubKey(keyName);
            if (subKey != null)
            {
                for (int i = 0; i < maxCount; i++)
                {
                    string PathName = (string)subKey.GetValue(i.ToString());
                    if (String.IsNullOrEmpty(PathName)) break;
                    listBox.Items.Add(PathName);
                }
            }
        }

        private void RestoreSettings()
        {
            registryAppKey = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            registryCompanyName = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            registryAppSubKey = "Software\\" + registryCompanyName + "\\" + registryAppKey;
            //
            // Retrieve Registry keys
            //
            RegistryKey appKey = Registry.CurrentUser.OpenSubKey(registryAppSubKey);
            if (appKey == null)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\" + registryAppSubKey, REG_VALUE_APP_VERSION, APP_REGISTRY_VERSION);
                return;
            }
            string Str = (string)appKey.GetValue(REG_VALUE_APP_VERSION, "0.0");
            if (String.Compare(APP_REGISTRY_VERSION, Str) != 0)
            {
                RegistryKey appFamilyKey = Registry.CurrentUser.OpenSubKey("Software\\" + registryCompanyName, true);
                if (appFamilyKey != null)
                {
                    appFamilyKey.DeleteSubKeyTree(registryAppKey);
                    Registry.SetValue("HKEY_CURRENT_USER\\" + registryAppSubKey, REG_VALUE_APP_VERSION, APP_REGISTRY_VERSION);
                }
                return;
            }
            //
            // Restore lists
            //
            RestoreComboSettings(appKey, REG_VALUE_WORKING_PATH_LIST, comboBoxWorkingFolder, MAX_WORKING_FOLDER_SAVED);
            RestoreListSettings(appKey, REG_VALUE_HASH_PATH_LIST, listBoxHash, MAX_HASH_PATH_COUNT);
            //
            // Restore controls
            //
            comboBoxWorkingFolder.SelectedIndex = (int)appKey.GetValue(REG_VALUE_WORKING_PATH_SELECTEDINDEX, 0);
            tabControlSign.SelectedIndex = (int)appKey.GetValue(REG_VALUE_SIGN_TAB_INDEX, 0);
            textBoxImageBios.Text = (string)appKey.GetValue(REG_VALUE_IMAGE_BIOS_PATH, "");
            textBoxOutputImageBios.Text = (string)appKey.GetValue(REG_VALUE_IMAGE_OUTPUT_BIOS_PATH, "");
            textBoxDsaPrivateKey.Text = (string)appKey.GetValue(REG_VALUE_PRIVATE_KEY_PATH, "");
            textBoxUbiosVersion.Text = (string)appKey.GetValue(REG_VALUE_UBIOS_VERSION, "");
            textBoxUbiosPublicKey.Text = (string)appKey.GetValue(REG_VALUE_UBIOS_PUBLIC_KEY_PATH, "");
            textBoxUbcPublicKey.Text = (string)appKey.GetValue(REG_VALUE_UBC_PUBLIC_KEY_PATH, "");
            textBoxBootLoaderPublicKey.Text = (string)appKey.GetValue(REG_VALUE_BOOT_LOADER_PUBLIC_KEY_PATH, "");

            textBoxImageDisk.Text = (string)appKey.GetValue(REG_VALUE_IMAGE_DISK_PATH, "");
            textBoxOutputImageDisk.Text = (string)appKey.GetValue(REG_VALUE_IMAGE_OUTPUT_DISK_PATH, "");

            textBoxImageUbc.Text = (string)appKey.GetValue(REG_VALUE_IMAGE_UBC_PATH, "");
            textBoxOutputImageUbc.Text = (string)appKey.GetValue(REG_VALUE_IMAGE_OUTPUT_UBC_PATH, "");
        }

        public Form1()
        {
            InitializeComponent();
            RestoreSettings();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                // save location and size if the state is normal
                Properties.Settings.Default.F1Location = this.Location;
                Properties.Settings.Default.F1Size = this.Size;
            }
            else
            {
                // save the RestoreBounds if the form is minimized or maximized!
                Properties.Settings.Default.F1Location = this.RestoreBounds.Location;
                Properties.Settings.Default.F1Size = this.RestoreBounds.Size;
            }

            // don't forget to save the settings
            Properties.Settings.Default.Save();

            SaveSettings();
        }

        private long searchBytes(byte[] needle)
        {
            if (BytesImageBios == null)
            {
                return -1;
            }
            var len = needle.Length;
            var limit = BytesImageBios.Length - len;
            for (var i = 0; i <= limit; i += identificationAlignment)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != BytesImageBios[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        private bool isValidImageBios()
        {
            //
            // Validation ADLink identifications
            //
            byte[] AdlinkBiosIdentification1 = Encoding.ASCII.GetBytes("BIOS_MADE_BY_ADLINK");
            //byte[] AdlinkBiosIdentification1 = Encoding.ASCII.GetBytes("BIOS_MADE_BY_ADLINX");
            var AdlinkBiosIdentification2 = new byte[] { 0x0e, 0x0e, 0x15, 0x23, 0x3e, 0x25, 0x1e, 0x5f, 0x04, 0x58, 0x01, 0x44, 0x57, 0x18, 0x14, 0x61 };
            //var AdlinkBiosIdentification2 = new byte[] { 0x0e, 0x0e, 0x15, 0x23, 0x3e, 0x25, 0x1e, 0x5f, 0x04, 0x58, 0x01, 0x44, 0x57, 0x18, 0x14, 0x60 };
            if (searchBytes(AdlinkBiosIdentification1) == -1)
            {
                //MessageBox.Show("This ROM image is not supported 1.");
                return false;
            }
            if (searchBytes(AdlinkBiosIdentification2) == -1)
            {
                //MessageBox.Show("This ROM image is not supported 2.");
                return false;
            }
            //
            // Validate signature 90 90 E9
            //
            byte[] validSignature = new byte[] { 0x90, 0x90, 0xe9 };
            //byte[] validSignature = new byte[] { 0x90, 0x90, 0xe8 };
            var len = validSignature.Length;
            var validSignarueOffset = BytesImageBios.Length - 16;
            var i = 0;
            for (; i < len; i++)
            {
                if (validSignature[i] != BytesImageBios[i + validSignarueOffset]) break;
            }
            if (i < len)
            {
                //MessageBox.Show("Incorrect file format: ROM Image not ended with 90 90 E9 ....");
                return false;
            }

            return true;
        }

        private void enableControlsBios(bool isValid)
        {
            textBoxOutputImageBios.Enabled = isValid;
            buttonOutputImageBios.Enabled = isValid;
            textBoxDsaPrivateKey.Enabled = isValid;
            buttonDsaPrivateKey.Enabled = isValid;
            textBoxUbiosVersion.Enabled = isValid;
            textBoxUbiosPublicKey.Enabled = isValid;
            buttonUbiosPublicKey.Enabled = isValid;
            textBoxUbcPublicKey.Enabled = isValid;
            buttonUbcPublicKey.Enabled = isValid;
            textBoxBootLoaderPublicKey.Enabled = isValid;
            buttonBootLoaderPublicKey.Enabled = isValid;
            buttonHashAdd.Enabled = isValid;
            buttonHashRemove.Enabled = isValid;
            listBoxHash.Enabled = isValid;
            buttonSignBios.Enabled = isValid;
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.F1Size.Width == 0 || Properties.Settings.Default.F1Size.Height == 0)
            {
                // first start
                // optional: add default values
            }
            else
            {
                //this.WindowState = Properties.Settings.Default.F1State;

                // we don't want a minimized window at startup
                if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

                this.Location = Properties.Settings.Default.F1Location;
                this.Size = Properties.Settings.Default.F1Size;
            }

            // update title text
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string projectName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            this.Text = projectName + " " + assemblyVersion;
        }

        private void textBoxImageBios_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(textBoxImageBios.Text))
            {
                //
                // Read Image file to bytes[] BytesImageBios
                //
                BytesImageBios = File.ReadAllBytes(textBoxImageBios.Text);
                //
                // support old project which size is 4MB and the identification is not 16 bytes aligned
                //
                //if (BytesImageBios.Length >= 0x800000)
                //{
                //    identificationAlignment = 16;
                //}
                //else
                //{
                //    identificationAlignment = 1;
                //}
                if (textBoxOutputImageBios.Text.Length == 0)
                {
                    textBoxOutputImageBios.Text = textBoxImageBios.Text;
                }
                enableControlsBios(isValidImageBios());
            }
            else
            {
                enableControlsBios(false);
            }
        }


        private string buttonFilePath_Click(string filePath)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filePath = this.openFileDialog1.FileName;
            }
            return filePath;
        }

        private void buttonRomImage_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.InitialDirectory = comboBoxWorkingFolder.Text;
            textBoxImageBios.Text = buttonFilePath_Click(textBoxImageBios.Text);
        }

        private void buttonDsaPrivateKey_Click(object sender, EventArgs e)
        {
            textBoxDsaPrivateKey.Text = buttonFilePath_Click(textBoxDsaPrivateKey.Text);
        }

        private void buttonUbiosPublicKey_Click(object sender, EventArgs e)
        {
            textBoxUbiosPublicKey.Text = buttonFilePath_Click(textBoxUbiosPublicKey.Text);
        }

        private void buttonUbcPublicKey_Click(object sender, EventArgs e)
        {
            textBoxUbcPublicKey.Text = buttonFilePath_Click(textBoxUbcPublicKey.Text);
        }

        private void buttonBootLoaderPublicKey_Click(object sender, EventArgs e)
        {
            textBoxBootLoaderPublicKey.Text = buttonFilePath_Click(textBoxBootLoaderPublicKey.Text);
        }

        private void buttonHashAdd_Click(object sender, EventArgs e)
        {
            if (listBoxHash.Items.Count == 0)
            {
                var files = Directory.GetFiles(comboBoxWorkingFolder.Text, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    var info = new FileInfo(file);
                    if (info.Length == HASH_SIZE)
                    {
                        listBoxHash.Items.Add(info.FullName);
                    }
                }
            }
            else
            {
                string hash_fp = "";
                hash_fp = buttonFilePath_Click(hash_fp);
                if (!String.IsNullOrEmpty(hash_fp))
                {
                    var info = new FileInfo(hash_fp);
                    if (info.Length == HASH_SIZE)
                    {
                        listBoxHash.Items.Add(hash_fp);
                    }
                    else
                    {
                        MessageBox.Show("Hash file size is limited to 20 bytes");
                    }
                }
            }
        }

        private void buttonHashRemove_Click(object sender, EventArgs e)
        {
            if (listBoxHash.SelectedItems.Count > 0)
            {
                while (listBoxHash.SelectedItems.Count > 0)
                {
                    listBoxHash.Items.Remove(listBoxHash.SelectedItems[0]);
                }
            }
        }

        private bool UpdateComboBox(System.Windows.Forms.ComboBox comboBox, string str, bool insert)
        {
            int index;
            if (!Directory.Exists(comboBoxWorkingFolder.Text))
            {
                return false;
            }
            if (insert)
            {
                index = comboBox.FindStringExact(str);
            }
            else
            {
                index = comboBox.FindString(str);
            }
            if (index == -1)
            {
                if (insert)
                {
                    comboBox.Items.Insert(0, str);
                    if (comboBox.Items.Count > MAX_WORKING_FOLDER_SAVED)
                    {
                        comboBox.Items.RemoveAt(MAX_WORKING_FOLDER_SAVED);
                    }
                    comboBox.SelectedIndex = 0;
                }
                else
                {
                    comboBox.Text = "";
                }
            }
            else
            {
                comboBox.SelectedIndex = index;
            }

            return (index == -1);
        }

        private void resetInputFiles(object sender, EventArgs e)
        {
            var ImageBiosSelected = false;
            textBoxImageBios.Text = string.Empty;
            textBoxOutputImageBios.Text = string.Empty;
            textBoxDsaPrivateKey.Text = string.Empty;
            textBoxUbiosPublicKey.Text = string.Empty;
            textBoxUbcPublicKey.Text = string.Empty;
            textBoxBootLoaderPublicKey.Text = string.Empty;

            listBoxHash.Items.Clear();

            var files = Directory.GetFiles(comboBoxWorkingFolder.Text, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                var info = new FileInfo(file);
                if (info.Length == HASH_SIZE)
                {
                    listBoxHash.Items.Add(info.FullName);
                    continue;
                }
                // TODO: confirm the size of the private key
                if (info.Length == PRIVATE_KEY_SIZE || info.Length == PRIVATE_KEY_SIZE_2)
                {
                    if (String.IsNullOrEmpty(textBoxDsaPrivateKey.Text))
                    {
                        textBoxDsaPrivateKey.Text = info.FullName;
                    }
                    continue;
                }
                if (info.Length == PUBLIC_KEY_SIZE)
                {
                    if (string.IsNullOrEmpty(textBoxUbcPublicKey.Text) && info.Name.Contains("UBC", StringComparison.OrdinalIgnoreCase))
                    {
                        textBoxUbcPublicKey.Text += info.FullName;
                        continue;
                    }
                    if (string.IsNullOrEmpty(textBoxUbiosPublicKey.Text) && info.Name.Contains("UBIOS", StringComparison.OrdinalIgnoreCase))
                    {
                        textBoxUbiosPublicKey.Text += info.FullName;
                        continue;
                    }
                    if (string.IsNullOrEmpty(textBoxBootLoaderPublicKey.Text) && info.Name.Contains("MBR", StringComparison.OrdinalIgnoreCase))
                    {
                        textBoxBootLoaderPublicKey.Text += info.FullName;
                        //continue;
                    }
                    continue;
                }
                if (!ImageBiosSelected && info.Length == ROM_FILE_SIZE)
                {
                    BytesImageBios = File.ReadAllBytes(info.FullName);

                    if (isValidImageBios())
                    {
                        textBoxImageBios.Text = info.FullName;
                        ImageBiosSelected = true;
                    }
                    continue;
                }
            }
            if (String.IsNullOrEmpty(textBoxUbiosVersion.Text))
            {
                textBoxUbiosVersion.Text = DEFAULT_VERSION_STRING;
            }

            enableControlsBios(ImageBiosSelected);

        }

        private void buttonWorkingFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(comboBoxWorkingFolder.Text))
            {
                folderBrowserDialog1.SelectedPath = comboBoxWorkingFolder.Text;
            }
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                comboBoxWorkingFolder.Text = folderBrowserDialog1.SelectedPath;
            }
            comboBoxWorkingFolder_Leave(sender, e);
        }

        private void comboBoxWorkingFolder_Leave(object sender, EventArgs e)
        {
            //if (comboBoxWorkingFolder.SelectedIndex== -1)
            {
                if (UpdateComboBox(comboBoxWorkingFolder, comboBoxWorkingFolder.Text, true))
                {
                    resetInputFiles(sender, e);
                }
            }
        }

        private void comboBoxWorkingFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is1stTime)
            {
                is1stTime = false;
            }
            else
            {
                resetInputFiles(sender, e);
            }
        }

        private void textBoxDsaPrivateKey_TextChanged(object sender, EventArgs e)
        {
            string pem = File.ReadAllText(textBoxDsaPrivateKey.Text);

            // Create a PemReader to parse the PEM file
            var reader = new PemReader(new StringReader(pem));

            // Read the private key from the PEM file
            AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)reader.ReadObject();

            // Get the DSA private key from the key pair
            DsaPrivateKeyParameters dsaPrivateKey = (DsaPrivateKeyParameters)keyPair.Private;

            // Create a SHA1 hash function
            hashFunction = new Sha1Digest();

            // Create a DSA signer object
            signer = new DsaSigner();

            // Initialize the signer with the DSA private key
            signer.Init(true, dsaPrivateKey);
        }

        private byte[] bigIntegersToBytes(Org.BouncyCastle.Math.BigInteger[] bigIntArray)
        {
            byte[][] byteArrays = new byte[bigIntArray.Length][];
            for (int i = 0; i < bigIntArray.Length; i++)
            {
                byteArrays[i] = bigIntArray[i].ToByteArray();
            }

            int totalLength = byteArrays.Sum(x => x.Length);
            byte[] mergedArray = new byte[totalLength];

            int currentIndex = 0;
            foreach (byte[] array in byteArrays)
            {
                Buffer.BlockCopy(array, 0, mergedArray, currentIndex, array.Length);
                currentIndex += array.Length;
            }

            return mergedArray;
        }

        private void buttonSignBios_Click(object sender, EventArgs e)
        {
            //
            // 1. patch UBC Public key @ 0x3c (Length 0x194)
            //
            byte[] UbcPublicKey;
            if (!File.Exists(textBoxUbcPublicKey.Text)) { return; }
            UbcPublicKey = File.ReadAllBytes(textBoxUbcPublicKey.Text);
            Buffer.BlockCopy(UbcPublicKey, 0, BytesImageBios, OFFSET_UBC_PUBLIC_KEY, UbcPublicKey.Length);
            //
            // 2. patch MBR_GPT_BL_PUBLIC_KEY Public key @ 0x1d0 (Length 0x194)
            //
            byte[] BootLoaderPublicKey;
            if (!File.Exists(textBoxBootLoaderPublicKey.Text)) { return; }
            BootLoaderPublicKey = File.ReadAllBytes(textBoxBootLoaderPublicKey.Text);
            Buffer.BlockCopy(BootLoaderPublicKey, 0, BytesImageBios, OFFSET_BOOT_LOADER_PUBLIC_KEY, BootLoaderPublicKey.Length);
            //
            // 3. patch UBIOS version string @ 0x364 (length 0x18)
            //
            byte[] VersionString = new byte[0x18];
            byte[] VersionStringInput = Encoding.ASCII.GetBytes(textBoxUbiosVersion.Text);
            // copy input to target array
            Buffer.BlockCopy(VersionStringInput, 0, VersionString, 0, VersionStringInput.Length);
            // override to Image buffer
            Buffer.BlockCopy(VersionString, 0, BytesImageBios, OFFSET_UBIOS_VERSION, VersionString.Length);
            //
            // 4. patch Hash list @OFFSET_HASH_LIST_START ~ OFFSET_HASH_LIST_END_PLUS1)
            //
            Array.Clear(BytesImageBios, OFFSET_HASH_LIST_START, OFFSET_HASH_LIST_END_PLUS1 - OFFSET_HASH_LIST_START);
            byte[] hash;
            int offsetRomImage = OFFSET_HASH_LIST_START;
            foreach (string hashFile in listBoxHash.Items)
            {
                if (!File.Exists(hashFile)) { return; }
                hash = File.ReadAllBytes(hashFile);
                Buffer.BlockCopy(hash, 0, BytesImageBios, offsetRomImage, hash.Length);
                offsetRomImage += hash.Length;
                // ignore hash files after offset OFFSET_HASH_LIST_END_PLUS1
                if (offsetRomImage > OFFSET_HASH_LIST_END_PLUS1 - hash.Length) { break; }
            }
            //
            // 5. patch UBIOS public key and it's double word - byte checksum to OFFSET_UBIOS_PUBLIC_KEY
            //
            byte[] UbiosPublicKey;
            int checksum = 0;
            if (!File.Exists(textBoxUbiosPublicKey.Text)) { return; }
            UbiosPublicKey = File.ReadAllBytes(textBoxUbiosPublicKey.Text);
            for (int i = 0; i < UbiosPublicKey.Length; i++)
            {
                BytesImageBios[OFFSET_UBIOS_PUBLIC_KEY + i] = UbiosPublicKey[i];
                checksum += UbiosPublicKey[i];
            }
            // override checksum after UBIOS Public key
            byte[] bytes = BitConverter.GetBytes(checksum);
            Buffer.BlockCopy(bytes, 0, BytesImageBios, OFFSET_UBIOS_PUBLIC_KEY + UbiosPublicKey.Length, sizeof(int));
            //
            // 6. get hash and sign the blob from offset 0x3c~EOF of the image.
            //
            // Dotnet's DSA class doesn't support loading DSA private keys refer to: https://www.reddit.com/r/dotnetcore/comments/tg5pqg/creating_dsa_signature_with_private_key/
            // Switch to BouncyCastle library

            // Compute the hash of the blob
            hashFunction.BlockUpdate(BytesImageBios, 0x3c, BytesImageBios.Length - 0x3c);
            hash = new byte[hashFunction.GetDigestSize()];
            hashFunction.DoFinal(hash, 0);

            // Convert the signature to an byte array
            byte[] signature = bigIntegersToBytes(signer.GenerateSignature(hash));

            // patch signature & hash to the head of ROM Image
            Buffer.BlockCopy(signature, 0, BytesImageBios, 0, signature.Length); // length = 0x28
            Buffer.BlockCopy(hash, 0, BytesImageBios, signature.Length, hash.Length); // length = 0x14
            //
            //  7. Write to output file
            //
            try
            {
                MessageBox.Show("Write to " + textBoxOutputImageBios.Text);
                File.WriteAllBytes(textBoxOutputImageBios.Text, BytesImageBios);
            }
            catch (IOException ex)
            {
                MessageBox.Show("An error occurred while writing to the file: " + ex.Message);
            }
        }

        private void buttonImageDisk_Click(object sender, EventArgs e)
        {
            textBoxImageDisk.Text = buttonFilePath_Click(textBoxImageDisk.Text);
        }

        private void buttonOutputImageDisk_Click(object sender, EventArgs e)
        {
            textBoxOutputImageDisk.Text = buttonFilePath_Click(textBoxOutputImageDisk.Text);
        }
        //
        // constants for singing disk
        //
        public const int SIZE_DISK_SECTOR = 512;

        private void enableControlsDisk(bool isValid)
        {
            textBoxOutputImageDisk.Enabled = isValid;
            buttonOutputImageDisk.Enabled = isValid;
            buttonSignDisk.Enabled = isValid;
        }

        private bool isValidImageDisk()
        {
            //
            // is it a sectors snapshot?
            //
            if (BytesImageDisk.Length % SIZE_DISK_SECTOR != 0)
            {
                MessageBox.Show("Not a valid disk sectors' image.");
                return false;
            }
            return true;
        }

        private void textBoxImageDisk_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(textBoxImageDisk.Text))
            {
                //
                // Read Image file to bytes[] BytesImageDisk
                //
                BytesImageDisk = File.ReadAllBytes(textBoxImageDisk.Text);
                //
                // set the default output image file name
                //
                if (textBoxOutputImageDisk.Text.Length == 0)
                {
                    textBoxOutputImageDisk.Text = textBoxImageDisk.Text;
                }
                enableControlsDisk(isValidImageDisk());
            }
            else
            {
                enableControlsDisk(false);
            }
        }

        // Verify:
        //  a) 0x000 to 0x177 : (MBR Code, GRUB Stage 1)
        //  b) 0x1B4 to 0x1FF : (Boot Loader Sectors, Partition Table and Disk Signature)
        // No Verification: 
        //  a) 0x178 to 0x1B3 : Hash data and signature area

        private void buttonSignDisk_Click(object sender, EventArgs e)
        {
            byte[] signature;
            byte[] hash;
            //
            // 1. sign MBR
            // 
            byte[] sector1 = BytesImageDisk[0..(SIZE_DISK_SECTOR - 1)]; // 1st sector

            short BL_Ss = 1; // GRUB Boot Loader Start Sector; 
            Buffer.BlockCopy(BitConverter.GetBytes(BL_Ss), 0, sector1, 0x1b4, sizeof(short));

            short BL_Si = (short)(BitConverter.ToInt16(sector1, 0x1c6) - 2); // BL Si = GRUB Boot Loader Sector Size
            Buffer.BlockCopy(BitConverter.GetBytes(BL_Ss), 0, sector1, 0x1b6, sizeof(short));
            // assemble a blobMbr
            byte[] blobMbr = BytesImageDisk[0..0x177].Concat(sector1[0x1b4..]).ToArray();

            // Compute the hash of the blobMbr
            hashFunction.BlockUpdate(blobMbr, 0, blobMbr.Length);
            hash = new byte[hashFunction.GetDigestSize()];
            hashFunction.DoFinal(hash, 0);
            // Convert the signature to an byte array
            signature = bigIntegersToBytes(signer.GenerateSignature(hash));

            // patch signature & hash
            Buffer.BlockCopy(signature, 0, BytesImageDisk, 0x178, signature.Length); // length = 0x28
            Buffer.BlockCopy(hash, 0, BytesImageDisk, 0x178 + signature.Length, hash.Length); // length = 0x14
            //
            // 2. sign GRUB
            //
            byte[] blobGrub = BytesImageDisk[SIZE_DISK_SECTOR..(BytesImageDisk.Length - SIZE_DISK_SECTOR)]; // 2nd~eof-1 sector sectors

            // Compute the hash of the blobGrub
            hashFunction.BlockUpdate(blobGrub, 0, blobGrub.Length);
            hash = new byte[hashFunction.GetDigestSize()];
            hashFunction.DoFinal(hash, 0);
            // Convert the signature to an byte array
            signature = bigIntegersToBytes(signer.GenerateSignature(hash));

            // patch signature & hash
            Buffer.BlockCopy(hash, 0, BytesImageDisk, BytesImageDisk.Length - SIZE_DISK_SECTOR, hash.Length); // length = 0x14
            Buffer.BlockCopy(signature, 0, BytesImageDisk, BytesImageDisk.Length - SIZE_DISK_SECTOR + hash.Length, signature.Length); // length = 0x28
            //
            //  3. Write to output file
            //
            try
            {
                MessageBox.Show("Write to " + textBoxOutputImageDisk.Text);
                File.WriteAllBytes(textBoxOutputImageDisk.Text, BytesImageDisk);
            }
            catch (IOException ex)
            {
                MessageBox.Show("An error occurred while writing to the file: " + ex.Message);
            }
        }

        private void buttonImageUbc_Click(object sender, EventArgs e)
        {
            textBoxImageUbc.Text = buttonFilePath_Click(textBoxImageUbc.Text);
        }

        private void buttonOutputImageUbc_Click(object sender, EventArgs e)
        {
            textBoxOutputImageUbc.Text = buttonFilePath_Click(textBoxOutputImageUbc.Text);
        }

        private void textBoxImageUbc_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(textBoxImageUbc.Text))
            {
                //
                // Read Image file to bytes[] BytesImageUbc
                //
                BytesImageUbc = File.ReadAllBytes(textBoxImageUbc.Text);
                if (textBoxOutputImageUbc.Text.Length == 0)
                {
                    textBoxOutputImageUbc.Text = textBoxImageUbc.Text;
                }
                //enableControlsUbc(isValidImageUbc());
            }
            else
            {
                //enableControlsUbc(false);
            }
        }

        private void buttonSignUbc_Click(object sender, EventArgs e)
        {
            byte[] blobUbc = BytesImageUbc[0..(BytesImageUbc.Length-(HASH_SIZE+SIGNATURE_SIZE))];

            // Compute the hash of the blobUbc
            hashFunction.BlockUpdate(blobUbc, 0, blobUbc.Length);
            byte[] hash = new byte[hashFunction.GetDigestSize()];
            hashFunction.DoFinal(hash, 0);
            // Convert the signature to an byte array
            byte[] signature = bigIntegersToBytes(signer.GenerateSignature(hash));

            // patch signature & hash
            Buffer.BlockCopy(hash, 0, BytesImageUbc, BytesImageUbc.Length - (hash.Length + signature.Length), hash.Length); // length = 0x14
            Buffer.BlockCopy(signature, 0, BytesImageUbc, BytesImageUbc.Length - signature.Length, signature.Length); // length = 0x28
            //
            //  3. Write to output file
            //
            try
            {
                MessageBox.Show("Write to " + textBoxOutputImageUbc.Text);
                File.WriteAllBytes(textBoxOutputImageUbc.Text, BytesImageUbc);
            }
            catch (IOException ex)
            {
                MessageBox.Show("An error occurred while writing to the file: " + ex.Message);
            }
        }

    }
}