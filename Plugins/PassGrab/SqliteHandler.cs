using System;
using System.IO;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace InfoGrab
{
    public class SQLiteHandler
    {
        public SQLiteHandler(string baseName)
        {
            if (File.Exists(baseName))
            {
                FileSystem.FileOpen(1, baseName, OpenMode.Binary, OpenAccess.Read, OpenShare.Shared, -1);
                string s = Strings.Space((int)FileSystem.LOF(1));
                FileSystem.FileGet(1, ref s, -1L, false);
                FileSystem.FileClose(new int[]
                {
                    1
                });
                this.db_bytes = Encoding.Default.GetBytes(s);
                if (Encoding.Default.GetString(this.db_bytes, 0, 15).CompareTo("SQLite format 3") != 0)
                {
                    throw new Exception("Not a valid SQLite 3 Database File");
                }
                if (this.db_bytes[52] != 0)
                {
                    throw new Exception("Auto-vacuum capable database is not supported");
                }
                this.page_size = (ushort)this.ConvertToInteger(16, 2);
                this.encoding = this.ConvertToInteger(56, 4);
                if (decimal.Compare(new decimal(this.encoding), 0m) == 0)
                {
                    this.encoding = 1UL;
                }
                this.ReadMasterTable(100UL);
            }
        }

        /// <summary>
        /// Converts a sequence of bytes to an unsigned integer.
        /// </summary>
        /// <param name="startIndex">The starting index of the byte sequence.</param>
        /// <param name="Size">The number of bytes to be converted.</param>
        /// <returns>The unsigned integer value converted from the byte sequence.</returns>
        /// <remarks>
        /// This method converts a sequence of bytes starting from the specified <paramref name="startIndex"/> to an unsigned integer value.
        /// If the <paramref name="Size"/> is greater than 8 or equal to 0, the method returns 0.
        /// The conversion is performed by bitwise shifting and OR operations to combine the bytes into an unsigned integer value.
        /// </remarks>
        private ulong ConvertToInteger(int startIndex, int Size)
        {
            if (Size > 8 | Size == 0)
            {
                return 0UL;
            }
            ulong num = 0UL;
            int num2 = Size - 1;
            for (int i = 0; i <= num2; i++)
            {
                num = (num << 8 | (ulong)this.db_bytes[startIndex + i]);
            }
            return num;
        }

        /// <summary>
        /// Converts a range of bytes to a long integer and returns the result.
        /// </summary>
        /// <param name="startIndex">The starting index of the byte range.</param>
        /// <param name="endIndex">The ending index of the byte range.</param>
        /// <returns>The long integer value converted from the specified byte range.</returns>
        /// <remarks>
        /// This method converts a range of bytes from the <paramref name="startIndex"/> to <paramref name="endIndex"/> (inclusive) to a long integer value.
        /// It handles different cases based on the number of bytes in the range and modifies the byte array accordingly.
        /// If the number of bytes is 0 or greater than 9, the method returns 0.
        /// If there is only one byte in the range, it extracts the value and returns it as a long integer.
        /// If there are 9 bytes in the range, it handles a special case and sets a flag.
        /// The method then processes the bytes and performs bitwise operations to construct the long integer value.
        /// The resulting long integer value is returned.
        /// </remarks>
        private long CVL(int startIndex, int endIndex)
        {
            endIndex++;
            byte[] array = new byte[8];
            int num = endIndex - startIndex;
            bool flag = false;
            if (num == 0 | num > 9)
            {
                return 0L;
            }
            if (num == 1)
            {
                array[0] = (byte)(this.db_bytes[startIndex] & 127);
                return BitConverter.ToInt64(array, 0);
            }
            if (num == 9)
            {
                flag = true;
            }
            int num2 = 1;
            int num3 = 7;
            int num4 = 0;
            if (flag)
            {
                array[0] = this.db_bytes[endIndex - 1];
                endIndex--;
                num4 = 1;
            }
            for (int i = endIndex - 1; i >= startIndex; i += -1)
            {
                if (i - 1 >= startIndex)
                {
                    array[num4] = (byte)(((int)((byte)(this.db_bytes[i] >> (num2 - 1 & 7))) & 255 >> num2) | (int)((byte)(this.db_bytes[i - 1] << (num3 & 7))));
                    num2++;
                    num4++;
                    num3--;
                }
                else if (!flag)
                {
                    array[num4] = (byte)((int)((byte)(this.db_bytes[i] >> (num2 - 1 & 7))) & 255 >> num2);
                }
            }
            return BitConverter.ToInt64(array, 0);
        }

        /// <summary>
        /// Gets the number of rows in the table.
        /// </summary>
        /// <returns>The number of rows in the table.</returns>
        public int GetRowCount()
        {
            return this.table_entries.Length;
        }

        /// <summary>
        /// Retrieves the names of tables from the master table entries and returns an array of strings.
        /// </summary>
        /// <returns>An array of strings containing the names of tables from the master table entries.</returns>
        /// <remarks>
        /// This method iterates through the master table entries and retrieves the names of tables.
        /// It then stores the names in an array of strings and returns the array.
        /// </remarks>
        public string[] GetTableNames()
        {
            string[] array = null;
            int num = 0;
            int num2 = this.master_table_entries.Length - 1;
            for (int i = 0; i <= num2; i++)
            {
                if (this.master_table_entries[i].item_type == "table")
                {
                    array = (string[])Utils.CopyArray(array, new string[num + 1]);
                    array[num] = this.master_table_entries[i].item_name;
                    num++;
                }
            }
            return array;
        }

        /// <summary>
        /// Retrieves the value at the specified row and field.
        /// </summary>
        /// <param name="row_num">The row number.</param>
        /// <param name="field">The field name.</param>
        /// <returns>The value at the specified <paramref name="row_num"/> and <paramref name="field"/>.</returns>
        /// <remarks>
        /// This method retrieves the value at the specified <paramref name="row_num"/> and <paramref name="field"/> from the internal data structure.
        /// If the specified <paramref name="field"/> is not found, it returns null.
        /// </remarks>
        public string GetValue(int row_num, int field)
        {
            if (row_num >= this.table_entries.Length)
            {
                return null;
            }
            if (field >= this.table_entries[row_num].content.Length)
            {
                return null;
            }
            return this.table_entries[row_num].content[field];
        }

        public string GetValue(int row_num, string field)
        {
            int num = -1;
            int num2 = this.field_names.Length - 1;
            for (int i = 0; i <= num2; i++)
            {
                if (this.field_names[i].ToLower().CompareTo(field.ToLower()) == 0)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return null;
            }
            return this.GetValue(row_num, num);
        }

        /// <summary>
        /// Returns the index of the first non-128 byte in the array starting from the specified index.
        /// </summary>
        /// <param name="startIndex">The starting index in the array.</param>
        /// <returns>The index of the first non-128 byte in the array starting from <paramref name="startIndex"/>.</returns>
        /// <remarks>
        /// This method returns the index of the first non-128 byte in the array starting from the specified index.
        /// If the <paramref name="startIndex"/> is greater than the length of the array, it returns 0.
        /// If no non-128 byte is found within the next 8 bytes from the <paramref name="startIndex"/>, it returns <paramref name="startIndex"/> + 8.
        /// </remarks>
        private int GVL(int startIndex)
        {
            if (startIndex > this.db_bytes.Length)
            {
                return 0;
            }
            int num = startIndex + 8;
            for (int i = startIndex; i <= num; i++)
            {
                if (i > this.db_bytes.Length - 1)
                {
                    return 0;
                }
                if ((this.db_bytes[i] & 128) != 128)
                {
                    return i;
                }
            }
            return startIndex + 8;
        }

        /// <summary>
        /// Determines if the given value is odd.
        /// </summary>
        /// <param name="value">The value to be checked for oddity.</param>
        /// <returns>True if the <paramref name="value"/> is odd; otherwise, false.</returns>
        private bool IsOdd(long value)
        {
            return (value & 1L) == 1L;
        }

        /// <summary>
        /// Reads the master table from the specified offset.
        /// </summary>
        /// <param name="Offset">The offset from which to read the master table.</param>
        /// <remarks>
        /// This method reads the master table from the specified offset in the database file.
        /// It processes the data according to the SQLite file format specifications and populates the master_table_entries array with the retrieved information.
        /// The method handles different encodings for reading the item type, item name, and SQL statement from the database bytes.
        /// If the byte at the specified offset is 13, it reads the master table entries and populates the master_table_entries array.
        /// If the byte at the specified offset is 5, it recursively calls the ReadMasterTable method to read the master table entries from the specified offsets within the database file.
        /// </remarks>
        private void ReadMasterTable(ulong Offset)
        {
            if (this.db_bytes[(int)Offset] == 13)
            {
                ushort num = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
                int num2 = 0;
                if (this.master_table_entries != null)
                {
                    num2 = this.master_table_entries.Length;
                    this.master_table_entries = (SQLiteHandler.sqlite_master_entry[])Utils.CopyArray(this.master_table_entries, new SQLiteHandler.sqlite_master_entry[this.master_table_entries.Length + (int)num + 1]);
                }
                else
                {
                    this.master_table_entries = new SQLiteHandler.sqlite_master_entry[(int)(num + 1)];
                }
                int num3 = (int)num;
                for (int i = 0; i <= num3; i++)
                {
                    ulong num4 = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8m), new decimal(i * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100m) != 0)
                    {
                        num4 += Offset;
                    }
                    int num5 = this.GVL((int)num4);
                    this.CVL((int)num4, num5);
                    int num6 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)));
                    this.master_table_entries[num2 + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)), num6);
                    num4 = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num6), new decimal(num4))), 1m));
                    num5 = this.GVL((int)num4);
                    num6 = num5;
                    long value = this.CVL((int)num4, num5);
                    long[] array = new long[5];
                    int num7 = 0;
                    do
                    {
                        num5 = num6 + 1;
                        num6 = this.GVL(num5);
                        array[num7] = this.CVL(num5, num6);
                        if (array[num7] > 9L)
                        {
                            if (this.IsOdd(array[num7]))
                            {
                                array[num7] = (long)Math.Round((double)(array[num7] - 13L) / 2.0);
                            }
                            else
                            {
                                array[num7] = (long)Math.Round((double)(array[num7] - 12L) / 2.0);
                            }
                        }
                        else
                        {
                            array[num7] = (long)((ulong)this.SQLDataTypeSize[(int)array[num7]]);
                        }
                        num7++;
                    }
                    while (num7 <= 4);
                    if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
                    {
                        this.master_table_entries[num2 + i].item_type = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num4), new decimal(value))), (int)array[0]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
                    {
                        this.master_table_entries[num2 + i].item_type = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num4), new decimal(value))), (int)array[0]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
                    {
                        this.master_table_entries[num2 + i].item_type = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num4), new decimal(value))), (int)array[0]);
                    }
                    if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
                    {
                        this.master_table_entries[num2 + i].item_name = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0]))), (int)array[1]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
                    {
                        this.master_table_entries[num2 + i].item_name = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0]))), (int)array[1]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
                    {
                        this.master_table_entries[num2 + i].item_name = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0]))), (int)array[1]);
                    }
                    this.master_table_entries[num2 + i].root_num = (long)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2]))), (int)array[3]);
                    if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
                    {
                        this.master_table_entries[num2 + i].sql_statement = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2])), new decimal(array[3]))), (int)array[4]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
                    {
                        this.master_table_entries[num2 + i].sql_statement = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2])), new decimal(array[3]))), (int)array[4]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
                    {
                        this.master_table_entries[num2 + i].sql_statement = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2])), new decimal(array[3]))), (int)array[4]);
                    }
                }
                return;
            }
            if (this.db_bytes[(int)Offset] == 5)
            {
                int num8 = (int)Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
                for (int j = 0; j <= num8; j++)
                {
                    ushort num9 = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12m), new decimal(j * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100m) == 0)
                    {
                        this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)num9, 4)), 1m), new decimal((int)this.page_size))));
                    }
                    else
                    {
                        this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + (ulong)num9), 4)), 1m), new decimal((int)this.page_size))));
                    }
                }
                this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8m)), 4)), 1m), new decimal((int)this.page_size))));
            }
        }

        /// <summary>
        /// Reads the specified table and returns a boolean indicating success or failure.
        /// </summary>
        /// <param name="TableName">The name of the table to be read.</param>
        /// <returns>True if the table was successfully read; otherwise, false.</returns>
        /// <remarks>
        /// This method searches for the specified table name in the master table entries and retrieves the SQL statement associated with it.
        /// It then extracts the field names from the SQL statement and populates the field_names array.
        /// If the table is found and the fields are successfully extracted, it calls the ReadTableFromOffset method to read the table data from the specified offset.
        /// </remarks>
        public bool ReadTable(string TableName)
        {
            int num = -1;
            int num2 = this.master_table_entries.Length - 1;
            for (int i = 0; i <= num2; i++)
            {
                if (this.master_table_entries[i].item_name.ToLower().CompareTo(TableName.ToLower()) == 0)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return false;
            }
            string[] array = this.master_table_entries[num].sql_statement.Substring(this.master_table_entries[num].sql_statement.IndexOf("(") + 1).Split(new char[]
            {
                ','
            });
            int num3 = array.Length - 1;
            for (int j = 0; j <= num3; j++)
            {
                array[j] = array[j].TrimStart(new char[0]);
                int num4 = array[j].IndexOf(" ");
                if (num4 > 0)
                {
                    array[j] = array[j].Substring(0, num4);
                }
                if (array[j].IndexOf("UNIQUE") == 0)
                {
                    break;
                }
                this.field_names = (string[])Utils.CopyArray(this.field_names, new string[j + 1]);
                this.field_names[j] = array[j];
            }
            return this.ReadTableFromOffset((ulong)((this.master_table_entries[num].root_num - 1L) * (long)((ulong)this.page_size)));
        }

        /// <summary>
        /// Reads a table from the specified offset and populates the table entries.
        /// </summary>
        /// <param name="Offset">The offset from which to read the table.</param>
        /// <returns>True if the table is successfully read and populated; otherwise, false.</returns>
        /// <remarks>
        /// This method reads a table from the specified offset in the database file and populates the table entries.
        /// It handles different types of data encoding and modifies the table entries in place.
        /// </remarks>
        private bool ReadTableFromOffset(ulong Offset)
        {
            if (this.db_bytes[(int)Offset] == 13)
            {
                int num = Convert.ToInt32(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
                int num2 = 0;
                if (this.table_entries != null)
                {
                    num2 = this.table_entries.Length;
                    this.table_entries = (SQLiteHandler.table_entry[])Utils.CopyArray(this.table_entries, new SQLiteHandler.table_entry[this.table_entries.Length + num + 1]);
                }
                else
                {
                    this.table_entries = new SQLiteHandler.table_entry[num + 1];
                }
                int num3 = num;
                for (int i = 0; i <= num3; i++)
                {
                    SQLiteHandler.record_header_field[] array = null;
                    ulong num4 = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8m), new decimal(i * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100m) != 0)
                    {
                        num4 += Offset;
                    }
                    int num5 = this.GVL((int)num4);
                    this.CVL((int)num4, num5);
                    int num6 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)));
                    this.table_entries[num2 + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)), num6);
                    num4 = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num6), new decimal(num4))), 1m));
                    num5 = this.GVL((int)num4);
                    num6 = num5;
                    long num7 = this.CVL((int)num4, num5);
                    long num8 = Convert.ToInt64(decimal.Add(decimal.Subtract(new decimal(num4), new decimal(num5)), 1m));
                    int num9 = 0;
                    while (num8 < num7)
                    {
                        array = (SQLiteHandler.record_header_field[])Utils.CopyArray(array, new SQLiteHandler.record_header_field[num9 + 1]);
                        num5 = num6 + 1;
                        num6 = this.GVL(num5);
                        array[num9].type = this.CVL(num5, num6);
                        if (array[num9].type > 9L)
                        {
                            if (this.IsOdd(array[num9].type))
                            {
                                array[num9].size = (long)Math.Round((double)(array[num9].type - 13L) / 2.0);
                            }
                            else
                            {
                                array[num9].size = (long)Math.Round((double)(array[num9].type - 12L) / 2.0);
                            }
                        }
                        else
                        {
                            array[num9].size = (long)((ulong)this.SQLDataTypeSize[(int)array[num9].type]);
                        }
                        num8 = num8 + (long)(num6 - num5) + 1L;
                        num9++;
                    }
                    this.table_entries[num2 + i].content = new string[array.Length - 1 + 1];
                    int num10 = 0;
                    int num11 = array.Length - 1;
                    for (int j = 0; j <= num11; j++)
                    {
                        if (array[j].type > 9L)
                        {
                            if (!this.IsOdd(array[j].type))
                            {
                                if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
                                {
                                    this.table_entries[num2 + i].content[j] = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
                                }
                                else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
                                {
                                    this.table_entries[num2 + i].content[j] = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
                                }
                                else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
                                {
                                    this.table_entries[num2 + i].content[j] = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
                                }
                            }
                            else
                            {
                                this.table_entries[num2 + i].content[j] = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
                            }
                        }
                        else
                        {
                            this.table_entries[num2 + i].content[j] = Conversions.ToString(this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size));
                        }
                        num10 += (int)array[j].size;
                    }
                }
            }
            else if (this.db_bytes[(int)Offset] == 5)
            {
                int num12 = (int)Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
                for (int k = 0; k <= num12; k++)
                {
                    ushort num13 = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12m), new decimal(k * 2))), 2);
                    this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + (ulong)num13), 4)), 1m), new decimal((int)this.page_size))));
                }
                this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8m)), 4)), 1m), new decimal((int)this.page_size))));
            }
            return true;
        }

        private byte[] db_bytes;
        private ulong encoding;
        private string[] field_names;
        private SQLiteHandler.sqlite_master_entry[] master_table_entries;
        private ushort page_size;
        private byte[] SQLDataTypeSize = new byte[]
        {
            0,
            1,
            2,
            3,
            4,
            6,
            8,
            8,
            0,
            0
        };

        private SQLiteHandler.table_entry[] table_entries;

        private struct record_header_field
        {
            public long size;
            public long type;
        }

        private struct sqlite_master_entry
        {
            public long row_id;
            public string item_type;
            public string item_name;
            public long root_num;
            public string sql_statement;
        }

        private struct table_entry
        {
            public long row_id;
            public string[] content;
        }
    }
}