using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace AuxStructureLib.IO
{
    /// <summary>
    /// 读数据
    /// </summary>
    public class TXTHelper
    {
        public static DataTable ReadDataFromTXT(string namepath)
        {
            System.IO.FileStream FS = null;
            System.IO.StreamReader SR = null;
            DataTable dt = new DataTable();
            if (System.IO.File.Exists(namepath))
            {
                FS = new System.IO.FileStream(namepath, System.IO.FileMode.Open);
                SR = new System.IO.StreamReader(FS, System.Text.Encoding.Default);
                string HeadLine = SR.ReadLine();
                string[] l = HeadLine.Replace("\t", "@").Split(new Char[2] { ' ', '@' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < l.Length; i++)
                {
                    dt.Columns.Add(l[i].Trim().ToString());
                }
                for (string Line = SR.ReadLine(); Line != null; Line = SR.ReadLine())
                {
                    if (!(Line.Trim() == ""))
                    {
                        string[] strdata = Line.Replace("\t", "@").Split(new Char[2] { ' ', '@' }, StringSplitOptions.RemoveEmptyEntries);
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < strdata.Length; i++)
                        {
                            dr[i] = strdata[i].ToString().Trim();
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
            FS.Dispose();
            SR.Dispose();
            return dt;
        }

        //将表保存到TXT
        public static bool ExportToTxt(System.Data.DataTable table, string fullName)
        {
            int[] iColumnLength = new int[table.Columns.Count];
            FileStream fileStream = new FileStream(fullName, FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream, System.Text.Encoding.Unicode);
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                int iLength = 0;
                for (int j = 0; j < table.Rows.Count; j++)
                {
                    if (iLength < (table.Rows[j][i].ToString()).Length)
                    {
                        iLength = (table.Rows[j][i].ToString()).Length;
                    }
                }
                iColumnLength[i] = iLength;
            }

            for (int j = 0; j < table.Columns.Count; j++)
            {
                string str1 = table.Columns[j].ColumnName.ToString();
                int iLength = str1.Length;
                int iColumnWidth = iColumnLength[j] + 4;
                for (int k = iLength; k < iColumnWidth; k++)
                {
                    str1 += " ";
                }
                if (j == table.Columns.Count - 1)
                {
                    strBuilder.AppendLine(str1);
                }
                else
                {
                    strBuilder.Append(str1);
                }
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    string str1 = table.Rows[i][j].ToString();
                    int iLength = str1.Length;
                    int iColumnWidth = iColumnLength[j] + 4;
                    for (int k = iLength; k < iColumnWidth; k++)
                    {
                        str1 += " ";
                    }
                    if (j == table.Columns.Count - 1)
                    {
                        strBuilder.AppendLine(str1);
                    }
                    else
                    {
                        strBuilder.Append(str1);
                    }
                }
            }
            streamWriter.WriteLine(strBuilder.ToString());
            streamWriter.Close();
            fileStream.Close();
            return true;
        }
    }
}
