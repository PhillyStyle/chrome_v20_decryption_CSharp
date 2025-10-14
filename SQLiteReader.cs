using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLiteReader
{
    public class SQLiteReader
    {
        private byte[] db_bytes;
        private ulong encoding;
        private ushort page_size;
        private byte[] SQLDataTypeSize = new byte[]
        {
            0, 1, 2, 3, 4, 6, 8, 8, 0, 0
        };

        // public-facing collections
        private List<string> field_names;
        private List<sqlite_master_entry> master_table_entries;
        private List<table_entry> table_entries;

#region public API

        public SQLiteReader(string baseName)
        {
            if (!File.Exists(baseName))
                throw new FileNotFoundException("Database file not found.", baseName);

            db_bytes = File.ReadAllBytes(baseName);

            // header check
            if (db_bytes.Length < 16 || Encoding.Default.GetString(db_bytes, 0, 15).CompareTo("SQLite format 3") != 0)
            {
                throw new Exception("Not a valid SQLite 3 Database File");
            }

            if (db_bytes.Length > 52 && db_bytes[52] != 0)
            {
                throw new Exception("Auto-vacuum capable database is not supported");
            }

            page_size = (ushort)ConvertToInteger(16, 2);
            encoding = ConvertToInteger(56, 4);
            if (encoding == 0UL) encoding = 1UL;

            ReadMasterTable(100UL);
        }

        public int GetRowCount()
        {
            return this.table_entries != null ? this.table_entries.Count : 0;
        }

        public List<string> GetTableNames()
        {
            var names = new List<string>();
            if (this.master_table_entries == null) return names;
            for (int i = 0; i < this.master_table_entries.Count; i++)
            {
                if (this.master_table_entries[i].item_type == "table")
                    names.Add(this.master_table_entries[i].item_name);
            }
            return names;
        }

        // return a List<string> for the whole row (public)
        public List<string> GetRow(int row_num)
        {
            if (this.table_entries == null) return null;
            if (row_num < 0 || row_num >= this.table_entries.Count) return null;
            var arr = this.table_entries[row_num].content;
            return arr == null ? null : new List<string>(arr);
        }

        public string GetValue(int row_num, int field)
        {
            if (this.table_entries == null) return null;
            if (row_num < 0 || row_num >= this.table_entries.Count) return null;
            if (field < 0 || field >= this.table_entries[row_num].content.Length) return null;
            return this.table_entries[row_num].content[field];
        }

        public string GetValue(int row_num, string field)
        {
            if (this.field_names == null) return null;
            int idx = -1;
            for (int i = 0; i < this.field_names.Count; i++)
            {
                if (this.field_names[i].Equals(field, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
            if (idx == -1) return null;
            return GetValue(row_num, idx);
        }

#endregion

#region low-level helpers (varint, decoding, etc.)

        private ulong ConvertToInteger(int startIndex, int Size)
        {
            if (Size > 8 || Size == 0) return 0UL;
            if (startIndex < 0 || startIndex + Size > db_bytes.Length) return 0UL;

            ulong num = 0UL;
            for (int i = 0; i < Size; i++)
            {
                num = (num << 8) | (ulong)db_bytes[startIndex + i];
            }
            return num;
        }

        // CVL: Convert varint byte range to Int64 — translated from original logic
        private long CVL(int startIndex, int endIndex)
        {
            endIndex++;
            byte[] array = new byte[8];
            int num = endIndex - startIndex;
            bool flag = false;
            if (num == 0 || num > 9) return 0L;
            if (num == 1)
            {
                array[0] = (byte)(db_bytes[startIndex] & 127);
                return BitConverter.ToInt64(array, 0);
            }
            if (num == 9) flag = true;

            int num2 = 1;
            int num3 = 7;
            int num4 = 0;
            if (flag)
            {
                array[0] = db_bytes[endIndex - 1];
                endIndex--;
                num4 = 1;
            }
            for (int i = endIndex - 1; i >= startIndex; i--)
            {
                if (i - 1 >= startIndex)
                {
                    array[num4] = (byte)(((int)((byte)(db_bytes[i] >> (num2 - 1 & 7))) & 255 >> num2) | (int)((byte)(db_bytes[i - 1] << (num3 & 7))));
                    num2++;
                    num4++;
                    num3--;
                }
                else if (!flag)
                {
                    array[num4] = (byte)((int)((byte)(db_bytes[i] >> (num2 - 1 & 7))) & 255 >> num2);
                }
            }
            return BitConverter.ToInt64(array, 0);
        }

        private int GVL(int startIndex)
        {
            if (startIndex > db_bytes.Length) return 0;
            int num = startIndex + 8;
            for (int i = startIndex; i <= num; i++)
            {
                if (i > db_bytes.Length - 1) return 0;
                if ((db_bytes[i] & 128) != 128) return i;
            }
            return startIndex + 8;
        }

        private bool IsOdd(long value)
        {
            return (value & 1L) == 1L;
        }

        private string DecodeText(int start, int size)
        {
            if (start < 0 || size <= 0 || start + size > db_bytes.Length) return string.Empty;
            if (encoding == 1UL) return Encoding.Default.GetString(db_bytes, start, size);
            if (encoding == 2UL) return Encoding.Unicode.GetString(db_bytes, start, size);
            return Encoding.BigEndianUnicode.GetString(db_bytes, start, size);
        }

#endregion

#region read master/table (kept your posted logic, cleaned formatting)

        private void ReadMasterTable(ulong Offset)
        {
            if (Offset >= (ulong)db_bytes.Length) return;

            int pageByte = db_bytes[(int)Offset];
            if (pageByte == 13) // leaf table b-tree page
            {
                int num = (int)ConvertToInteger((int)(Offset + 3), 2) - 1;
                if (master_table_entries == null) master_table_entries = new List<sqlite_master_entry>();

                for (int i = 0; i <= num; i++)
                {
                    ulong num4 = ConvertToInteger((int)(Offset + 8 + (ulong)(i * 2)), 2);
                    if (Offset != 100UL) num4 += Offset;

                    int num5 = GVL((int)num4);
                    CVL((int)num4, num5);
                    int num6 = GVL(Convert.ToInt32((ulong)(num4 + (ulong)(num5 - (int)num4) + 1UL)));
                    long rowid = CVL(Convert.ToInt32((ulong)(num4 + (ulong)(num5 - (int)num4) + 1UL)), num6);
                    num4 = (ulong)((long)num4 + (long)(num6 - (int)num4) + 1L);

                    num5 = GVL((int)num4);
                    num6 = num5;
                    long value = CVL((int)num4, num5);

                    long[] array = new long[5];
                    int num7 = 0;

                    do
                    {
                        num5 = num6 + 1;
                        num6 = GVL(num5);
                        array[num7] = CVL(num5, num6);

                        if (array[num7] > 9L)
                        {
                            if (IsOdd(array[num7]))
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
                            array[num7] = (long)((ulong)SQLDataTypeSize[(int)array[num7]]);
                        }
                        num7++;
                    }
                    while (num7 <= 4);

                    sqlite_master_entry entry = new sqlite_master_entry();
                    entry.row_id = rowid;

                    int dataStart = (int)((long)num4 + value);

                    // item_type
                    entry.item_type = DecodeText(dataStart, (int)array[0]);

                    // item_name
                    int nextStart = dataStart + (int)array[0];
                    entry.item_name = DecodeText(nextStart, (int)array[1]);

                    // root_num
                    int rootNumStart = nextStart + (int)array[1];
                    entry.root_num = (long)ConvertToInteger(rootNumStart + (int)array[2], (int)array[3]);

                    // sql_statement
                    int sqlStart = rootNumStart + (int)array[2] + (int)array[3];
                    entry.sql_statement = DecodeText(sqlStart, (int)array[4]);

                    master_table_entries.Add(entry);
                }
                return;
            }

            if (pageByte == 5) // interior table b-tree page
            {
                int num8 = (int)ConvertToInteger((int)(Offset + 3), 2) - 1;
                for (int j = 0; j <= num8; j++)
                {
                    ushort num9 = (ushort)ConvertToInteger((int)((int)Offset + 12 + j * 2), 2);
                    if (Offset == 100UL)
                    {
                        ulong childPage = (ConvertToInteger((int)num9, 4) - 1UL) * (ulong)page_size;
                        ReadMasterTable(childPage);
                    }
                    else
                    {
                        ulong childPage = (ConvertToInteger((int)(Offset + (ulong)num9), 4) - 1UL) * (ulong)page_size;
                        ReadMasterTable(childPage);
                    }
                }

                // read right-most pointer
                ulong rightPointer = (ConvertToInteger((int)(Offset + 8), 4) - 1UL) * (ulong)page_size;
                ReadMasterTable(rightPointer);
            }
        }

        public bool ReadTable(string TableName)
        {
            if (master_table_entries == null) return false;

            int num = -1;
            for (int i = 0; i < master_table_entries.Count; i++)
            {
                if (string.Equals(master_table_entries[i].item_name, TableName, StringComparison.OrdinalIgnoreCase))
                {
                    num = i;
                    break;
                }
            }
            if (num == -1) return false;

            string sql = master_table_entries[num].sql_statement;
            int paren = sql.IndexOf("(");
            if (paren < 0) return false;
            string colsPart = sql.Substring(paren + 1);
            string[] colDefs = colsPart.Split(new char[] { ',' });

            field_names = new List<string>();

            for (int j = 0; j < colDefs.Length; j++)
            {
                string s = colDefs[j].TrimStart();
                int idx = s.IndexOf(" ");
                if (idx > 0) s = s.Substring(0, idx);
                if (s.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    break;
                }
                field_names.Add(s);
            }

            long rootNum = master_table_entries[num].root_num;
            ulong offset = (ulong)((rootNum - 1L) * (long)((ulong)page_size));
            return ReadTableFromOffset(offset);
        }

        // This is your modified ReadTableFromOffset (kept logic exactly; small formatting cleanup only)
        private bool ReadTableFromOffset(ulong Offset)
        {
            if (Offset >= (ulong)db_bytes.Length) return false;

            int pageByte = db_bytes[(int)Offset];
            if (pageByte == 13) // leaf table b-tree page
            {
                int num = (int)ConvertToInteger((int)(Offset + 3), 2) - 1;
                if (table_entries == null) table_entries = new List<table_entry>();

                for (int i = 0; i <= num; i++)
                {
                    record_header_field[] headerFields = null;
                    ulong num4 = ConvertToInteger((int)(Offset + 8 + (ulong)(i * 2)), 2);
                    if (Offset != 100UL)
                    {
                        num4 += Offset;
                    }

                    int num5 = GVL((int)num4);
                    CVL((int)num4, num5);
                    int num6 = GVL(Convert.ToInt32((ulong)(num4 + (ulong)(num5 - (int)num4) + 1UL)));
                    long rowid = CVL(Convert.ToInt32((ulong)(num4 + (ulong)(num5 - (int)num4) + 1UL)), num6);
                    num4 = (ulong)((long)num4 + (long)(num6 - (int)num4) + 1L);

                    num5 = GVL((int)num4);
                    num6 = num5;
                    long num7 = CVL((int)num4, num5);
                    long num8 = (long)((long)num4 - num5 + 1L);

                    int num9 = 0;
                    while (num8 < num7)
                    {
                        // enlarge headerFields
                        if (headerFields == null)
                            headerFields = new record_header_field[1];
                        else
                            Array.Resize(ref headerFields, headerFields.Length + 1);

                        num5 = num6 + 1;
                        num6 = GVL(num5);
                        headerFields[num9].type = CVL(num5, num6);

                        if (headerFields[num9].type > 9L)
                        {
                            if (IsOdd(headerFields[num9].type))
                            {
                                headerFields[num9].size = (long)Math.Round((double)(headerFields[num9].type - 13L) / 2.0);
                            }
                            else
                            {
                                headerFields[num9].size = (long)Math.Round((double)(headerFields[num9].type - 12L) / 2.0);
                            }
                        }
                        else
                        {
                            headerFields[num9].size = (long)((ulong)SQLDataTypeSize[(int)headerFields[num9].type]);
                        }
                        num8 = num8 + (long)(num6 - num5) + 1L;
                        num9++;
                    }

                    table_entry row = new table_entry();
                    row.row_id = rowid;
                    row.content = new string[headerFields.Length];

                    int num10 = 0;
                    for (int j = 0; j < headerFields.Length; j++)
                    {
                        if (headerFields[j].type > 9L)
                        {
                            if (!IsOdd(headerFields[j].type))
                            {
                                int readStart = (int)((long)num4 + num7 + num10);
                                int readSize = (int)headerFields[j].size;
                                if (encoding == 1UL)
                                {
                                    row.content[j] = Encoding.Default.GetString(db_bytes, readStart, readSize);
                                }
                                else if (encoding == 2UL)
                                {
                                    row.content[j] = Encoding.Unicode.GetString(db_bytes, readStart, readSize);
                                }
                                else
                                {
                                    row.content[j] = Encoding.BigEndianUnicode.GetString(db_bytes, readStart, readSize);
                                }
                            }
                            else
                            {
                                int readStart = (int)((long)num4 + num7 + num10);
                                row.content[j] = Encoding.Default.GetString(db_bytes, readStart, (int)headerFields[j].size);
                            }
                        }
                        else if (headerFields[j].type == 8)
                        {
                            row.content[j] = "0";
                        }
                        else if (headerFields[j].type == 9)
                        {
                            row.content[j] = "1";
                        }
                        else
                        {
                            int readStart = (int)((long)num4 + num7 + num10);
                            row.content[j] = ConvertToInteger(readStart, (int)headerFields[j].size).ToString();
                        }
                        num10 += (int)headerFields[j].size;
                    }

                    table_entries.Add(row);
                }
            }
            else if (pageByte == 5) // interior table b-tree page
            {
                int num12 = (int)ConvertToInteger((int)(Offset + 3), 2) - 1;
                for (int k = 0; k <= num12; k++)
                {
                    ushort num13 = (ushort)ConvertToInteger((int)((int)Offset + 12 + k * 2), 2);
                    ulong childOffset = (ConvertToInteger((int)(Offset + (ulong)num13), 4) - 1UL) * (ulong)page_size;
                    ReadTableFromOffset(childOffset);
                }

                ulong rightPointer = (ConvertToInteger((int)(Offset + 8), 4) - 1UL) * (ulong)page_size;
                ReadTableFromOffset(rightPointer);
            }
            return true;
        }

#endregion

#region internal structs

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

#endregion
    }
}
